using Emgu.CV;
using GigeVision.Core.Enums;
using GigeVision.Core.Models;
using GigeVision.Core.Services;
using System.Net;
using System.Net.Sockets;

namespace GigeVision.OpenCV
{
    public class StreamReceiverParallelOpencv : StreamReceiverBase
    {
        public uint frameInCounter = 0;
        public Mat[] image = null!;
        public long imageIndex, lossCount = 0;
        public SemaphoreSlim waitHandleFrame = new(0);
        private const int ChunkPacketCount = 100;
        private const int flatBufferCount = 5;
        private readonly int packetBufferLength = ChunkPacketCount;
        private readonly SemaphoreSlim waitForPacketChunk = new(0);
        private byte[][] packetBuffersFlat = null!;
        private readonly ManualResetEventSlim _decodeReady = new(false);
        public int TotalBuffers { get; private set; }
        public StreamReceiverParallelOpencv(int totalBuffers = 3)
        {
            if (totalBuffers < 1)
            {
                throw new ArgumentException("Total Buffers should be greater than 0, Use at least 2 buffers");
            }
            MissingPacketTolerance = 0;
            TotalBuffers = totalBuffers;
        }

        protected override async void Receiver()
        {
            int indexMemoryWriter = 0;
            int length = 0;
            int counterForChunkPackets = 0;
            frameInCounter = 0;
            int counterBufferWriter = 0;
            try
            {
                DetectGvspType();
                packetBuffersFlat = new byte[flatBufferCount][];
                Memory<byte>[] memory = new Memory<byte>[flatBufferCount];
                for (int i = 0; i < flatBufferCount; i++)
                {
                    packetBuffersFlat[i] = new byte[packetBufferLength * GvspInfo.PacketLength];
                    memory[i] = new Memory<byte>(packetBuffersFlat[i]);
                }
                image = new Mat[TotalBuffers];
                for (int i = 0; i < TotalBuffers; i++)
                {
                    if (GvspInfo.BytesPerPixel == 1)
                    {
                        image[i] = new Mat(GvspInfo.Height, GvspInfo.Width, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
                    }
                    if (GvspInfo.BytesPerPixel == 2)
                    {
                        image[i] = new Mat(GvspInfo.Height, GvspInfo.Width, Emgu.CV.CvEnum.DepthType.Cv16U, 1);
                    }
                    else
                    {
                        // Packed or non-byte aligned formats (10/12-bit packed, YUV422, etc)
                        // Allocate raw byte buffer with the exact frame size in BYTES.
                        // Make columns = width * bytesPerPixel so Mat buffer size matches.
                        int cols = checked((int)(GvspInfo.Width * GvspInfo.BytesPerPixel));
                        image[i] = new Mat(GvspInfo.Height, cols, Emgu.CV.CvEnum.DepthType.Cv8U, 1);
                    }
                }
                indexMemoryWriter = 0;
                _decodeReady.Set();               // <= buffers are safe to use now
                _ = Task.Run(DecodePackets);

                while (IsReceiving)
                {
                    Memory<byte> singlePacket;
                    singlePacket = memory[indexMemoryWriter].Slice(counterBufferWriter * GvspInfo.PacketLength, GvspInfo.PacketLength);
                    counterBufferWriter++;

                    length = socketRxRaw.Receive(singlePacket.Span);

                    if (++counterForChunkPackets % ChunkPacketCount == 0)
                    {
                        if (++indexMemoryWriter > flatBufferCount - 1)
                        {
                            indexMemoryWriter = 0;
                            counterBufferWriter = 0;
                        }
                        waitForPacketChunk.Release();
                        counterBufferWriter = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                if (IsReceiving) // We didn't deliberately stop the stream
                {
                    Updates?.Invoke(UpdateType.StreamStopped, ex.Message);
                }
                IsReceiving = false;
                waitForPacketChunk.Release();
                waitHandleFrame.Release();
            }
        }

        private void DecodePackets()
        {
            // Don't touch image[] until the receiver finished allocating it
            _decodeReady.Wait();

            int indexMemoryReader = 0;
            int imageBufferIndex = 0;
            int packetRxCount = 0;

            // Flat chunk views (what the Receiver just filled)
            var chunks = new Memory<byte>[flatBufferCount];
            for (int i = 0; i < flatBufferCount; i++)
                chunks[i] = new Memory<byte>(packetBuffersFlat[i]);

            // Current destination frame (as raw bytes)
            Mat img = image[imageBufferIndex] ?? throw new InvalidOperationException("Image ring not allocated.");
            Span<byte> dest = img.GetSpan<byte>();

            // Frame/packet sizes in BYTES
            int payloadBytes = GvspInfo.PayloadSize;               // payload per GVSP data packet
            int frameBytes = dest.Length;                        // full frame size in BYTES
            int lastPacketBytes = frameBytes - payloadBytes * (GvspInfo.FinalPacketID - 1);
            if (lastPacketBytes <= 0 || lastPacketBytes > payloadBytes)
                lastPacketBytes = payloadBytes;                    // guard (exact multiple or weird XML)

            int pktLen = GvspInfo.PacketLength;

            while (IsReceiving)
            {
                // Wait until the receiver gives us a filled chunk buffer
                waitForPacketChunk.Wait();
                if (!IsReceiving) break;

                var current = chunks[indexMemoryReader];

                for (int i = 0; i < ChunkPacketCount; i++)
                {
                    Span<byte> udp = current.Span.Slice(i * pktLen, pktLen);
                    byte pt = (byte)(udp[4] & 0x0F);               // packet type (data / data end / etc.)

                    switch (pt)
                    {
                        case 3: // Data
                            {
                                packetRxCount++;

                                // 1..FinalPacketID
                                int id = (udp[GvspInfo.PacketIDIndex] << 8) | udp[GvspInfo.PacketIDIndex + 1];
                                if (id < 1 || id > GvspInfo.FinalPacketID) break;     // sanity

                                int start = (id - 1) * payloadBytes;                // dest start in BYTES
                                int intended = (id == GvspInfo.FinalPacketID) ? lastPacketBytes : payloadBytes;

                                // Clamp by what really arrived and what still fits
                                int available = Math.Max(0, udp.Length - GvspInfo.PayloadOffset);
                                int remaining = Math.Max(0, frameBytes - start);
                                int toCopy = Math.Min(intended, Math.Min(available, remaining));

                                if (toCopy > 0)
                                    udp.Slice(GvspInfo.PayloadOffset, toCopy)
                                       .CopyTo(dest.Slice(start, toCopy));
                                break;
                            }

                        case 2: // Data End (frame complete)
                            {
                                // Optional: loss check (vendor packetization tolerant)
                                if (Math.Abs(packetRxCount - GvspInfo.FinalPacketID) > MissingPacketTolerance)
                                    lossCount++;
                                packetRxCount = 0;

                                frameInCounter++;
                                waitHandleFrame.Release();

                                // Advance to next ring buffer
                                imageBufferIndex = (imageBufferIndex + 1) % TotalBuffers;
                                img = image[imageBufferIndex];
                                if (img == null) continue;             // defensive (should not happen)

                                dest = img.GetSpan<byte>();
                                frameBytes = dest.Length;
                                lastPacketBytes = frameBytes - payloadBytes * (GvspInfo.FinalPacketID - 1);
                                if (lastPacketBytes <= 0 || lastPacketBytes > payloadBytes)
                                    lastPacketBytes = payloadBytes;

                                break;
                            }

                        default:
                            // Ignore leader/trailer/unknown types
                            break;
                    }
                }

                // Next filled chunk buffer
                indexMemoryReader = (indexMemoryReader + 1) % flatBufferCount;
            }
        }
    }
}