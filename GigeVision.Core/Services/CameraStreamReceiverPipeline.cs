using System;
using System.Drawing;
using System.IO;
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
            int pixel = 0;
            var greenColor = 0;
            var redColor = 0;
            var blueColor = 0;
            packetSize = GvspInfo.PayloadSize + GvspInfo.PayloadOffset;
            var buffer = new byte[GvspInfo.FinalPacketID * packetSize];
            byte[] colorImageBytes = new byte[GvspInfo.FinalPacketID * packetSize * 3];
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
                        int packetLength = singlePacket.FirstSpan.Length;
                        int segmentLength = 0;

                        if (singlePacket.IsSingleSegment)
                        {
                            if (singlePacket.FirstSpan[4] == GvspInfo.DataIdentifier)
                            {
                                totalPacketsInFrame++;
                                packetId = (singlePacket.FirstSpan[GvspInfo.PacketIDIndex] << 8 | singlePacket.FirstSpan[GvspInfo.PacketIDIndex + 1]);
                                startPos = (packetId - 1) * GvspInfo.PayloadSize;
                                singlePacket.FirstSpan.Slice(GvspInfo.PayloadOffset, packetLength - GvspInfo.PayloadOffset).CopyTo(buffer.AsSpan().Slice(startPos, packetLength));
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
                                for (int row = 1; row < GvspInfo.Height - 1; row++)
                                {
                                    for (int column = 1; column < GvspInfo.Width - 1; column += 3)
                                    {
                                        pixel = row * GvspInfo.Width + column;
                                        if (row % 2 == 0 && column % 2 == 0)
                                        {
                                            redColor = buffer[pixel];
                                            greenColor = ((buffer[row * GvspInfo.Width + (column - 1)]) + (buffer[row * GvspInfo.Width + (column + 1)])
                                                          + (buffer[(row - 1) * GvspInfo.Width + (column)]) + (buffer[(row + 1) * GvspInfo.Width + column])) / 4;
                                            blueColor = ((buffer[(row - 1) * GvspInfo.Width + (column - 1)]) + (buffer[(row - 1) * GvspInfo.Width + (column + 1)])
                                                          + (buffer[(row + 1) * GvspInfo.Width + (column - 1)]) + (buffer[(row + 1) * GvspInfo.Width + (column + 1)])) / 4;
                                            colorImageBytes[pixel]     = (byte)greenColor;
                                            colorImageBytes[pixel + 1] = (byte)redColor;
                                            colorImageBytes[pixel + 2] = (byte)blueColor;

                                        }
                                        else if (row % 2 != 0 && column % 2 != 0)
                                        {
                                            blueColor = buffer[pixel];
                                            greenColor = ((buffer[row * GvspInfo.Width + (column - 1)]) + (buffer[row * GvspInfo.Width + (column + 1)])
                                                          + (buffer[(row - 1) * GvspInfo.Width + (column)]) + (buffer[(row + 1) * GvspInfo.Width + column])) / 4;
                                            redColor = ((buffer[(row - 1) * GvspInfo.Width + (column - 1)]) + (buffer[(row - 1) * GvspInfo.Width + (column + 1)])
                                                          + (buffer[(row + 1) * GvspInfo.Width + (column - 1)]) + (buffer[(row + 1) * GvspInfo.Width + (column + 1)])) / 4;
                                            colorImageBytes[pixel] = (byte)greenColor;
                                            colorImageBytes[pixel + 1] = (byte)redColor;
                                            colorImageBytes[pixel + 2] = (byte)blueColor;

                                        }
                                        else if (row % 2 == 0 && column % 2 != 0)
                                        {
                                            greenColor = buffer[pixel];
                                            redColor = ((buffer[row * GvspInfo.Width + (column - 1)]) + (buffer[row * GvspInfo.Width + (column + 1)])) / 2;
                                            blueColor = ((buffer[(row - 1) * GvspInfo.Width + column]) + (buffer[(row + 1) * GvspInfo.Width + column])) / 2;
                                            colorImageBytes[pixel] = (byte)greenColor;
                                            colorImageBytes[pixel + 1] = (byte)redColor;
                                            colorImageBytes[pixel + 2] = (byte)blueColor;

                                        }
                                        else if (row % 2 != 0 && column % 2 == 0)
                                        {
                                            greenColor = buffer[pixel];
                                            blueColor = ((buffer[row * GvspInfo.Width + (column - 1)]) + (buffer[row * GvspInfo.Width + (column + 1)])) / 2;
                                            redColor = ((buffer[(row - 1) * GvspInfo.Width + column]) + (buffer[(row + 1) * GvspInfo.Width + column])) / 2;
                                            colorImageBytes[pixel] = (byte)greenColor;
                                            colorImageBytes[pixel + 1] = (byte)redColor;
                                            colorImageBytes[pixel + 2] = (byte)blueColor;

                                        }
                                    }
                                }
                                FrameReady?.Invoke(totalPacketsInFrame, colorImageBytes);
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
                    int length = socketRxRaw.Receive(packet.Span);
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