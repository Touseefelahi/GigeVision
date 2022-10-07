using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace GigeVision.Core.Services
{
    public class StreamReceiverPipeLine : StreamReceiverBase
    {
        private Stopwatch stopwatch;
        private List<TimeSpan> timeSpansReception, timeSpansWriter;

        protected override async void Receiver()
        {
            timeSpansReception = new();
            timeSpansWriter = new();
            stopwatch = new();
            stopwatch.Start();
            DetectGvspType();
            var options = new PipeOptions(minimumSegmentSize: 9000);
            var decodingPipeline = new Pipe(options);

            var readingPipe = ReceiverPipe(socketRxRaw, decodingPipeline.Writer);
            var decoderPipe = DecoderPipe(decodingPipeline.Reader);
            await Task.WhenAll(readingPipe, decoderPipe);
        }

        private async Task DecoderPipe(PipeReader reader)
        {
            int i;
            byte[] buffer = GC.AllocateArray<byte>(length: GvspInfo.RawImageSize, pinned: true);
            int bufferIndex = 0;
            int packetID = 0, bufferStart;
            int packetRxCount = 0;
            var listOfPacketIDs = new List<int>();
            var packetSize = GvspInfo.PayloadSize + GvspInfo.PayloadOffset;
            SequencePosition sequencePosition;
            while (IsReceiving)
            {
                ReadResult dataBuffer = await reader.ReadAsync();
                timeSpansWriter.Add(stopwatch.Elapsed);
                var packetIDs = dataBuffer.Buffer.Length / GvspInfo.PayloadSize;
                for (i = 0; i < dataBuffer.Buffer.Length - packetSize; i += packetSize)
                {
                    var singlePacket = dataBuffer.Buffer.Slice(i, packetSize);
                    if (singlePacket.FirstSpan.Slice(4, 1)[0] == GvspInfo.DataIdentifier) //Packet
                    {
                        packetRxCount++;
                        packetID = (singlePacket.FirstSpan.Slice(GvspInfo.PacketIDIndex, 1)[0] << 8) | singlePacket.FirstSpan.Slice(GvspInfo.PacketIDIndex + 1, 1)[0];
                        bufferStart = (packetID - 1) * GvspInfo.PayloadSize; //This use buffer length of regular packet
                        listOfPacketIDs.Add(packetID);
                        //singlePacket.Slice(GvspInfo.PayloadOffset, GvspInfo.PayloadSize).CopyTo(buffer.AsSpan().Slice(bufferStart, GvspInfo.PayloadSize));
                    }
                    if (packetID == GvspInfo.FinalPacketID)
                    {
                        if (packetID - packetRxCount < MissingPacketTolerance)
                        {
                            //  FrameReady?.Invoke((ulong)1, buffer);
                            bufferIndex = bufferIndex == 1 ? 0 : 1;
                            listOfPacketIDs.Clear();
                        }
                        packetRxCount = 0;
                    }
                }
                sequencePosition = dataBuffer.Buffer.GetPosition(dataBuffer.Buffer.Length);
                reader.AdvanceTo(sequencePosition);
            }
        }

        private async Task ReceiverPipe(Socket socketRxRaw, PipeWriter writer)
        {
            int count = 0;
            while (IsReceiving)
            {
                var spanPacket = writer.GetMemory(9000);
                int length = socketRxRaw.Receive(spanPacket.Span);
                timeSpansReception.Add(stopwatch.Elapsed);
                //int length = await socketRxRaw.ReceiveAsync(spanPacket, SocketFlags.None);
                if (length > 100)
                {
                    writer.Advance(length);
                    if (count++ >= 8)
                    {
                        await writer.FlushAsync();
                        count = 0;
                    }
                }
            }
        }
    }
}