using GigeVision.Core.Enums;
using GigeVision.Core.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using GenICam;
using GigeVision.Core.Models;
using Converter = GigeVision.Core.Models.Converter;

namespace GigeVision.Core.Services
{
    /// <summary>
    /// Camera class is responsible to initialize the stream and receive the stream
    /// </summary>
    public class Camera : BaseNotifyPropertyChanged, ICamera
    {
        /// <summary>
        /// frame ready action
        /// </summary>
        public Action<byte[]> frameReadyAction;

        /// <summary>
        /// External buffer it has to set from externally using <see cref="SetBuffer(byte[])"/>
        /// </summary>
        internal IntPtr externalBuffer;

        /// <summary>
        /// Raw bytes
        /// </summary>
        internal byte[] rawBytes;

        private bool isMulticast;
        private bool isStreaming;
        private uint payload = 0;
        private uint streamPacketDelay;
        private int portRx;
        private string rxIP;
        private uint width, height, offsetX, offsetY, bytesPerPixel;
        private readonly ConcurrentDictionary<string, ICategory> cameraParametersCache;
        private readonly SemaphoreSlim syncParametersSemaphore = new(1, 1);

        /// <summary>
        /// Camera constructor with initialized Gvcp Controller
        /// </summary>
        /// <param name="gvcp">GVCP Controller</param>
        public Camera(IGvcp gvcp)
        {
            Gvcp = gvcp;
            cameraParametersCache = new ConcurrentDictionary<string, ICategory>();
            Init();
        }

        /// <summary>
        /// Register dictionary of camera
        /// </summary>
        /// <summary>
        /// Default camera constructor initializes the controller
        /// </summary>
        public Camera()
        {
            Gvcp = new Gvcp();
            cameraParametersCache = new ConcurrentDictionary<string, ICategory>();
            Init();
        }

        /// <summary>
        /// Event for frame ready
        /// </summary>
        public EventHandler<byte[]> FrameReady { get; set; }

        /// <summary>
        /// Fired alongside <see cref="FrameReady"/> with per-frame metadata
        /// (hardware timestamp and frame ID) from the GVSP image leader.
        /// </summary>
        public EventHandler<GvspFrameInfo> FrameReadyWithInfo { get; set; }

        /// <summary>
        /// GVCP controller
        /// </summary>
        public IGvcp Gvcp { get; private set; }

        /// <summary>
        /// The source port from camera to host for GSVP protocol
        /// </summary>
        public int SCSPPort
        {
            get;
            private set;
        }

        /// <summary>
        /// Camera height
        /// </summary>
        public uint Height
        {
            get => height;
            set
            {
                if (value != height)
                {
                    height = value;
                    OnPropertyChanged(nameof(Height));
                }
            }
        }

        /// <summary>
        /// Camera IP
        /// </summary>
        public string IP
        {
            get => Gvcp.CameraIp;
            set
            {
                Gvcp.CameraIp = value;
                OnPropertyChanged(nameof(IP));
            }
        }

        /// <summary>
        /// The receive socket timeout in milliseconds. Set -1 for infinite timeout
        /// </summary>
        public int ReceiveTimeoutInMilliseconds
        {
            get => Gvcp.ReceiveTimeoutInMilliseconds;
            set
            {
                Gvcp.ReceiveTimeoutInMilliseconds = value;
                if (StreamReceiver != null)
                {
                    StreamReceiver.ReceiveTimeoutInMilliseconds = value;
                }

                OnPropertyChanged(nameof(ReceiveTimeoutInMilliseconds));
            }
        }


        /// <summary>
        /// Multi-Cast Option
        /// </summary>
        public bool IsMulticast
        {
            get => isMulticast;
            set { isMulticast = value; OnPropertyChanged(nameof(IsMulticast)); }
        }

        /// <summary>
        /// Gets the raw data from the camera. Set false to get RGB frame instead of BayerGR8
        /// </summary>
        public bool IsRawFrame { get; set; } = true;

        /// <summary>
        /// Camera stream status
        /// </summary>
        public bool IsStreaming
        {
            get => isStreaming;
            set
            {
                isStreaming = value;
                OnPropertyChanged(nameof(IsStreaming));
            }
        }

        /// <summary>
        /// If enabled library will use C++ native code for stream reception
        /// </summary>
        public bool IsUsingCppForRx { get; set; }

