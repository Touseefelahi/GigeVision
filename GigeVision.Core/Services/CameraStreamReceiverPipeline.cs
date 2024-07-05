using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GigeVision.Core.Services
{
    public class CameraStreamReceiverPipeline : StreamReceiverBase
    {
        private int packetId = 0;
        private int packetSize;

        protected override async void Receiver()
        {
            DetectGvspType();
            var options = new PipeOptions(minimumSegmentSize: GvspInfo.PacketLength);
            var streamPipe = new Pipe(options);

            var readPipe = StreamReceiverPipe(socketRxRaw, streamPipe.Writer);
            var displayStreamPipe = DisplayStreamPipe(streamPipe.Reader);
            await Task.WhenAll(readPipe, displayStreamPipe);
        }

        private async Task DisplayStreamPipe(PipeReader reader)
        {
            packetSize = GvspInfo.PayloadSize + GvspInfo.PayloadOffset;
            var buffer = new byte[GvspInfo.FinalPacketID * packetSize];
            int startPos = 0;
            int totalPacketsInFrame = 0;

            while (IsReceiving)
            {
                try
                {
                    var i = 0;
                    var readerResult = await reader.ReadAsync();
                    var packetsBuffer = readerResult.Buffer;
                    var bufferLength = packetsBuffer.Length;

                    while (i < bufferLength)
                    {
                        var singlePacket = i + GvspInfo.PacketLength <= packetsBuffer.Length ? packetsBuffer.Slice(i, GvspInfo.PacketLength) : packetsBuffer.Slice(i, bufferLength - i);
                        int packetLength = singlePacket.First.Span.Length;
                        int segmentLength = 0;

                        if (singlePacket.IsSingleSegment)
                        {
                            if (singlePacket.First.Span[4] == GvspInfo.DataIdentifier)
                            {
                                totalPacketsInFrame++;
                                packetId = (singlePacket.First.Span[GvspInfo.PacketIDIndex] << 8 | singlePacket.First.Span[GvspInfo.PacketIDIndex + 1]);
                                startPos = (packetId - 1) * GvspInfo.PayloadSize;
                                singlePacket.First.Span.Slice(GvspInfo.PayloadOffset, packetLength - GvspInfo.PayloadOffset).CopyTo(buffer.AsSpan().Slice(startPos, packetLength));
                            }
                        }
                        else
                        {
                            foreach (var segment in singlePacket)
                            {
                                if (!segment.IsEmpty)
                                {
                                    segmentLength = segment.Span.Length;
                                    if (segment.Span[4] == GvspInfo.DataIdentifier)
                                    {
                                        totalPacketsInFrame++;
                                        packetId = (segment.Span[GvspInfo.PacketIDIndex] << 8 | segment.Span[GvspInfo.PacketIDIndex + 1]);
                                        startPos = (packetId - 1) * GvspInfo.PayloadSize;
                                        segment.Span.Slice(GvspInfo.PayloadOffset, segmentLength - GvspInfo.PayloadOffset).CopyTo(buffer.AsSpan().Slice(startPos, segmentLength));
                                    }
                                }
                            }
                        }

                        if (packetId == GvspInfo.FinalPacketID)
                        {
                            if (packetId - totalPacketsInFrame <= MissingPacketTolerance)
                            {
                                FrameReady?.Invoke(totalPacketsInFrame, buffer);
                                totalPacketsInFrame = 0;
                            }
                        }
                        i += packetSize;
                    }
                    reader.AdvanceTo(packetsBuffer.End);
                }
                catch (Exception ex)
                {

                }
            }
        }

        public async Task StreamReceiverPipe(Socket socketRxRaw, PipeWriter writer)
        {
            int i = 0;
            try
            {
                while (IsReceiving)
                {
                    var packet = writer.GetMemory(GvspInfo.PacketLength);
                    int length = socketRxRaw.Receive(packet.Span.ToArray());
                    writer.Advance(length);
                    i++;
                    if (i >= (GvspInfo.IsDecodingAsVersion2 ? 10 : 9))
                    {
                        await writer.FlushAsync();
                        i = 0;
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}