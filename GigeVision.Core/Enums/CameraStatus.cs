namespace GigeVision.Core.Enums
{
    /// <summary>
    /// GigE Camera status
    /// </summary>
    public enum CameraStatus
    {
        /// <summary>
        /// Camera Available in network and its not in Control/Streaming
        /// </summary>
        Available,

        /// <summary>
        /// Camera available in network and its in control
        /// </summary>
        InControl,

        /// <summary>
        /// Camera not found in network
        /// </summary>
        UnAvailable
    }
}