        /// <summary>
        /// If we set the external buffer using <see cref="SetBuffer(byte[])"/> this will be set
        /// true and software will copy stream on this buffer
        /// </summary>
        public bool IsUsingExternalBuffer { get; set; }

        /// <summary>
        /// Tolernace for missing packet
        /// </summary>
        public int MissingPacketTolerance { get; set; } = 2;

        /// <summary>
        /// Motor Controller for camera, zoom/focus/iris control if any
        /// </summary>
        public MotorControl MotorController { get; set; }

        /// <summary>
        /// Multi-Cast IP: it will be applied only when IsMulticast Property is true
        /// </summary>
        public string MulticastIP { get; set; } = "239.192.11.12";

        /// <summary>
        /// Camera offset X
        /// </summary>
        public uint OffsetX
        {
            get => offsetX;
            set
            {
                if (value != offsetX)
                {
                    offsetX = value;
                    OnPropertyChanged(nameof(OffsetX));
                }
            }
        }

        /// <summary>
        /// Camera offset Y
        /// </summary>
        public uint OffsetY
        {
            get => offsetY;
            set
            {
                if (value != offsetY)
                {
                    offsetY = value;
                    OnPropertyChanged(nameof(OffsetY));
                }
            }
        }

        /// <summary>
        /// Payload size, if not provided it will be automatically set to one row, depending on resolution
        /// </summary>
        public uint Payload
        {
            get => payload;
            set
            {
                if (payload != value)
                {
                    payload = value;
                    OnPropertyChanged(nameof(Payload));
                }
            }
        }

        /// <summary>
        /// Optional inter-packet delay written to GevSCPD before starting the stream.
        /// </summary>
        public uint StreamPacketDelay
        {
            get => streamPacketDelay;
            set
            {
                if (streamPacketDelay != value)
                {
                    streamPacketDelay = value;
                    OnPropertyChanged(nameof(StreamPacketDelay));
                }
            }
        }

        /// <summary>
        /// Camera Pixel Format
        /// </summary>
        public PixelFormat PixelFormat { get; set; }

        /// <summary>
        /// Human-readable pixel format name. Returns the registry name for vendor-specific
        /// formats (e.g. "QOI_BayerRG8"), the enum name for standard PFNC formats,
        /// or a hex string (e.g. "0x81080B99") for completely unknown values.
        /// </summary>
        public string PixelFormatName => PixelFormatHelper.GetName((uint)PixelFormat);

        /// <summary>
        /// Rx port
        /// </summary>
        public int PortRx
        {
            get => portRx;
            set
            {
                if (portRx != value)
                {
                    portRx = value;
                    OnPropertyChanged(nameof(PortRx));
                }
            }
        }

        public string RxIP
        {
            get => rxIP;
            set
            {
                if (rxIP != value)
                {
                    rxIP = value;
                    OnPropertyChanged(nameof(RxIP));
                }
            }
        }

        /// <summary>
        /// Stream Receiver, replace this receiver with your own if want to receive the packets in application
        /// </summary>
        public IStreamReceiver StreamReceiver { get; set; }

        /// <summary>
        /// Event for general updates
        /// </summary>
        public EventHandler<string> Updates { get; set; }

        /// <summary>
        /// Camera width
        /// </summary>
        public uint Width
        {
            get => width;
            set
            {
                if (value != width)
                {
                    width = value;
                    OnPropertyChanged(nameof(Width));
                }
            }
        }

        /// <summary>
        /// Bridge Command for motor controller, controls focus/zoom/iris operation
        /// </summary>
        /// <param name="command">Command to set</param>
        /// <param name="value">Value to set (Applicable for ZoomValue/FocusValue)</param>
        /// <returns>Command Status</returns>
        public async Task<bool> MotorControl(LensCommand command, uint value = 1)
        {
            return await MotorController.SendMotorCommand(Gvcp, command, value).ConfigureAwait(false);
        }

        /// <summary>
        /// Read register for camera
        /// </summary>
        /// <returns>Command Status</returns>
        public async Task<bool> ReadRegisters()
        {
            return await SyncParameters().ConfigureAwait(false);
        }

        /// <summary>
        /// Sets the buffer from external source
        /// </summary>
        /// <param name="externalRawBytes"></param>
        public void SetBuffer(byte[] externalRawBytes)
        {
            if (!IsStreaming && externalRawBytes != default)
            {
                if (rawBytes != null)
                {
                    Array.Clear(rawBytes, 0, rawBytes.Length);
                }

                rawBytes = externalRawBytes;
                IsUsingExternalBuffer = true;
            }
        }

