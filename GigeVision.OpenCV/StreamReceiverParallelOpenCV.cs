using Emgu.CV;
using GigeVision.Core.Enums;
using GigeVision.Core.Models;
using GigeVision.Core.Services;

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

        public StreamReceiverParallelOpencv(int totalBuffers = 3)
        {
            if (totalBuffers < 1)
            {
                throw new ArgumentException("Total Buffers should be greater than 0, Use at least 2 buffers");
            }
            MissingPacketTolerance = 0;
            TotalBuffers = totalBuffers;
        }

        public int TotalBuffers { get; private set; }

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
                }
                indexMemoryWriter = 0;
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
            int indexMemoryReader, imageBufferIndex = 0, packetRxCount = 0, packetID, bufferStart, bufferLength;
            imageIndex = 0;
            lossCount = 0;
            var imageSpan = image[imageBufferIndex].GetSpan<byte>();
            Memory<byte>[] span = new Memory<byte>[flatBufferCount];

            for (int i = 0; i < flatBufferCount; i++)
            {
                span[i] = new Memory<byte>(packetBuffersFlat[i]);
            }

            int finalPacketLength = (image[imageBufferIndex].Width * image[imageBufferIndex].Height) % GvspInfo.PayloadSize;
            var length = GvspInfo.PacketLength;
            indexMemoryReader = 0;
            while (IsReceiving)
            {
                waitForPacketChunk.Wait();
                var currentMemoryBuffer = span[indexMemoryReader];

                for (int i = 0; i < ChunkPacketCount; i++)
                {
                    Span<byte> packet;
                    packet = currentMemoryBuffer.Span.Slice(i * length, length);

                    switch (packet[4] & 0x0F)//it unifies extended ID and normal ID
                    {
                        case 3: //Data
                            packetRxCount++;
                            packetID = (packet[GvspInfo.PacketIDIndex] << 8) | packet[GvspInfo.PacketIDIndex + 1];
                            bufferStart = (packetID - 1) * GvspInfo.PayloadSize;
                            bufferLength = GvspInfo.PayloadSize;
                            if (packetID == GvspInfo.FinalPacketID)
                            {
                                bufferLength = finalPacketLength;
                            }
                            packet.Slice(GvspInfo.PayloadOffset, bufferLength).TryCopyTo(imageSpan.Slice(bufferStart, bufferLength));
                            break;

                        case 2: //Data End
                            imageIndex++;
                            //Checking if we receive all packets
                            if (Math.Abs(packetRxCount - GvspInfo.FinalPacketID) > MissingPacketTolerance)
                            {
                                lossCount++;
                                packetRxCount = 0;
                                break;
                            }
                            packetRxCount = 0;
                            frameInCounter++;
                            waitHandleFrame.Release();
                            imageBufferIndex++;
                            if (imageBufferIndex == TotalBuffers)
                            {
                                imageBufferIndex = 0;
                            }
                            imageSpan = image[imageBufferIndex].GetSpan<byte>(); //Next Frame
                            break;
                    }
                }

                if (++indexMemoryReader > flatBufferCount - 1)
                {
                    indexMemoryReader = 0;
                }
            }
        }
    }
}