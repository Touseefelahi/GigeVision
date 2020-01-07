using GigeVision.Core.Enums;
using GigeVision.Core.Interfaces;
using Stira.WpfCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace GigeVision.Core.Models
{
    public class Gvsp : BaseNotifyPropertyChanged, IGvsp
    {
        private int Port = 5556;
        private uint width, height, offsetX, offsetY, bytesPerPixel;

        private uint zoomValue;
        private uint focusValue;
        private IntPtr intPtr;
        private byte[] rawBytes = Array.Empty<byte>();

        private bool isStreaming;

        public Gvsp(IGvcp gvcp)
        {
            Gvcp = gvcp;
            Task.Run(async () => await ReadParameters().ConfigureAwait(false));
            MotorController = new MotorControl();
            Gvcp.CameraIpChanged += CameraIpChanged;
        }

        public bool IsStreaming { get => isStreaming; set { isStreaming = value; OnPropertyChanged(nameof(IsStreaming)); } }

        public IGvcp Gvcp { get; private set; }

        public EventHandler<byte[]> FrameReady { get; set; }

        public EventHandler<string> Updates { get; set; }

        public uint Payload { get; set; } = 1400;

        public uint Width
        {
            get { return width; }
            set
            {
                if (value != width)
                {
                    width = value;
                    OnPropertyChanged(nameof(Width));
                }
            }
        }

        public uint Height
        {
            get { return height; }
            set
            {
                if (value != height)
                {
                    height = value;
                    OnPropertyChanged(nameof(Height));
                }
            }
        }

        public uint OffsetX
        {
            get { return offsetX; }
            set
            {
                if (value != offsetX)
                {
                    offsetX = value;
                    OnPropertyChanged(nameof(OffsetX));
                }
            }
        }

        public uint OffsetY
        {
            get { return offsetY; }
            set
            {
                if (value != offsetY)
                {
                    offsetY = value;
                    OnPropertyChanged(nameof(OffsetY));
                }
            }
        }

        public PixelFormat PixelFormat { get; set; }

        public uint ZoomValue
        {
            get => zoomValue;
            set
            {
                zoomValue = value;
                OnPropertyChanged(nameof(ZoomValue));
            }
        }

        public uint FocusValue
        {
            get => focusValue;
            set
            {
                focusValue = value;
                OnPropertyChanged(nameof(FocusValue));
            }
        }

        public MotorControl MotorController { get; set; }

        public async Task<bool> StartStreamAsync(string rxIP = null, int port = 0)
        {
            if (string.IsNullOrEmpty(rxIP))
            {
                rxIP = GetMyIp();
            }
            try
            {
                if (Gvcp.RegistersDictionary.Count == 0)
                {
                    await ReadParameters().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                if (Gvcp.RegistersDictionary.Count == 0)
                    return false;
            }
            if (port == 0)
            {
                Port = new Random().Next(5000, 6000);
            }
            Port = 5557;
            if (Gvcp.RegistersDictionary.ContainsKey(nameof(RegisterName.AcquisitionStartReg)))
            {
                if (await Gvcp.TakeControl(true).ConfigureAwait(false))
                {
                    StartRxCppThread();
                    IsStreaming = true;

                    if ((await Gvcp.WriteRegisterAsync(GvcpRegister.SCPHostPort, (uint)Port).ConfigureAwait(false)).Status == GvcpStatus.GEV_STATUS_SUCCESS)
                    {
                        await Gvcp.WriteRegisterAsync(GvcpRegister.SCDA, Converter.IpToNumber(rxIP)).ConfigureAwait(false);
                        await Gvcp.WriteRegisterAsync(GvcpRegister.SCPSPacketSize, Payload).ConfigureAwait(false);
                        await Gvcp.WriteRegisterAsync(Gvcp.RegistersDictionary[nameof(RegisterName.AcquisitionStartReg)], 1).ConfigureAwait(false);
                    }
                }
            }
            return true;
        }

        public async Task<bool> StopStream()
        {
            CvInterop.Stop();
            await Gvcp.WriteRegisterAsync(GvcpRegister.SCDA, 0).ConfigureAwait(false);
            return await Gvcp.LeaveControl().ConfigureAwait(false);
        }

        public async Task<bool> SetResolutionAsync(uint width, uint height)
        {
            try
            {
                await Gvcp.TakeControl().ConfigureAwait(false);
                var registers = new string[2];
                registers[0] = Gvcp.RegistersDictionary[nameof(RegisterName.WidthReg)];
                registers[1] = Gvcp.RegistersDictionary[nameof(RegisterName.HeightReg)];
                var valueToWrite = new uint[] { width, height };
                var status = (await Gvcp.WriteRegisterAsync(registers, valueToWrite).ConfigureAwait(false)).Status == GvcpStatus.GEV_STATUS_SUCCESS;
                var reply = await Gvcp.ReadRegisterAsync(registers).ConfigureAwait(false);
                if (reply.Status == GvcpStatus.GEV_STATUS_SUCCESS)
                {
                    Width = reply.RegisterValues[0];
                    Height = reply.RegisterValues[1];
                }

                await Gvcp.LeaveControl().ConfigureAwait(false);
                return status;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> SetResolutionAsync(int width, int height)
        {
            return await SetResolutionAsync((uint)width, (uint)height).ConfigureAwait(false);
        }

        public async Task<bool> SetOffsetAsync(int offsetX, int offsetY)
        {
            return await SetOffsetAsync((uint)offsetX, (uint)offsetY).ConfigureAwait(false);
        }

        public async Task<bool> SetOffsetAsync(uint offsetX, uint offsetY)
        {
            if (!IsStreaming)
            {
                await Gvcp.TakeControl().ConfigureAwait(false);
            }
            var registers = new string[2];
            registers[0] = Gvcp.RegistersDictionary[nameof(RegisterName.OffsetXReg)];
            registers[1] = Gvcp.RegistersDictionary[nameof(RegisterName.OffsetYReg)];
            var valueToWrite = new uint[] { offsetX, offsetY };
            var status = (await Gvcp.WriteRegisterAsync(registers, valueToWrite).ConfigureAwait(false)).Status == GvcpStatus.GEV_STATUS_SUCCESS;
            var reply = await Gvcp.ReadRegisterAsync(registers).ConfigureAwait(false);
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

        public async Task<bool> MotorControl(LensCommand command, uint value = 1)
        {
            return await MotorController.SendMotorCommand(Gvcp, command, value).ConfigureAwait(false);
        }

        public async Task<bool> ReadRegisters()
        {
            return await ReadParameters().ConfigureAwait(false);
        }

        private async void CameraIpChanged(object sender, EventArgs e)
        {
            await ReadParameters().ConfigureAwait(false);
        }

        private void StartRxCppThread()
        {
            Thread threadDecode = new Thread(RxCpp)
            {
                Priority = ThreadPriority.Highest,
                Name = "Decode Cpp Packets Thread",
                IsBackground = true
            };
            threadDecode.Start();
        }

        private void RxCpp()
        {
            intPtr = new IntPtr();
            CvInterop.Start(Port, out intPtr, (int)Width, (int)Height, (int)bytesPerPixel, RawFrameReady);
        }

        private void RawFrameReady(int value)
        {
            Marshal.Copy(intPtr, rawBytes, 0, rawBytes.Length);
            FrameReady?.Invoke(null, rawBytes);
        }

        private async Task<bool> ReadParameters()
        {
            await Gvcp.ReadAllRegisterAddressFromCameraAsync().ConfigureAwait(false);
            if (Gvcp.RegistersDictionary.Count == 0) return false;
            try
            {
                var registersToRead = new string[]
                {
                    Gvcp.RegistersDictionary[nameof(RegisterName.WidthReg)],
                    Gvcp.RegistersDictionary[nameof(RegisterName.HeightReg)],
                    Gvcp.RegistersDictionary[nameof(RegisterName.OffsetXReg)],
                    Gvcp.RegistersDictionary[nameof(RegisterName.OffsetYReg)],
                    Gvcp.RegistersDictionary[nameof(RegisterName.PixelFormatReg)],
                };
                var reply2 = await Gvcp.ReadRegisterAsync(registersToRead);
                if (reply2.Status == GvcpStatus.GEV_STATUS_SUCCESS)
                {
                    Width = reply2.RegisterValues[0];
                    Height = reply2.RegisterValues[1];
                    OffsetX = reply2.RegisterValues[2];
                    OffsetY = reply2.RegisterValues[3];
                    PixelFormat = (PixelFormat)reply2.RegisterValues[4];
                    bytesPerPixel = (uint)(reply2.Reply[reply2.Reply.Count - 3] / 8);
                    rawBytes = new byte[Width * Height * (int)bytesPerPixel];
                }
            }
            catch (Exception ex)
            {
            }
            if (Gvcp.RegistersDictionary.Count > 0)
            {
                MotorController.CheckMotorControl(Gvcp.RegistersDictionary);
            }
            return true;
        }

        private string GetMyIp()
        {
            string localIP;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
            return localIP;
        }
    }
}