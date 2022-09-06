using GigeVision.Core.Models;
using System;

namespace GigeVision.Core.Interfaces
{
    public interface IGvsp
    {
        int BufferSize { get; }
        EventHandler<byte[]> FrameReady { get; set; }
        GvspInfo GvspInfo { get; }
        uint Height { get; }
        bool IsDecodingAsVersion2 { get; set; }
        bool IsMulticast { get; }
        bool IsReceiving { get; }
        int MissingPacketTolerance { get; }
        string MulticastIP { get; }
        uint PayloadSize { get; }
        int PortRx { get; }
        string RxIP { get; }
        uint Width { get; }
        public int NetworkFps { get; set; }
        void SetPayloadSize(uint height, uint width, uint channels = 1);
        void StartRxThread();
    }
}