namespace GigeVision.Core.Models
{
    /// <summary>
    /// Per-frame metadata extracted from the GVSP image leader packet.
    /// Available in <see cref="GigeVision.Core.Services.StreamReceiverBase.FrameReadyWithInfo"/>.
    /// </summary>
    public readonly struct GvspFrameInfo
    {
        /// <summary>
        /// Camera block ID (frame counter). Monotonically increasing; gaps indicate dropped frames.
        /// </summary>
        public ulong FrameId { get; }

        /// <summary>
        /// 64-bit hardware timestamp from the camera's free-running clock, in camera clock ticks.
        /// For Lucid cameras the clock runs at 1 GHz (1 tick = 1 ns).
        /// Convert to seconds: <c>Timestamp / 1e9</c>.
        /// </summary>
        public ulong Timestamp { get; }

        public GvspFrameInfo(ulong frameId, ulong timestamp)
        {
            FrameId = frameId;
            Timestamp = timestamp;
        }
    }
}
