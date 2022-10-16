using GigeVision.Core.Models;
using System;

namespace GigeVision.Core.Interfaces
{
    /// <summary>
    /// General interface for stream reception
    /// </summary>
    public interface IStreamReceiver
    {
        /// <summary>
        /// Event for frame ready
        /// </summary>
        EventHandler<byte[]> FrameReady { get; set; }

        /// <summary>
        /// GVSP info for image info
        /// </summary>
        GvspInfo GvspInfo { get; }

        /// <summary>
        /// Is multicast enabled
        /// </summary>
        bool IsMulticast { get; set; }

        /// <summary>
        /// Is listening to receive the stream
        /// </summary>
        bool IsReceiving { get; set; }

        /// <summary>
        /// Missing packet tolerance, if we lost more than this many packets then the frame will be skipped
        /// </summary>
        int MissingPacketTolerance { get; set; }

        /// <summary>
        /// Multicast IP, only used if Multicasting is enabled by setting <see cref="IsMulticast"/> as true
        /// </summary>
        string MulticastIP { get; set; }

        /// <summary>
        /// Receiver port
        /// </summary>
        int PortRx { get; set; }

        /// <summary>
        /// RX IP, required for multicast group
        /// </summary>
        string RxIP { get; set; }

        /// <summary>
        /// General update event
        /// </summary>
        EventHandler<string> Updates { get; set; }

        /// <summary>
        /// Start reception thread
        /// </summary>
        void StartRxThread();

        /// <summary>
        /// Stop reception thread
        /// </summary>
        void StopReception();
    }
}