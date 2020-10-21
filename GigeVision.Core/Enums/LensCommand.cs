namespace GigeVision.Core.Enums
{
    /// <summary>
    /// General Lens commands
    /// </summary>
    public enum LensCommand
    {
        /// <summary>
        /// Zoom in
        /// </summary>
        ZoomIn,
        /// <summary>
        /// Zoom out
        /// </summary>
        ZoomOut,
        /// <summary>
        /// Zoom stop for continous zoom lens
        /// </summary>
        ZoomStop,
        /// <summary>
        /// Set zoom speed
        /// </summary>
        ZoomSpeed,
        /// <summary>
        /// Set particular zoom value
        /// </summary>
        ZoomValue,
        /// <summary>
        /// Focus far
        /// </summary>
        FocusFar,
        /// <summary>
        /// Focus Stop        
        /// </summary>
        FocusNear,
        /// <summary>
        /// Focus stop for continous focus
        /// </summary>
        FocusStop,
        /// <summary>
        /// Set focus speed
        /// </summary>
        FocusSpeed,
        /// <summary>
        /// Set to auto focus
        /// </summary>
        FocusAuto,
        /// <summary>
        /// Open iris
        /// </summary>
        IrisOpen,
        /// <summary>
        /// Clos iris
        /// </summary>
        IrisClose,
        /// <summary>
        /// Stop iris motor for continous iris
        /// </summary>
        IrisStop,
        /// <summary>
        /// Set iris speed
        /// </summary>
        IrisSpeed,
        /// <summary>
        /// Set Auto Iris
        /// </summary>
        IrisAuto,
        /// <summary>
        /// Set particular focus value
        /// </summary>
        FocusValue,
    }
}