using GigeVision.Core.Enums;
using GigeVision.Core.Models;
using System;
using System.Threading.Tasks;

namespace GigeVision.Core.Interfaces
{
    /// <summary>
    /// Camera class is responsible to initilize the stream and receive the stream
    /// </summary>
    public interface ICamera
    {
        /// <summary>
        /// Motor Controller for camera, zoom/focus/iris control if any
        /// </summary>
        MotorControl MotorController { get; set; }

        /// <summary>
        /// Multicast IP: it will be applied only when IsMulticast Property is true
        /// </summary>
        string MulticastIP { get; set; }

        /// <summary>
        /// Multicast Option
        /// </summary>
        bool IsMulticast { get; set; }

        /// <summary>
        /// GVCP controller
        /// </summary>
        IGvcp Gvcp { get; }

        /// <summary>
        /// Event for frame ready
        /// </summary>
        EventHandler<byte[]> FrameReady { get; set; }

        /// <summary>
        /// Event for general updates
        /// </summary>
        EventHandler<string> Updates { get; set; }

        /// <summary>
        /// Payload size, if not provided it will be automatically set to one row, depending on resolution
        /// </summary>
        uint Payload { get; set; }

        /// <summary>
        /// Camera width
        /// </summary>
        uint Width { get; set; }

        /// <summary>
        /// Camera height
        /// </summary>
        uint Height { get; set; }

        /// <summary>
        /// Camera offset X
        /// </summary>
        uint OffsetX { get; set; }

        /// <summary>
        /// Camera offset Y
        /// </summary>
        uint OffsetY { get; set; }

        /// <summary>
        /// Camera Pixel Format
        /// </summary>
        PixelFormat PixelFormat { get; set; }

        /// <summary>
        /// Device IP
        /// </summary>
        string IP { get; set; }

        /// <summary>
        /// Camera stream status
        /// </summary>
        bool IsStreaming { get; set; }

        /// <summary>
        /// Gets the raw data from the camera. Set false to get RGB frame instead of BayerGR8
        /// </summary>
        bool IsRawFrame { get; set; }

        /// <summary>
        /// This method will get current PC IP and Gets the Camera ip from Gvcp
        /// </summary>
        /// <param name="rxIP">If rxIP is not provided, method will detect system IP and use it</param>
        /// <param name="rxPort">It will set randomly when not provided</param>
        /// <param name="frameReady">If not Null this action will be called on frameready</param>
        /// <returns></returns>
        Task<bool> StartStreamAsync(string rxIP = null, int rxPort = 0, Action<byte[]> frameReady = null);

        /// <summary>
        /// Stops the camera stream and leave camera control
        /// </summary>
        /// <returns>Is streaming status</returns>
        Task<bool> StopStream();

        /// <summary>
        /// Sets the Resolution
        /// </summary>
        /// <returns></returns>
        Task<bool> SetResolutionAsync();

        /// <summary>
        /// Sets the Offset
        /// </summary>
        /// <returns></returns>
        Task<bool> SetOffsetAsync();

        /// <summary>
        /// Sets the resolution of camera
        /// </summary>
        /// <param name="width">Width to set</param>
        /// <param name="height">Height to set</param>
        /// <returns>Command Status</returns>
        Task<bool> SetResolutionAsync(uint width, uint height);

        /// <summary>
        /// Sets the resolution of camera
        /// </summary>
        /// <param name="width">Width to set</param>
        /// <param name="height">Height to set</param>
        /// <returns>Command Status</returns>
        Task<bool> SetResolutionAsync(int width, int height);

        /// <summary>
        /// Sets the offset of camera
        /// </summary>
        /// <param name="offsetX">Offset X to set</param>
        /// <param name="offsetY">Offset Y to set</param>
        /// <returns>Command Status</returns>
        Task<bool> SetOffsetAsync(int offsetX, int offsetY);

        /// <summary>
        /// Sets the offset of camera
        /// </summary>
        /// <param name="offsetX">Offset X to set</param>
        /// <param name="offsetY">Offset Y to set</param>
        /// <returns>Command Status</returns>
        Task<bool> SetOffsetAsync(uint offsetX, uint offsetY);

        /// <summary>
        /// Bridge Command for motor controller, controls focus/zoom/iris operation
        /// </summary>
        /// <param name="command">Command to set</param>
        /// <param name="value">Value to set (Applicable for ZoomValue/FocusValue)</param>
        /// <returns>Command Status</returns>
        Task<bool> MotorControl(LensCommand command, uint value = 0);

        /// <summary>
        /// Read register for camera
        /// </summary>
        /// <returns>Command Status</returns>
        Task<bool> ReadRegisters();
    }
}