        /// <summary>
        /// Sets the offset of camera
        /// </summary>
        /// <param name="offsetX">Offset X to set</param>
        /// <param name="offsetY">Offset Y to set</param>
        /// <returns>Command Status</returns>
        public async Task<bool> SetOffsetAsync(int offsetX, int offsetY)
        {
            return await SetOffsetAsync((uint)offsetX, (uint)offsetY).ConfigureAwait(false);
        }

        /// <summary>
        /// Sets the offset of camera
        /// </summary>
        /// <returns>Command Status</returns>
        public async Task<bool> SetOffsetAsync()
        {
            return await SetOffsetAsync(OffsetX, OffsetY).ConfigureAwait(false);
        }

        //ToDo: Error handle the following method.
        /// <summary>
        /// Sets the offset of camera
        /// </summary>
        /// <param name="offsetX">Offset X to set</param>
        /// <param name="offsetY">Offset Y to set</param>
        /// <returns>Command Status</returns>
        public async Task<bool> SetOffsetAsync(uint offsetX, uint offsetY)
        {
            bool controlTaken = false;
            try
            {
                if (!IsStreaming)
                {
                    controlTaken = await Gvcp.TakeControl().ConfigureAwait(false);
                    if (!controlTaken)
                    {
                        return false;
                    }
                }

                var offsetXRegister = (await Gvcp.GetRegister(nameof(RegisterName.OffsetX))).register;
                var offsetYRegister = (await Gvcp.GetRegister(nameof(RegisterName.OffsetY))).register;
                string[] registers = new string[2];
                registers[0] = string.Format("0x{0:X8}", (await offsetXRegister.GetAddressAsync()));
                registers[1] = string.Format("0x{0:X8}", (await offsetYRegister.GetAddressAsync()));
                uint[] valueToWrite = new uint[] { offsetX, offsetY };
                bool status = (await Gvcp.WriteRegisterAsync(registers, valueToWrite).ConfigureAwait(false)).Status == GvcpStatus.GEV_STATUS_SUCCESS;
                GvcpReply reply = await Gvcp.ReadRegisterAsync(registers).ConfigureAwait(false);
                if (reply.Status == GvcpStatus.GEV_STATUS_SUCCESS)
                {
                    OffsetX = reply.RegisterValues[0];
                    OffsetY = reply.RegisterValues[1];
                }

                return status;
            }
            finally
            {
                if (controlTaken && !IsStreaming)
                {
                    await Gvcp.LeaveControl().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Sets the resolution of camera
        /// </summary>
        /// <param name="width">Width to set</param>
        /// <param name="height">Height to set</param>
        /// <returns>Command Status</returns>
        public async Task<bool> SetResolutionAsync(uint width, uint height)
        {
            bool controlTaken = false;
            try
            {
                controlTaken = await Gvcp.TakeControl().ConfigureAwait(false);
                if (!controlTaken)
                {
                    return false;
                }

                var widthPValue = (await Gvcp.GetRegister(nameof(RegisterName.Width))).pValue;
                var heightPValue = (await Gvcp.GetRegister(nameof(RegisterName.Height))).pValue;
                GvcpReply widthWriteReply = (await widthPValue.SetValueAsync(width).ConfigureAwait(false)) as GvcpReply;
                GvcpReply heightWriteReply = (await heightPValue.SetValueAsync(height).ConfigureAwait(false)) as GvcpReply;
                bool status = (widthWriteReply.Status == GvcpStatus.GEV_STATUS_SUCCESS && heightWriteReply.Status == GvcpStatus.GEV_STATUS_SUCCESS);
                if (status)
                {
                    long newWidth = (long)(await widthPValue.GetValueAsync().ConfigureAwait(false));
                    long newHeight = (long)(await heightPValue.GetValueAsync().ConfigureAwait(false));

                    Width = (uint)newWidth;
                    Height = (uint)newHeight;
                }
                return status;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                if (controlTaken)
                {
                    await Gvcp.LeaveControl().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Sets the resolution of camera
        /// </summary>
        /// <param name="width">Width to set</param>
        /// <param name="height">Height to set</param>
        /// <returns>Command Status</returns>
        public async Task<bool> SetResolutionAsync(int width, int height)
        {
            return await SetResolutionAsync((uint)width, (uint)height).ConfigureAwait(false);
        }

        /// <summary>
        /// Sets the Resolution of camera
        /// </summary>
        /// <returns>Command Status</returns>
        public async Task<bool> SetResolutionAsync()
        {
            return await SetResolutionAsync(Width, Height).ConfigureAwait(false);
        }

        /// <summary>
        /// This method will get current PC IP and Gets the Camera ip from Gvcp
        /// </summary>
        /// <param name="rxIP">If rxIP is not provided, method will detect system IP and use it</param>
        /// <param name="rxPort">It will set randomly when not provided</param>
        /// <param name="frameReady">If not null this event will be raised</param>
        /// <returns></returns>
        public async Task<bool> StartStreamAsync(string? rxIP = null, int rxPort = 0)
        {
            string ip2Send;
            bool controlTaken = false;
            bool receiverStarted = false;
            // If the custom stream receiver is not set then it will set the default one

            if (string.IsNullOrEmpty(rxIP))
            {
                if (string.IsNullOrEmpty(RxIP) && !SetRxIP())
                {
                    Updates?.Invoke(this, "StartStreamAsync failed: no valid receiver IP was available.");
                    return false;
                }
            }
            else
            {
                RxIP = rxIP;
            }
            ip2Send = RxIP;

            if (IsMulticast)
            {
                ip2Send = MulticastIP;
            }
            try
            {
                var status = await SyncParameters().ConfigureAwait(false);
                if (!status)
                {
                    Updates?.Invoke(this, "StartStreamAsync failed: SyncParameters returned false.");
                    return status;
                }
            }
            catch (Exception ex)
            {
                Updates?.Invoke(this, $"StartStreamAsync failed during SyncParameters: {ex.Message}");
                return false;
            }

            if (rxPort != 0)
            {
                PortRx = rxPort;
            }
            else if (!IsMulticast)
            {
                PortRx = 0;
            }

            if (Payload == 0)
            {
                CalculateSingleRowPayload();
            }

            if (!IsUsingExternalBuffer)
            {
                SetRxBuffer();
            }

            var acquisitionStart = (await Gvcp.GetRegister(nameof(RegisterName.AcquisitionStart))).pValue;
            if (acquisitionStart == null)
            {
                Updates?.Invoke(this, "StartStreamAsync failed: AcquisitionStart command was not found in the XML/register map.");
                return false;
            }

            try
            {
                SetupReceiver();
                if (!IsMulticast && PortRx == 0)
                {
                    SetupRxThread();
                    receiverStarted = true;
                    PortRx = StreamReceiver.PortRx;
                }

                controlTaken = await Gvcp.TakeControl(true).ConfigureAwait(false);
                if (!controlTaken)
                {
                    Updates?.Invoke(this, "StartStreamAsync failed: camera control could not be acquired.");
                    if (IsMulticast)
                    {
                        if (!receiverStarted)
                        {
                            SetupReceiver();
                            SetupRxThread();
                            receiverStarted = true;
                        }

                        IsStreaming = true;
                    }

                    return IsStreaming;
                }

                SetupReceiver();
                var gevSCPHostPort = (await Gvcp.GetRegister(nameof(GvcpRegister.GevSCPHostPort))).pValue;
                var gevSCPHostPortReply = (await gevSCPHostPort.SetValueAsync((uint)PortRx).ConfigureAwait(false) as GvcpReply);
                if (gevSCPHostPortReply?.Status != GvcpStatus.GEV_STATUS_SUCCESS)
                {
                    Updates?.Invoke(this, $"StartStreamAsync failed: GevSCPHostPort write returned {gevSCPHostPortReply?.Status} for port {PortRx}.");
                    return false;
                }

                var gevSCDA = (await Gvcp.GetRegister(nameof(GvcpRegister.GevSCDA))).pValue;
                var gevSCDAReply = (await gevSCDA.SetValueAsync(Converter.IpToNumber(ip2Send)).ConfigureAwait(false) as GvcpReply);
                if (gevSCDAReply?.Status != GvcpStatus.GEV_STATUS_SUCCESS)
                {
                    Updates?.Invoke(this, $"StartStreamAsync failed: GevSCDA write returned {gevSCDAReply?.Status} for receiver IP {ip2Send}.");
                    return false;
                }

                var gevSCSP = (await Gvcp.GetRegister(nameof(GvcpRegister.GevSCSP))).pValue;
                var getSCSPPortValue = await gevSCSP.GetValueAsync().ConfigureAwait(false);
                if (getSCSPPortValue.HasValue)
                {
                    SCSPPort = (int)getSCSPPortValue.Value;
                }

                StreamReceiver.CameraSourcePort = SCSPPort;

                var gevSCPSPacketSize = (await Gvcp.GetRegister(nameof(GvcpRegister.GevSCPSPacketSize))).pValue;
                var gevSCPSPacketSizeReply = (await gevSCPSPacketSize.SetValueAsync(Payload).ConfigureAwait(false) as GvcpReply);
                if (gevSCPSPacketSizeReply?.Status != GvcpStatus.GEV_STATUS_SUCCESS)
                {
                    Updates?.Invoke(this, $"StartStreamAsync failed: GevSCPSPacketSize write returned {gevSCPSPacketSizeReply?.Status} for payload {Payload}.");
                    return false;
                }

                var gevSCPDReply = await Gvcp.WriteRegisterAsync(GvcpRegister.GevSCPD, StreamPacketDelay).ConfigureAwait(false);
                if (gevSCPDReply?.Status != GvcpStatus.GEV_STATUS_SUCCESS)
                {
                    Updates?.Invoke(this, $"StartStreamAsync failed: GevSCPD write returned {gevSCPDReply?.Status} for delay {StreamPacketDelay}.");
                    return false;
                }

                if (!receiverStarted)
                {
                    SetupReceiver();
                    SetupRxThread();
                    receiverStarted = true;
                    PortRx = StreamReceiver.PortRx;
                }

                var acquisitionStartReply = (await acquisitionStart.SetValueAsync(1).ConfigureAwait(false)) as GvcpReply;
                if (acquisitionStartReply?.Status == GvcpStatus.GEV_STATUS_SUCCESS)
                {
                    IsStreaming = true;
                }
                else
                {
                    Updates?.Invoke(this, $"StartStreamAsync failed: AcquisitionStart returned {acquisitionStartReply?.Status}.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Updates?.Invoke(this, $"StartStreamAsync failed with exception: {ex.Message}");
                return false;
            }
            finally
            {
                if (!IsStreaming)
                {
                    if (receiverStarted)
                    {
                        StreamReceiver?.StopReception();
                    }

                    if (controlTaken)
                    {
                        await Gvcp.LeaveControl().ConfigureAwait(false);
                    }
                }
            }

            return IsStreaming;
        }

        /// <summary>
        /// Stops the camera stream and leave camera control
        /// </summary>
        /// <returns>Is streaming status</returns>
        public async Task<bool> StopStream()
        {
            await Gvcp.WriteRegisterAsync(GvcpRegister.GevSCDA, 0).ConfigureAwait(false);
            StreamReceiver?.StopReception();
            if (await Gvcp.LeaveControl().ConfigureAwait(false))
            {
                IsStreaming = false;
            }
            return IsStreaming;
        }

        public async Task<long?> GetParameterValue(string parameterName)
        {
            ICategory parameter = await GetParameter(parameterName).ConfigureAwait(false);
            if (parameter == null)
            {
                return null;
            }

            return await parameter.PValue.GetValueAsync().ConfigureAwait(false);
        }

        public async Task<T?> GetParameterValue<T>(string parameterName) where T : struct
        {
            Type requestedType = typeof(T);

            if (requestedType == typeof(bool))
            {
                var result = await GetBooleanParameterValueCore(parameterName).ConfigureAwait(false);
                return result is null ? null : (T?)(object)result.Value;
            }

            if (requestedType == typeof(double))
            {
                var result = await GetFloatParameterValueCore(parameterName).ConfigureAwait(false);
                return result is null ? null : (T?)(object)result.Value;
            }

            if (requestedType == typeof(float))
            {
                var result = await GetFloatParameterValueCore(parameterName).ConfigureAwait(false);
                return result is null ? null : (T?)(object)(float)result.Value;
            }

            var integerValue = await GetParameterValue(parameterName).ConfigureAwait(false);
            if (integerValue is null)
            {
                return null;
            }

            object convertedValue = requestedType switch
            {
                _ when requestedType == typeof(long) => integerValue.Value,
                _ when requestedType == typeof(int) => checked((int)integerValue.Value),
                _ when requestedType == typeof(uint) => checked((uint)integerValue.Value),
                _ when requestedType == typeof(short) => checked((short)integerValue.Value),
                _ when requestedType == typeof(ushort) => checked((ushort)integerValue.Value),
                _ when requestedType == typeof(byte) => checked((byte)integerValue.Value),
                _ when requestedType == typeof(sbyte) => checked((sbyte)integerValue.Value),
                _ => throw new NotSupportedException($"GetParameterValue<{requestedType.Name}> is not supported."),
            };

            return (T?)convertedValue;
        }

        [Obsolete("Use GetParameterValue<double>(parameterName) instead.")]
        public async Task<double?> GetFloatParameterValue(string parameterName)
        {
            return await GetFloatParameterValueCore(parameterName).ConfigureAwait(false);
        }

        private async Task<double?> GetFloatParameterValueCore(string parameterName)
        {
            ICategory parameter = await GetParameter(parameterName).ConfigureAwait(false);
            if (parameter == null)
            {
                return null;
            }

            if (parameter is IFloat floatParameter)
            {
                return await floatParameter.GetValueAsync().ConfigureAwait(false);
            }

            if (parameter.PValue is IDoubleValue doubleValue)
            {
                return await doubleValue.GetDoubleValueAsync().ConfigureAwait(false);
            }

            if (parameter.PValue is not null)
            {
                var result = await parameter.PValue.GetValueAsync().ConfigureAwait(false);
                return result;
            }

            return null;
        }

        [Obsolete("Use GetParameterValue<bool>(parameterName) instead.")]
        public async Task<bool?> GetBooleanParameterValue(string parameterName)
        {
            return await GetBooleanParameterValueCore(parameterName).ConfigureAwait(false);
        }

        private async Task<bool?> GetBooleanParameterValueCore(string parameterName)
        {
            ICategory parameter = await GetParameter(parameterName).ConfigureAwait(false);
            if (parameter == null)
            {
                return null;
            }

            if (parameter is IBoolean boolParameter)
            {
                return await boolParameter.GetValueAsync().ConfigureAwait(false);
            }

            if (parameter.PValue is null)
            {
                return null;
            }

            var result = await parameter.PValue.GetValueAsync().ConfigureAwait(false);
            return result switch
            {
                null => null,
                0 => false,
                _ => true,
            };
        }

        /// <summary>
        /// Load a camera parameter
        /// </summary>
        /// <param name="parameterName"></param>
        /// <returns></returns>
        public async Task<bool> LoadParameter(string parameterName)
        {
            var value = (await Gvcp.GetRegisterCategory(parameterName).ConfigureAwait(false));
            if (value == null)
            {
                return false;
            }

            cameraParametersCache[parameterName] = value;
            return true;
        }

        /// <summary>
        /// Obtain the parameter properties like name, description, tooltip
        /// </summary>
        /// <param name="parameterName">The name of the parameter</param>
        /// <returns></returns>
        public async Task<CategoryProperties> GetParameterProperties(string parameterName)
        {
            ICategory parameter = await GetParameter(parameterName).ConfigureAwait(false);
            if (parameter == null)
            {
                return null;
            }

            return parameter.CategoryProperties;
        }

        /// <summary>
        /// Obtain the minimum value allowed for the parameter. 0 if the parameter does not support it.
        /// </summary>
        /// <param name="parameterName">The name of the parameter</param>
        /// <returns></returns>
        public async Task<long> GetParameterMinValue(string parameterName)
        {
            ICategory parameter = await GetParameter(parameterName).ConfigureAwait(false);

            if (parameter == null)
            {
                return 0;
            }

            if (parameter.PMin == null)
            {
                return 0;
            }

            var result = await parameter.PMin.GetValueAsync().ConfigureAwait(false);
            return result ?? 0;
        }

        public async Task<double> GetFloatParameterMinValue(string parameterName)
        {
            ICategory parameter = await GetParameter(parameterName).ConfigureAwait(false);
            if (parameter == null)
            {
                return 0;
            }

            if (parameter is IFloat floatParameter)
            {
                return await floatParameter.GetMinAsync().ConfigureAwait(false);
            }

            if (parameter.PMin is IDoubleValue doubleMin)
            {
                var result = await doubleMin.GetDoubleValueAsync().ConfigureAwait(false);
                return result ?? 0;
            }

            if (parameter.PMin == null)
            {
                return 0;
            }

            var fallback = await parameter.PMin.GetValueAsync().ConfigureAwait(false);
            return fallback ?? 0;
        }

        /// <summary>
        /// Obtain the maximum value allowed for the parameter. 0 if the parameter does not support it.
        /// </summary>
        /// <param name="parameterName">The name of the parameter</param>;
        /// <returns></returns>
        public async Task<long> GetParameterMaxValue(string parameterName)
        {
            ICategory parameter = await GetParameter(parameterName).ConfigureAwait(false);

            if (parameter == null)
            {
                return 0;
            }

            if (parameter.PMax == null)
            {
                return 0;
            }

            var result = await parameter.PMax.GetValueAsync().ConfigureAwait(false);
            return result ?? 0;
        }

        public async Task<double> GetFloatParameterMaxValue(string parameterName)
        {
            ICategory parameter = await GetParameter(parameterName).ConfigureAwait(false);
            if (parameter == null)
            {
                return 0;
            }

            if (parameter is IFloat floatParameter)
            {
                return await floatParameter.GetMaxAsync().ConfigureAwait(false);
            }

            if (parameter.PMax is IDoubleValue doubleMax)
            {
                var result = await doubleMax.GetDoubleValueAsync().ConfigureAwait(false);
                return result ?? 0;
            }

            if (parameter.PMax == null)
            {
                return 0;
            }

            var fallback = await parameter.PMax.GetValueAsync().ConfigureAwait(false);
            return fallback ?? 0;
        }

        /// <summary>
        /// Get the description of the parameter
        /// </summary>
        /// <param name="parameterName">The name of the parameter</param>
        /// <returns></returns>
        public async Task<ICategory> GetParameter(string parameterName)
        {
            if (!cameraParametersCache.TryGetValue(parameterName, out ICategory parameter) &&
                !await LoadParameter(parameterName).ConfigureAwait(false))
            {
                return null;
            }

            cameraParametersCache.TryGetValue(parameterName, out parameter);
            return parameter;
        }

        /// <summary>
        /// Set the value of a camera paramter
        /// </summary>
        /// <param name="parameterName">The name of the parameter to change</param>
        /// <param name="value">the new value to set</param>
        /// <returns></returns>
        public async Task<bool> SetCameraParameter(string parameterName, long value)
        {
            ICategory parameter = await GetParameter(parameterName).ConfigureAwait(false);
            if (parameter == null)
            {
                return false;
            }

            if (parameter is IBoolean)
            {
                return await SetCameraParameter(parameterName, value != 0).ConfigureAwait(false);
            }

            if (parameter is IFloat || parameter.PValue is IDoubleValue)
            {
                return await SetCameraParameter(parameterName, (double)value).ConfigureAwait(false);
            }

            if (parameter.PValue is null)
            {
                return false;
            }

            return IsSuccessfulReply(await parameter.PValue.SetValueAsync(value).ConfigureAwait(false));
        }

        public Task<bool> SetCameraParameter(string parameterName, double value)
        {
            return SetFloatParameterCore(parameterName, value);
        }

        public Task<bool> SetCameraParameter(string parameterName, bool value)
        {
            return SetBooleanParameterCore(parameterName, value);
        }

        [Obsolete("Use SetCameraParameter(parameterName, value) instead.")]
        public async Task<bool> SetFloatParameter(string parameterName, double value)
        {
            return await SetFloatParameterCore(parameterName, value).ConfigureAwait(false);
        }

        private async Task<bool> SetFloatParameterCore(string parameterName, double value)
        {
            ICategory parameter = await GetParameter(parameterName).ConfigureAwait(false);
            if (parameter == null)
            {
                return false;
            }

            if (parameter.PValue is IDoubleValue doubleValue)
            {
                return IsSuccessfulReply(await doubleValue.SetDoubleValueAsync(value).ConfigureAwait(false));
            }

            if (parameter is IFloat floatParameter)
            {
                try
                {
                    await floatParameter.SetValueAsync(value).ConfigureAwait(false);
                    return true;
                }
                catch (Exception ex)
                {
                    Updates?.Invoke(this, ex.Message);
                    return false;
                }
            }

            if (parameter.PValue is not null)
            {
                return IsSuccessfulReply(await parameter.PValue.SetValueAsync((long)Math.Round(value, MidpointRounding.AwayFromZero)).ConfigureAwait(false));
            }

            return false;
        }

        [Obsolete("Use SetCameraParameter(parameterName, value) instead.")]
        public async Task<bool> SetBooleanParameter(string parameterName, bool value)
        {
            return await SetBooleanParameterCore(parameterName, value).ConfigureAwait(false);
        }

        private async Task<bool> SetBooleanParameterCore(string parameterName, bool value)
        {
            ICategory parameter = await GetParameter(parameterName).ConfigureAwait(false);
            if (parameter == null)
            {
                return false;
            }

            if (parameter is IBoolean boolParameter)
            {
                return IsSuccessfulReply(await boolParameter.SetValueAsync(value).ConfigureAwait(false));
            }

            if (parameter.PValue is null)
            {
                return false;
            }

            return IsSuccessfulReply(await parameter.PValue.SetValueAsync(value ? 1 : 0).ConfigureAwait(false));
        }

        /// <summary>
        /// It reads all the parameters from the camera
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SyncParameters(int syncAttempts = 1)
        {
            await syncParametersSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!await Gvcp.ReadXmlFileAsync(IP))
                {
                    return false;
                }

                cameraParametersCache.Clear();

                Width = (uint)await GetParameterValue(nameof(RegisterName.Width)).ConfigureAwait(false);
                Height = (uint)await GetParameterValue(nameof(RegisterName.Height)).ConfigureAwait(false);
                OffsetX = (uint)await GetParameterValue(nameof(RegisterName.OffsetX)).ConfigureAwait(false);
                OffsetY = (uint)await GetParameterValue(nameof(RegisterName.OffsetY)).ConfigureAwait(false);
                PixelFormat = (PixelFormat)(uint)await GetParameterValue(nameof(RegisterName.PixelFormat)).ConfigureAwait(false);
                bytesPerPixel = (uint)PixelFormatToBytesPerPixel(PixelFormat);

                return true;
            }
            catch (Exception ex)
            {
                Updates?.Invoke(this, ex.Message);
            }
            finally
            {
                syncParametersSemaphore.Release();
            }

            return false;
        }

        private void CalculateSingleRowPayload()
        {
            Payload = (uint)(8 + 28 + PixelFormatHelper.GetLineSize((int)Width, (uint)PixelFormat));
        }

        private async void CameraIpChanged(object sender, EventArgs e)
        {
            await SyncParameters().ConfigureAwait(false);
        }

        private void Init()
        {
            MotorController = new MotorControl();
            SetRxIP();
            Gvcp.CameraIpChanged += CameraIpChanged;
        }

        private int PixelFormatToBytesPerPixel(PixelFormat pixelFormat)
        {
            return PixelFormatHelper.GetBytesPerPixelRoundedUp((uint)pixelFormat);
        }

        private void SetRxBuffer()
        {
            if (rawBytes != null)
            {
                Array.Clear(rawBytes, 0, rawBytes.Length);
            }

            if (!IsRawFrame && PixelFormatHelper.IsBayerFormat((uint)PixelFormat))
            {
                rawBytes = new byte[Width * Height * 3];
            }
            else
            {
                rawBytes = new byte[PixelFormatHelper.GetFrameSize((int)Width, (int)Height, (uint)PixelFormat)];
            }
        }

        private bool SetRxIP()
        {
            try
            {
                string ip = NetworkService.GetMyIp();
                if (string.IsNullOrEmpty(ip))
                {
                    return false;
                }
                RxIP = ip;
                return true;
            }
            catch (Exception ex)
            {
                Updates?.Invoke(this, ex.Message);
                return false;
            }
        }

        private void SetupReceiver()
        {
            StreamReceiver ??= new StreamReceiverBufferswap();
            StreamReceiver.RxIP = RxIP;
            StreamReceiver.CameraIP = IP;
            StreamReceiver.CameraSourcePort = SCSPPort;
            StreamReceiver.ReceiveTimeoutInMilliseconds = ReceiveTimeoutInMilliseconds;
            StreamReceiver.IsMulticast = IsMulticast;
            StreamReceiver.MulticastIP = MulticastIP;
            StreamReceiver.PortRx = PortRx;
            StreamReceiver.MissingPacketTolerance = MissingPacketTolerance;
            StreamReceiver.Updates = Updates;
            StreamReceiver.FrameReady = FrameReady;
            StreamReceiver.FrameReadyWithInfo = FrameReadyWithInfo;
        }

        private void SetupRxThread()
        {
            StreamReceiver.StartRxThread();
        }

        private static bool IsSuccessfulReply(IReplyPacket reply)
        {
            return reply is GvcpReply gvcpReply && gvcpReply.Status == GvcpStatus.GEV_STATUS_SUCCESS;
        }
    }
}