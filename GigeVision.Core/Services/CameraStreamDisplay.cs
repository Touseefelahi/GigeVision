using GigeVision.Core.Models;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GigeVision.Core.Services
{
    public class CameraStreamDisplay : StreamReceiverBase
    {
        private string SharedMemoryName = "CameraStreamSharedMemory";
        private string InfoSharedMemoryName = "CameraStreamInfoSharedMemory";
        private int SharedMemorySize = int.MaxValue;
        private string CameraStreamReaderSignalKey = "CameraStreamReaderSignalKey";
        private string CameraStreamDisplaySignalKey = "CameraStreamDisplaySignalKey";
        private EventWaitHandle CameraStreamReaderSignal;
        private EventWaitHandle CameraStreamDisplaySignal;
        private int packetSize;
        private long globalDisplayBufferCounter = 0;
        private long globalReadBufferCounter = 0;

        protected override async void Receiver()
        {
            DetectGvspType();
            MemoryMappedFile infoSharedMemory = MemoryMappedFile.OpenExisting(InfoSharedMemoryName);
            MemoryMappedFile sharedMemory = MemoryMappedFile.OpenExisting(SharedMemoryName);
            CameraStreamReaderSignal = EventWaitHandle.OpenExisting(CameraStreamReaderSignalKey);
            CameraStreamDisplaySignal = EventWaitHandle.OpenExisting(CameraStreamDisplaySignalKey);

            WriteGvspInfoToSharedMemory(infoSharedMemory);
            CameraStreamReaderSignal.Set();

            packetSize = GvspInfo.PayloadSize + GvspInfo.PayloadOffset;
            var packetsBuffer = new byte[GvspInfo.FinalPacketID * packetSize];
            var displayBuffer = new byte[GvspInfo.FinalPacketID * packetSize];

            Task.Run(() => DisplayCameraStreamFromSharedMemory(sharedMemory, displayBuffer, packetsBuffer));
            Task.Run(() => GetReaderBufferCounter());
        }

        public void DisplayCameraStreamFromSharedMemory(MemoryMappedFile sharedMemory, byte[] displayBuffer, byte[] packetsBuffer)
        {
            var i = 1;
            var frameIndex = 1;
            var framesCount = 4;
            var frameOffset = 0;
            var packetId = 0;
            var totalPacketsInFrame = 0;
            int startPos = 0;
            int pixel = 0;
            var greenColor = 0;
            var redColor = 0;
            var blueColor = 0;
            byte[] colorImageBytes = new byte[GvspInfo.FinalPacketID * packetSize * 3];

            using (MemoryMappedViewAccessor accessor = sharedMemory.CreateViewAccessor())
            {
                var singlePacket = new byte[GvspInfo.PacketLength];
                while (true)
                {
                    try
                    {
                        if (globalReadBufferCounter < globalDisplayBufferCounter)
                        {
                            continue;
                        }

                        accessor.ReadArray((i * GvspInfo.PacketLength) + frameOffset, singlePacket, 0, singlePacket.Length);
                        int packetLength = singlePacket.Length;
                        if (singlePacket[4] == GvspInfo.DataIdentifier)
                        {
                            i++;
                            totalPacketsInFrame++;
                            packetId = (singlePacket[GvspInfo.PacketIDIndex] << 8 | singlePacket[GvspInfo.PacketIDIndex + 1]);
                            startPos = (packetId - 1) * GvspInfo.PayloadSize;
                            singlePacket.AsSpan().Slice(GvspInfo.PayloadOffset, packetLength - GvspInfo.PayloadOffset).CopyTo(packetsBuffer.AsSpan().Slice(startPos, packetLength));
                        }

                        if (i == GvspInfo.FinalPacketID)
                        {
                            i++;
                            displayBuffer = packetsBuffer;
                            if (packetId - totalPacketsInFrame <= 2)
                            {
                                Task.Run(() =>
                                {
                                    //for (int row = 1; row < GvspInfo.Height - 1; row++)
                                    //{
                                    //    for (int column = 1; column < GvspInfo.Width - 1; column += 3)
                                    //    {
                                    //        pixel = row * GvspInfo.Width + column;
                                    //        if (row % 2 == 0 && column % 2 == 0)
                                    //        {
                                    //            redColor = displayBuffer[pixel];
                                    //            greenColor = ((displayBuffer[row * GvspInfo.Width + (column - 1)]) + (displayBuffer[row * GvspInfo.Width + (column + 1)])
                                    //                          + (displayBuffer[(row - 1) * GvspInfo.Width + (column)]) + (displayBuffer[(row + 1) * GvspInfo.Width + column])) / 4;
                                    //            blueColor = ((displayBuffer[(row - 1) * GvspInfo.Width + (column - 1)]) + (displayBuffer[(row - 1) * GvspInfo.Width + (column + 1)])
                                    //                          + (displayBuffer[(row + 1) * GvspInfo.Width + (column - 1)]) + (displayBuffer[(row + 1) * GvspInfo.Width + (column + 1)])) / 4;
                                    //            colorImageBytes[pixel] = (byte)greenColor;
                                    //            colorImageBytes[pixel + 1] = (byte)redColor;
                                    //            colorImageBytes[pixel + 2] = (byte)blueColor;

                                    //        }
                                    //        else if (row % 2 != 0 && column % 2 != 0)
                                    //        {
                                    //            blueColor = displayBuffer[pixel];
                                    //            greenColor = ((displayBuffer[row * GvspInfo.Width + (column - 1)]) + (displayBuffer[row * GvspInfo.Width + (column + 1)])
                                    //                          + (displayBuffer[(row - 1) * GvspInfo.Width + (column)]) + (displayBuffer[(row + 1) * GvspInfo.Width + column])) / 4;
                                    //            redColor = ((displayBuffer[(row - 1) * GvspInfo.Width + (column - 1)]) + (displayBuffer[(row - 1) * GvspInfo.Width + (column + 1)])
                                    //                          + (displayBuffer[(row + 1) * GvspInfo.Width + (column - 1)]) + (displayBuffer[(row + 1) * GvspInfo.Width + (column + 1)])) / 4;
                                    //            colorImageBytes[pixel] = (byte)greenColor;
                                    //            colorImageBytes[pixel + 1] = (byte)redColor;
                                    //            colorImageBytes[pixel + 2] = (byte)blueColor;

                                    //        }
                                    //        else if (row % 2 == 0 && column % 2 != 0)
                                    //        {
                                    //            greenColor = displayBuffer[pixel];
                                    //            redColor = ((displayBuffer[row * GvspInfo.Width + (column - 1)]) + (displayBuffer[row * GvspInfo.Width + (column + 1)])) / 2;
                                    //            blueColor = ((displayBuffer[(row - 1) * GvspInfo.Width + column]) + (displayBuffer[(row + 1) * GvspInfo.Width + column])) / 2;
                                    //            colorImageBytes[pixel] = (byte)greenColor;
                                    //            colorImageBytes[pixel + 1] = (byte)redColor;
                                    //            colorImageBytes[pixel + 2] = (byte)blueColor;

                                    //        }
                                    //        else if (row % 2 != 0 && column % 2 == 0)
                                    //        {
                                    //            greenColor = displayBuffer[pixel];
                                    //            blueColor = ((displayBuffer[row * GvspInfo.Width + (column - 1)]) + (displayBuffer[row * GvspInfo.Width + (column + 1)])) / 2;
                                    //            redColor = ((displayBuffer[(row - 1) * GvspInfo.Width + column]) + (displayBuffer[(row + 1) * GvspInfo.Width + column])) / 2;
                                    //            colorImageBytes[pixel] = (byte)greenColor;
                                    //            colorImageBytes[pixel + 1] = (byte)redColor;
                                    //            colorImageBytes[pixel + 2] = (byte)blueColor;
                                    //        }
                                    //    }
                                    //}
                                    FrameReady?.Invoke(totalPacketsInFrame, displayBuffer);
                                    totalPacketsInFrame = 0; ;
                                    globalDisplayBufferCounter++;
                                    CameraStreamDisplaySignal.Set();
                                });

                                frameOffset = packetId * GvspInfo.PacketLength * frameIndex;
                                frameIndex++;
                            }
                        }

                        if (i >= GvspInfo.FinalPacketID)
                        {
                            i = 1;
                        }

                        if (frameIndex > framesCount)
                        {
                            frameIndex = 1;
                            frameOffset = 0;
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        public void GetReaderBufferCounter()
        {
            while (true)
            {
                CameraStreamReaderSignal.WaitOne();
                globalReadBufferCounter++;
            }
        }

        public void WriteGvspInfoToSharedMemory(MemoryMappedFile infoSharedMemory)
        {
            using (MemoryMappedViewAccessor accessor = infoSharedMemory.CreateViewAccessor())
            {
                var gvspInfo = new string[5];
                gvspInfo[0] = RxIP.ToString();
                gvspInfo[1] = PortRx.ToString();
                gvspInfo[2] = GvspInfo.PacketLength.ToString();
                gvspInfo[3] = GvspInfo.PacketIDIndex.ToString();
                gvspInfo[4] = GvspInfo.FinalPacketID.ToString();

                string jsonString = JsonSerializer.Serialize(gvspInfo);
                byte[] jsonData = Encoding.UTF8.GetBytes(jsonString);
                accessor.WriteArray(0, jsonData, 0, jsonData.Length);
            }
        }
    }
}
