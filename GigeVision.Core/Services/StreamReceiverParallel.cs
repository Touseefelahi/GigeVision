using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace GigeVision.Core.Services
{
    public class StreamReceiverParallel : StreamReceiverBase
    {
        private int arrayIndex = 0;
        private Thread decoderThread;
        private byte[] globalBuffer;
        private List<string> info;
        private List<int> listOfPacketIDs;
        private volatile int offsetCopy, lengthCopy;
        private int packetCount = 20;
        private Stopwatch stopwatch;
        private List<TimeSpan> timeSpansReception, timeSpansWriter;
        private AutoResetEvent waitHandle;

        protected override async void Receiver()
        {
            listOfPacketIDs = new List<int>();
            waitHandle = new AutoResetEvent(false);
            timeSpansReception = new();
            timeSpansWriter = new();
            stopwatch = new();
            stopwatch.Start();
            DetectGvspType();
            info = new();
            globalBuffer = new byte[GvspInfo.PacketLength * packetCount];

            var readingPipe = ReceiverTask();
            // var decoderPipe = DecoderTask();
            await Task.WhenAll(readingPipe);
        }

        //private async Task DecoderTask()
        //{
        //    int i;
        //    byte[] buffer = GC.AllocateArray<byte>(length: GvspInfo.RawImageSize, pinned: true);
        //    int bufferIndex = 0;
        //    int packetID = 0, bufferStart;
        //    int packetRxCount = 0;
        //    var listOfPacketIDs = new List<int>();
        //    var packetSize = GvspInfo.PayloadSize + GvspInfo.PayloadOffset;
        //    int arrayIndexDecoder = 0;
        //    while (IsReceiving)
        //    {
        //        waitHandle.WaitOne();
        //        arrayIndexDecoder = arrayIndex == 1 ? 0 : 1;
        //        Trace.WriteLine($"{arrayIndexDecoder} {stopwatch.ElapsedTicks}");
        //        timeSpansWriter.Add(stopwatch.Elapsed);
        //        for (i = 0; i < buffers[arrayIndexDecoder].Length - packetSize; i += packetSize)
        //        {
        //            var singlePacket = buffers[arrayIndexDecoder].AsMemory().Slice(i, packetSize);
        //            if (singlePacket.Span.Slice(4, 1)[0] == GvspInfo.DataIdentifier) //Packet
        //            {
        //                packetRxCount++;
        //                packetID = (singlePacket.Span.Slice(GvspInfo.PacketIDIndex, 1)[0] << 8) | singlePacket.Span.Slice(GvspInfo.PacketIDIndex + 1, 1)[0];
        //                bufferStart = (packetID - 1) * GvspInfo.PayloadSize; //This use buffer length of regular packet
        //                listOfPacketIDs.Add(packetID);
        //                //singlePacket.Slice(GvspInfo.PayloadOffset, GvspInfo.PayloadSize).CopyTo(buffer.AsSpan().Slice(bufferStart, GvspInfo.PayloadSize));
        //            }
        //            if (packetID == GvspInfo.FinalPacketID)
        //            {
        //                if (packetID - packetRxCount < MissingPacketTolerance)
        //                {
        //                    //  FrameReady?.Invoke((ulong)1, buffer);
        //                    bufferIndex = bufferIndex == 1 ? 0 : 1;
        //                    //listOfPacketIDs.Clear();
        //                }
        //                packetRxCount = 0;
        //            }
        //        }
        //        waitHandle.Reset();
        //    }
        //}
        private void ProcessPackets(int startIndex, int length)
        {
            int i;
            int bufferIndex = 0;
            int packetID = 0, bufferStart;
            int packetRxCount = 0;
            info.Add($"P {startIndex} : {length}");
            for (i = startIndex; i < length - GvspInfo.PacketLength; i += GvspInfo.PacketLength)
            {
                if (globalBuffer[i + 4] == GvspInfo.DataIdentifier) //Packet
                {
                    packetRxCount++;
                    packetID = (globalBuffer[i + GvspInfo.PacketIDIndex] << 8) | globalBuffer[i + GvspInfo.PacketIDIndex + 1];
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
                        //listOfPacketIDs.Clear();
                    }
                    packetRxCount = 0;
                }
            }
        }

        private async Task ReceiverTask()
        {
            int count = 0;
            int localByteCounter = 0;
            int size = GvspInfo.PacketLength;
            Memory<byte> memoryList = new Memory<byte>(globalBuffer);
            var listMissingPacketIDs = new List<int>();
            await Task.Delay(2);
            int previousID = 0;
            int midValue = globalBuffer.Length / 2;
            int offset = 0;
            int offsetCopy, lengthCopy;
            try
            {
                while (IsReceiving)
                {
                    int length = socketRxRaw.Receive(memoryList.Span.Slice(offset + localByteCounter, size));
                    timeSpansReception.Add(stopwatch.Elapsed);
                    if (length > 100)
                    {
                        int packetID = (globalBuffer[offset + localByteCounter + GvspInfo.PacketIDIndex] << 8) | (globalBuffer[offset + localByteCounter + GvspInfo.PacketIDIndex + 1]);
                        if (packetID - previousID != 1)
                        {
                            listMissingPacketIDs.Add(packetID);
                        }
                        previousID = packetID;
                        if (packetID == GvspInfo.FinalPacketID)
                        {
                            previousID = 0;
                        }
                        localByteCounter += length;
                        if (count++ >= packetCount / 2 - 1)
                        {
                            count = 0;

                            offsetCopy = offset;
                            lengthCopy = localByteCounter;
                            info.Add($"R {offsetCopy} : {lengthCopy}");
                            ProcessPackets(offsetCopy, lengthCopy);

                            localByteCounter = 0;
                            if (offset == 0)
                            {
                                offset = midValue;
                            }
                            else
                            {
                                offset = 0;
                            }
                            //waitHandle.Set();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}