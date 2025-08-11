using GenICam;
using GigeVision.Core.Enums;
using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Threading.Tasks;
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
        private int portRx;
        private string rxIP;
        private uint width, height, offsetX, offsetY, bytesPerPixel;
        private Dictionary<string,ICategory> cameraParametersCache;

        /// <summary>
        /// Camera constructor with initialized Gvcp Controller
        /// </summary>
        /// <param name="gvcp">GVCP Controller</param>
        public Camera(IGvcp gvcp)
        {
            Gvcp = gvcp;
            cameraParametersCache = new Dictionary<string, ICategory>();
            Task.Run(async () => await SyncParameters().ConfigureAwait(false));
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
            cameraParametersCache = new Dictionary<string, ICategory>();
            Init();
        }

        /// <summary>
        /// Event for frame ready
        /// </summary>
        public EventHandler<byte[]> FrameReady { get; set; }

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
        /// Camera Pixel Format
        /// </summary>
        public PixelFormat PixelFormat { get; set; }

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

        public bool IsBitSet<T>(T t, int pos) where T : struct, IConvertible
        {
            var value = t.ToInt64(CultureInfo.CurrentCulture);
            return (value & (1 << pos)) != 0;
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
            if (!IsStreaming)
            {
                await Gvcp.TakeControl().ConfigureAwait(false);
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
            if (!IsStreaming)
            {
                await Gvcp.LeaveControl().ConfigureAwait(false);
            }
            return status;
        }

        /// <summary>
        /// Sets the resolution of camera
        /// </summary>
        /// <param name="width">Width to set</param>
        /// <param name="height">Height to set</param>
        /// <returns>Command Status</returns>
        public async Task<bool> SetResolutionAsync(uint width, uint height)
        {
            try
            {
                await Gvcp.TakeControl().ConfigureAwait(false);
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

                await Gvcp.LeaveControl().ConfigureAwait(false);
                return status;
            }
            catch (Exception)
            {
                return false;
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
        public async Task<bool> StartStreamAsync(string rxIP = null, int rxPort = 0)
        {
            // Resolve Rx IP
            if (string.IsNullOrWhiteSpace(rxIP))
            {
                if (string.IsNullOrWhiteSpace(RxIP) && !SetRxIP())
                    return false;
            }
            else
            {
                RxIP = rxIP;
            }
            string ip2Send = IsMulticast ? MulticastIP : RxIP;

            // Ensure parameters are synced (loads XML etc.)
            try
            {
                var status = await SyncParameters().ConfigureAwait(false);
                if (!status) return false;
            }
            catch
            {
                return false;
            }

            // Decide receive port
            if (rxPort == 0)
            {
                if (PortRx == 0)
                {
                    using var udp = new UdpClient(0);
                    PortRx = ((IPEndPoint)udp.Client.LocalEndPoint).Port;
                }
            }
            else
            {
                PortRx = rxPort;
            }

            // Payload + buffer
            if (Payload == 0) CalculateSingleRowPayload();
            if (!IsUsingExternalBuffer) SetRxBuffer();

            // AcquisitionStart node (command/int). If missing, we won’t start acquisition.
            var (acquisitionStartPValue, _) = await Gvcp.GetRegister(nameof(RegisterName.AcquisitionStart)).ConfigureAwait(false);
            if (acquisitionStartPValue == null)
                return false;

            if (await Gvcp.TakeControl(true).ConfigureAwait(false))
            {
                // Defaults GevStreamChannelSelector to Stream0 In case the vendor does not
                // map the stream channels (SC0, SC1, …) registers directly to channel 0
                if (await LoadParameter("GevStreamChannelSelector").ConfigureAwait(false))
                {
                    //The test avoids a null ref or invalid cast and lets you gracefully skip channel selection if it isn’t there
                    if (await GetParameter("GevStreamChannelSelector").ConfigureAwait(false) is GenICam.GenEnumeration streamChannelSelector && streamChannelSelector.PValue != null)
                    {
                        if (streamChannelSelector.Entries.ContainsKey("Stream0"))
                            await streamChannelSelector.PValue.SetValueAsync(streamChannelSelector.Entries["Stream0"].Value).ConfigureAwait(false);
                        else if (streamChannelSelector.Entries.Count > 0)
                            await streamChannelSelector.PValue.SetValueAsync(streamChannelSelector.Entries.Values.First().Value).ConfigureAwait(false);
                    }
                }

                // Host port for stream (GevSCPHostPort)
                if (!await TrySetRegAsync(GvcpRegister.GevSCPHostPort, (uint)PortRx).ConfigureAwait(false))
                {
                    await Gvcp.LeaveControl().ConfigureAwait(false);
                    return false;
                }

                // Destination address (host IP or multicast IP) (GevSCDA)
                if (!await TrySetRegAsync(GvcpRegister.GevSCDA, Converter.IpToNumber(ip2Send)).ConfigureAwait(false))
                {
                    await Gvcp.LeaveControl().ConfigureAwait(false);
                    return false;
                }

                // Optional: read camera source port (GevSCSP) for info
                var scsp = await TryGetRegAsync(GvcpRegister.GevSCSP).ConfigureAwait(false);
                if (scsp.HasValue) SCSPPort = (int)scsp.Value;

                // Packet size (payload) (GevSCPSPacketSize)
                await TrySetRegAsync(GvcpRegister.GevSCPSPacketSize, Payload).ConfigureAwait(false);

                // Bring up receiver before starting acquisition so you don’t miss the first frames.
                SetupReceiver();
                SetupRxThread();

                // Start acquisition
                var acquisitionReply = await acquisitionStartPValue.SetValueAsync(1).ConfigureAwait(false) as GvcpReply;
                if (acquisitionReply != null && acquisitionReply.Status == GvcpStatus.GEV_STATUS_SUCCESS)
                {
                    IsStreaming = true;
                }
                else
                {
                    await StopStream().ConfigureAwait(false);
                }

                // (Optionally keep control; StopStream() will LeaveControl)
            }
            else
            {
                // Could not take control; allow multicast passive receive if requested
                // With multicast, the camera may already be broadcasting to a multicast group.
                // You don’t need control to receive; you can simply join the group and listen
                if (IsMulticast)
                {
                    SetupRxThread();
                    IsStreaming = true;
                }
            }

            return IsStreaming;
        }

        // Local helper (tuple-safe, fall back to raw GVCP when pValue is null)
        private async Task<bool> TrySetRegAsync(GvcpRegister reg, uint value)
        {
            var (pValue, register) = await Gvcp.GetRegister(reg.ToString()).ConfigureAwait(false);
            if (pValue != null)
            {
                var r = await pValue.SetValueAsync(value).ConfigureAwait(false) as GvcpReply;
                return r != null && r.Status == GvcpStatus.GEV_STATUS_SUCCESS;
            }
            else
            {
                var r = await Gvcp.WriteRegisterAsync(reg, value).ConfigureAwait(false);
                return r != null && r.Status == GvcpStatus.GEV_STATUS_SUCCESS;
            }
        }

        // Local helper (tuple-safe, fall back to raw GVCP when pValue is null)
        private async Task<uint?> TryGetRegAsync(GvcpRegister reg)
        {
            var (pValue, register) = await Gvcp.GetRegister(reg.ToString()).ConfigureAwait(false);

            if (pValue != null)
            {
                var v = await pValue.GetValueAsync().ConfigureAwait(false);
                return v.HasValue ? (uint)v.Value : (uint?)null;
            }
            else
            {
                var r = await Gvcp.ReadRegisterAsync(reg).ConfigureAwait(false);
                return (r != null
                        && r.Status == GvcpStatus.GEV_STATUS_SUCCESS
                        && r.RegisterValues != null
                        && r.RegisterValues.Count > 0)
                    ? r.RegisterValues[0]
                    : (uint?)null;
            }
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
            if (cameraParametersCache == null) 
            {
                cameraParametersCache = new Dictionary<string, ICategory>();
            }
            ICategory parameter = await GetParameter(parameterName).ConfigureAwait(false);
            if (parameter == null)
            {
                return null;
            }
           
            return await parameter.PValue.GetValueAsync().ConfigureAwait(false);
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
        
        /// <summary>
        /// Get the description of the parameter
        /// </summary>
        /// <param name="parameterName">The name of the parameter</param>
        /// <returns></returns>
        public async Task<ICategory> GetParameter(string parameterName)
        {
            if (!cameraParametersCache.ContainsKey(parameterName) && ! await LoadParameter(parameterName).ConfigureAwait(false))
            {
                return null;
            }

            return cameraParametersCache[parameterName];
        }

        /// <summary>
        /// Set the value of a camera paramter
        /// </summary>
        /// <param name="parameterName">The name of the parameter to change</param>
        /// <param name="value">the new value to set</param>
        /// <returns></returns>
        public async Task<bool> SetCameraParameter(string parameterName, long value)
        {
            if (!cameraParametersCache.ContainsKey(parameterName) && ! await LoadParameter(parameterName).ConfigureAwait(false))
            {
                return false;
            }
            
            var result  = await cameraParametersCache[parameterName].PValue.SetValueAsync(value).ConfigureAwait(false) as GvcpReply;
            return result.Status == GvcpStatus.GEV_STATUS_SUCCESS;
        }

        /// <summary>
        /// It reads all the parameters from the camera
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SyncParameters(int syncAttempts = 1)
        {
            try
            {
                if (!await Gvcp.ReadXmlFileAsync(IP))
                    return false;

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
            return false;
        }

        private void CalculateSingleRowPayload()
        {
            const uint GvspHeader = 8;
            const uint UdpIpHeader = 28;
            uint oneRow = Width * bytesPerPixel;
            uint desired = GvspHeader + UdpIpHeader + oneRow;

            // Clamp to typical MTU if you don't have jumbo frames enabled.
            uint maxOnWire = 1500; // or read camera’s allowed max for GevSCPSPacketSize
            Payload = Math.Min(desired, maxOnWire);
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
            var rawValue = (int)pixelFormat;
            var isBitHigh = IsBitSet(rawValue, 20);
            return isBitHigh ? 2 : 1;
        }

        private void SetRxBuffer()
        {
            if (rawBytes != null)
            {
                Array.Clear(rawBytes, 0, rawBytes.Length);
            }

            if (!IsRawFrame && PixelFormat.ToString().Contains("Bayer"))
            {
                rawBytes = new byte[Width * Height * 3];
            }
            else
            {
                rawBytes = new byte[Width * Height * bytesPerPixel];
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
        }

        private void SetupRxThread()
        {
            StreamReceiver.StartRxThread();
        }
    }
}