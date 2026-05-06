using GenICam;
using GigeVision.Core.Enums;
using GigeVision.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GigeVision.Core.Interfaces
{
    /// <summary>
    /// Camera class is responsible to initialize the stream and receive the stream
    /// </summary>
    public interface ICamera
    {
        /// <summary>
        /// If enabled library will use C++ native library for Stream Reception
        /// </summary>
        bool IsUsingCppForRx { get; set; }

        /// <summary>
        /// Motor Controller for camera, zoom/focus/iris control if any
        /// </summary>
        MotorControl MotorController { get; set; }

        /// <summary>
        /// Multi-cast IP: it will be applied only when IsMulticast Property is true
        /// </summary>
        string MulticastIP { get; set; }

        /// <summary>
        /// Receivers IP. Once the stream is on camera will stream to this IP address. It will be
        /// automatically fetch the system IP if not set
        /// </summary>
        string RxIP { get; set; }

        /// <summary>
        /// Multi-cast Option
        /// </summary>
        bool IsMulticast { get; set; }

        /// <summary>
        /// GVCP controller
        /// </summary>
        IGvcp Gvcp { get; }

        /// <summary>
        /// Rx port for GVSP Stream. If not set software will take any available port
        /// </summary>
        int PortRx { get; set; }

        /// <summary>
        /// Event for frame ready
        /// </summary>
        EventHandler<byte[]> FrameReady { get; set; }

        /// <summary>
        /// Fired alongside <see cref="FrameReady"/> with per-frame metadata
        /// (hardware timestamp and frame ID) from the GVSP image leader.
        /// </summary>
        EventHandler<GvspFrameInfo> FrameReadyWithInfo { get; set; }

        /// <summary>
        /// Event for general updates
        /// </summary>
        EventHandler<string> Updates { get; set; }

        /// <summary>
        /// Payload size, if not provided it will be automatically set to one row, depending on resolution
        /// </summary>
        uint Payload { get; set; }

        /// <summary>
        /// Optional inter-packet delay written to GevSCPD before starting the stream.
        /// </summary>
        uint StreamPacketDelay { get; set; }

        /// <summary>
        /// Tolerance for missing packet
        /// </summary>
        public int MissingPacketTolerance { get; set; }

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
        /// Human-readable pixel format name. Returns the registry name for vendor-specific
        /// formats (e.g. "QOI_BayerRG8"), the enum name for standard PFNC formats,
        /// or a hex string (e.g. "0x81080B99") for completely unknown values.
        /// </summary>
        string PixelFormatName { get; }

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
        /// If we set the external buffer using <see cref="SetBuffer(byte[])"/> this will be set
        /// true and software will copy stream on this buffer
        /// </summary>
        bool IsUsingExternalBuffer { get; set; }


        /// <summary>
        /// This method will get current PC IP and Gets the Camera IP from Gvcp
        /// </summary>
        /// <param name="rxIP">If rxIP is not provided, method will detect system IP and use it</param>
        /// <param name="rxPort">It will set randomly when not provided</param>
        /// <returns></returns>
        Task<bool> StartStreamAsync(string? rxIP = null, int rxPort = 0);

        /// <summary>
        /// Stops the camera stream and leave camera control
        /// </summary>
        /// <returns>Is streaming status</returns>
        Task<bool> StopStream();

        /// <summary>
        /// Gets the value of an integer-backed parameter.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>The parameter value when available.</returns>
        Task<long?> GetParameterValue(string parameterName);

        /// <summary>
        /// Gets the minimum of an integer-backed parameter.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>The minimum value when available.</returns>
        Task<long> GetParameterMinValue(string parameterName);

        /// <summary>
        /// Gets the maximum of an integer-backed parameter.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>The maximum value when available.</returns>
        Task<long> GetParameterMaxValue(string parameterName);

        /// <summary>
        /// Sets an integer-backed parameter.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>True when the write succeeds.</returns>
        Task<bool> SetCameraParameter(string parameterName, long value);

        /// <summary>
        /// Sets a floating-point or converter-backed parameter.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>True when the write succeeds.</returns>
        Task<bool> SetCameraParameter(string parameterName, double value);

        /// <summary>
        /// Sets a boolean parameter.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>True when the write succeeds.</returns>
        Task<bool> SetCameraParameter(string parameterName, bool value);

        /// <summary>
        /// Gets the value of a parameter using the requested CLR type.
        /// </summary>
        /// <typeparam name="T">The requested value type.</typeparam>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>The parameter value when available.</returns>
        Task<T?> GetParameterValue<T>(string parameterName) where T : struct;

        /// <summary>
        /// Gets the value of a floating-point or converter-backed parameter.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>The parameter value when available.</returns>
        [Obsolete("Use GetParameterValue<double>(parameterName) instead.")]
        Task<double?> GetFloatParameterValue(string parameterName);

        /// <summary>
        /// Gets the minimum of a floating-point or converter-backed parameter.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>The minimum value when available.</returns>
        Task<double> GetFloatParameterMinValue(string parameterName);

        /// <summary>
        /// Gets the maximum of a floating-point or converter-backed parameter.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>The maximum value when available.</returns>
        Task<double> GetFloatParameterMaxValue(string parameterName);

        /// <summary>
        /// Sets a floating-point or converter-backed parameter.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>True when the write succeeds.</returns>
        [Obsolete("Use SetCameraParameter(parameterName, value) instead.")]
        Task<bool> SetFloatParameter(string parameterName, double value);

        /// <summary>
        /// Gets the value of a boolean parameter.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>The boolean value when available.</returns>
        [Obsolete("Use GetParameterValue<bool>(parameterName) instead.")]
        Task<bool?> GetBooleanParameterValue(string parameterName);

        /// <summary>
        /// Sets a boolean parameter.
        /// </summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>True when the write succeeds.</returns>
        [Obsolete("Use SetCameraParameter(parameterName, value) instead.")]
        Task<bool> SetBooleanParameter(string parameterName, bool value);

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

        /// <summary>
        /// Set External Raw bytes buffer
        /// </summary>
        /// <param name="externalBuffer"></param>
        void SetBuffer(byte[] externalBuffer);

        /// <summary>
        /// It reads all the parameters from the camera
        /// </summary>
        /// <returns></returns>
        Task<bool> SyncParameters(int syncAttempts = 1);
    }
}