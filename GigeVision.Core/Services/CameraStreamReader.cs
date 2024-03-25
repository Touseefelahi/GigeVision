using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GigeVision.Core.Services
{
    public class CameraStreamReader : StreamReceiverBase
    {
        private string SharedMemoryName = "CameraStreamSharedMemory";
        private int SharedMemorySize = int.MaxValue;
        private string CameraStreamReaderSignalKey = "CameraStreamReaderSignalKey";
        private string CameraStreamDisplaySignalKey = "CameraStreamDisplaySignalKey";
        private EventWaitHandle CameraStreamReaderSignal;
        private EventWaitHandle CameraStreamDisplaySignal;
        private int packetId = 0;
        private long globalDisplayBufferCounter = 0;
        private long globalReadBufferCounter = 0;

        protected override async void Receiver()
        {
            DetectGvspType();
            MemoryMappedFile sharedMemory = MemoryMappedFile.CreateNew(SharedMemoryName, SharedMemorySize);
            CameraStreamReaderSignal = new EventWaitHandle(false, EventResetMode.AutoReset, CameraStreamReaderSignalKey);
            CameraStreamDisplaySignal = new EventWaitHandle(false, EventResetMode.AutoReset, CameraStreamDisplaySignalKey);

            Task.Run(() => ReadCameraStreamAndWriteToSharedMemory(sharedMemory));
            Task.Run(() => GetCameraStreamDisplayBufferCount());
            Task.Run(() => new CameraStreamDisplay(GvspInfo, FrameReady));
        }

        public void ReadCameraStreamAndWriteToSharedMemory(MemoryMappedFile sharedMemory)
        {
            using (MemoryMappedViewAccessor accessor = sharedMemory.CreateViewAccessor())
            {
                try
                {
                    int i = 0;
                    byte[] singlePacket = new byte[GvspInfo.PacketLength];
                    while (IsReceiving)
                    {
                        i++;
                        int length = socketRxRaw.Receive(singlePacket);
                        packetId = (singlePacket[GvspInfo.PacketIDIndex] << 8) | singlePacket[GvspInfo.PacketIDIndex + 1];
                        accessor.WriteArray(i * GvspInfo.PacketLength, singlePacket, 0, singlePacket.Length);
                        CameraStreamReaderSignal.Set();
                        globalReadBufferCounter++;
                        if (i >= GvspInfo.FinalPacketID)
                        {
                            i = 0;
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

        public void GetCameraStreamDisplayBufferCount()
        {
            while (true)
            {
                CameraStreamDisplaySignal.WaitOne();
                globalDisplayBufferCounter++;
            }
        }
    }
}
