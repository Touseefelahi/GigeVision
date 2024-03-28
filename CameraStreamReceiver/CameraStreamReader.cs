using GigeVision.Core.Services;
using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GigeVision.Core.Enums;
using GigeVision.Core;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.VisualBasic;

namespace CameraStreamReceiver
{
    public class CameraStreamReader : StreamReceiverBase
    {
        private string SharedMemoryName = "CameraStreamSharedMemory";
        private string InfoSharedMemoryName = "CameraStreamInfoSharedMemory";
        private int SharedMemorySize = int.MaxValue;
        private string CameraStreamReaderSignalKey = "CameraStreamReaderSignalKey";
        private string CameraStreamDisplaySignalKey = "CameraStreamDisplaySignalKey";
        private EventWaitHandle CameraStreamReaderSignal;
        private EventWaitHandle CameraStreamDisplaySignal;
        private int packetId = 0;
        private long globalDisplayBufferCounter = 0;
        private long globalReadBufferCounter = 0;

        public async Task Receiver()
        {
            MemoryMappedFile infoSharedMemory = MemoryMappedFile.CreateNew(InfoSharedMemoryName, SharedMemorySize);
            MemoryMappedFile sharedMemory = MemoryMappedFile.CreateNew(SharedMemoryName, SharedMemorySize);
            CameraStreamReaderSignal = new EventWaitHandle(false, EventResetMode.AutoReset, CameraStreamReaderSignalKey);
            CameraStreamDisplaySignal = new EventWaitHandle(false, EventResetMode.AutoReset, CameraStreamDisplaySignalKey);

            CameraStreamReaderSignal.WaitOne();
            SetupSocketAndGvspInfo(infoSharedMemory);

            //Task.Run(() => GetCameraStreamDisplayBufferCount());
            await StreamReceiver(sharedMemory);
        }

        public async Task StreamReceiver(MemoryMappedFile sharedMemory)
        {
            using (MemoryMappedViewAccessor accessor = sharedMemory.CreateViewAccessor())
            {
                int i = 1;
                var framesCount = 4;
                var frameOffset = 0;
                byte[] singlePacket = new byte[GvspInfo.PacketLength];
                while (true)
                {
                    try
                    {
                        socketRxRaw.Receive(singlePacket);
                        packetId = (singlePacket[GvspInfo.PacketIDIndex] << 8) | singlePacket[GvspInfo.PacketIDIndex + 1];
                        accessor.WriteArray((packetId * GvspInfo.PacketLength) + frameOffset, singlePacket, 0, singlePacket.Length);
                        if (packetId == GvspInfo.FinalPacketID)
                        {
                            frameOffset = packetId * GvspInfo.PacketLength * i;
                            globalReadBufferCounter++;
                            CameraStreamReaderSignal.Set();
                            i++;
                        }

                        if (i > framesCount)
                        {
                            i = 1;
                            frameOffset = 0;
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        //public void GetCameraStreamDisplayBufferCount()
        //{
        //    while (true)
        //    {
        //        CameraStreamDisplaySignal.WaitOne();
        //        globalDisplayBufferCounter++;
        //    }
        //}

        public void SetupSocketAndGvspInfo(MemoryMappedFile infoSharedMemory)
        {
            using (MemoryMappedViewAccessor accessor = infoSharedMemory.CreateViewAccessor())
            {
                try
                {
                    byte[] byteArray = new byte[100];
                    accessor.ReadArray(0, byteArray, 0, byteArray.Length);
                    string jsonString = Encoding.UTF8.GetString(byteArray);
                    jsonString = jsonString.Replace("\0", "");
                    string[] readerInfo = JsonSerializer.Deserialize<string[]>(jsonString);

                    IPAddress iPAddress = IPAddress.Parse(readerInfo[0]);
                    PortRx = Int32.Parse(readerInfo[1]);
                    GvspInfo.PacketLength = Int32.Parse(readerInfo[2]);
                    GvspInfo.PacketIDIndex = Int32.Parse(readerInfo[3]);
                    GvspInfo.FinalPacketID = Int32.Parse(readerInfo[4]);

                    socketRxRaw = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socketRxRaw.Bind(new IPEndPoint(iPAddress, PortRx));
                    socketRxRaw.ReceiveTimeout = 1000;
                    socketRxRaw.ReceiveBufferSize = (int)(1920 * 1100);
                }
                catch (Exception ex)
                {

                }
            }
        }
    }
}
