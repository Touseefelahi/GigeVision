using GigeVision.Core.Enums;
using GigeVision.Core.Interfaces;
using Stira.WpfCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GigeVision.Core.Models
{
    public class Gvsp : BaseNotifyPropertyChanged, IGvsp
    {
        private int Port = 5454;

        private UdpClient SocketRx;

        private IPEndPoint endPoint;

        private uint width, height, offsetX, offsetY;

        private Dictionary<LensCommand, string> lensControl;

        private uint zoomValue;
        private uint focusValue;

        public Gvsp(IGvcp gvcp)
        {
            Gvcp = gvcp;
            Task.Run(async () => await ReadParameters().ConfigureAwait(false));
            lensControl = new Dictionary<LensCommand, string>();
        }

        public bool IsStreaming { get; set; }

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
        public bool HasZoomControl { get; set; }
        public bool HasFocusControl { get; set; }
        public bool HasIrisControl { get; set; }
        public bool HasFixedZoomValue { get; set; }
        public bool HasFixedFocusValue { get; set; }

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
            if (Gvcp.RegistersDictionary.ContainsKey(nameof(RegisterName.AcquisitionStartReg)))
            {
                if (await Gvcp.TakeControl(true).ConfigureAwait(false))
                {
                    SetupRxSocket(); IsStreaming = true; Decode();
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

        public void Decode()
        {
            Thread threadDecode = new Thread(DecodePacketsAsync)
            {
                Priority = ThreadPriority.Highest,
                Name = "Decode Packets Thread",
                IsBackground = true
            };
            threadDecode.Start();
        }

        public async Task<bool> StopStream()
        {
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
            if (!IsStreaming)
            {
                await Gvcp.LeaveControl().ConfigureAwait(false);
            }
            return status;
        }

        public async Task<bool> MotorControl(LensCommand command, uint value = 1)
        {
            if (lensControl.ContainsKey(command))
            {
                if (lensControl.ContainsKey(LensCommand.FocusAuto))
                {
                    switch (command)
                    {
                        case LensCommand.FocusFar:
                        case LensCommand.FocusNear:
                            await Gvcp.WriteRegisterAsync(lensControl[LensCommand.FocusAuto], 0).ConfigureAwait(false);
                            break;
                    }
                }
                var status = (await Gvcp.WriteRegisterAsync(lensControl[command], value).ConfigureAwait(false)).Status == GvcpStatus.GEV_STATUS_SUCCESS;
                if (lensControl.ContainsKey(LensCommand.FocusAuto))
                {
                    switch (command)
                    {
                        case LensCommand.ZoomStop:
                            await Gvcp.WriteRegisterAsync(lensControl[LensCommand.FocusAuto], 3).ConfigureAwait(false);
                            if (lensControl.ContainsKey(LensCommand.ZoomValue))
                            {
                                var zoomValue = await Gvcp.ReadRegisterAsync(lensControl[LensCommand.ZoomValue]).ConfigureAwait(false);
                                if (zoomValue.Status == GvcpStatus.GEV_STATUS_SUCCESS)
                                {
                                    ZoomValue = zoomValue.RegisterValue;
                                }
                            }
                            break;

                        case LensCommand.FocusStop:
                        case LensCommand.FocusValue:
                            if (lensControl.ContainsKey(LensCommand.FocusValue))
                            {
                                var focusValue = await Gvcp.ReadRegisterAsync(lensControl[LensCommand.FocusValue]).ConfigureAwait(false);
                                if (focusValue.Status == GvcpStatus.GEV_STATUS_SUCCESS)
                                {
                                    FocusValue = focusValue.RegisterValue;
                                }
                            }
                            break;
                    }
                }
                return status;
            }
            return false;
        }

        private async void SetZoomValueAsync()
        {
            if (lensControl.ContainsKey(LensCommand.ZoomValue))
            {
                await Gvcp.WriteRegisterAsync(lensControl[LensCommand.ZoomValue], zoomValue).ConfigureAwait(false);
            }
        }

        private async Task ReadParameters()
        {
            await Gvcp.ReadAllRegisterAddressFromCameraAsync().ConfigureAwait(false);
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
                }
            }
            catch (Exception ex)
            {
            }
            if (Gvcp.RegistersDictionary.Count > 0)
            {
                CheckMotorControl();
            }
        }

        private void CheckMotorControl()
        {
            try
            {
                lensControl = new Dictionary<LensCommand, string>();
                var take = new string[] { "ZoomIn", "ZoomTele" };
                var skip = new string[] { "Step", "Speed", "Limit", "Digital" };
                AddLensRegister(take, skip, LensCommand.ZoomIn);

                take = new string[] { "ZoomOut", "ZoomWide" };
                if (AddLensRegister(take, skip, LensCommand.ZoomOut))
                {
                    HasZoomControl = true;
                }
                take = new string[] { "ZoomStop" };
                AddLensRegister(take, skip, LensCommand.ZoomStop);

                take = new string[] { "ZoomReg" };
                if (AddLensRegister(take, skip, LensCommand.ZoomValue))
                {
                    HasFixedZoomValue = true;
                }

                take = new string[] { "FocusFar" };
                if (AddLensRegister(take, skip, LensCommand.FocusFar))
                {
                    HasFocusControl = true;
                }

                take = new string[] { "FocusNear" };
                if (AddLensRegister(take, skip, LensCommand.FocusNear))
                {
                    HasFocusControl = true;
                }

                take = new string[] { "FocusStop" };
                if (AddLensRegister(take, skip, LensCommand.FocusStop))
                {
                    HasFocusControl = true;
                }

                take = new string[] { "FocusReg" };
                if (AddLensRegister(take, skip, LensCommand.FocusNear))
                {
                    HasFixedFocusValue = true;
                }

                take = new string[] { "FocusAuto" };
                AddLensRegister(take, skip, LensCommand.FocusAuto);

                take = new string[] { "IrisOpen" };
                if (AddLensRegister(take, skip, LensCommand.IrisOpen))
                {
                    HasIrisControl = true;
                }

                take = new string[] { "IrisClose" };
                if (AddLensRegister(take, skip, LensCommand.IrisClose))
                {
                    HasIrisControl = true;
                }

                take = new string[] { "IrisStop" };
                if (AddLensRegister(take, skip, LensCommand.IrisStop))
                {
                    HasIrisControl = true;
                }

                take = new string[] { "AutoIris" };
                if (AddLensRegister(take, skip, LensCommand.IrisAuto))
                {
                    HasIrisControl = true;
                }
            }
            catch (Exception ex)
            {
            }
        }

        private bool AddLensRegister(string[] lookFor, string[] skipThese, LensCommand lensCommand)
        {
            List<string> totalKeys = new List<string>();
            foreach (var item in lookFor)
            {
                var keys = Gvcp.RegistersDictionary.Keys.Where(x => x.Contains(item));
                foreach (var keyItem in keys)
                {
                    totalKeys.Add(keyItem);
                }
            }
            if (totalKeys?.Count() > 0)
            {
                foreach (var skipKey in skipThese)
                {
                    var toBeRemoved = totalKeys.Where(x => x.Contains(skipKey)).ToList();

                    foreach (var item in toBeRemoved)
                    {
                        totalKeys.Remove(item);
                    }
                }
                if (totalKeys.Count > 0)
                {
                    ;
                    if (!string.IsNullOrEmpty(Gvcp.RegistersDictionary[totalKeys.FirstOrDefault()]))
                    {
                        HasZoomControl = true;
                        lensControl.Add(lensCommand, Gvcp.RegistersDictionary[totalKeys.FirstOrDefault()]);
                        return true;
                    }
                }
            }
            return false;
        }

        private async void DecodePacketsAsync()
        {
            bool isResolutionUpdateRequired = true;
            int packetID = 0;
            int finalPacketID = 20000;
            int bufferLength = 0;
            byte[] singlePacket;
            var rawBytes = Array.Empty<byte>();
            try
            {
                while (IsStreaming)
                {
                    singlePacket = SocketRx.Receive(ref endPoint);
                    if (isResolutionUpdateRequired)
                    {
                        if (singlePacket.Length == 44) //Image Data Leader
                        {
                            if (singlePacket[11] == 01) //Payload type Image
                            {
                                var pixelFormat = (PixelFormat)(singlePacket[20] << 24 | singlePacket[21] << 16 | singlePacket[22] << 8 | singlePacket[23]);
                                var width = (uint)(singlePacket[24] << 24 | singlePacket[25] << 16 | singlePacket[26] << 8 | singlePacket[27]);
                                var height = (uint)(singlePacket[28] << 24 | singlePacket[29] << 16 | singlePacket[30] << 8 | singlePacket[31]);
                                int bytesPerPixel = (int)Math.Ceiling(singlePacket[21] / 8.0);
                                rawBytes = new byte[width * height * bytesPerPixel];
                                isResolutionUpdateRequired = false;
                            }
                        }
                    }
                    else
                    {
                        if (singlePacket.Length > 44) //Packet
                        {
                            packetID = (singlePacket[6] << 8) | singlePacket[7];
                            if (packetID < finalPacketID) //Check for final packet because final packet length maybe lesser than the regular packets
                            {
                                bufferLength = singlePacket.Length - 8;
                                Buffer.BlockCopy(singlePacket, 8, rawBytes, (packetID - 1) * bufferLength, bufferLength);
                            }
                            else
                            {
                                Buffer.BlockCopy(singlePacket, 8, rawBytes, (packetID - 1) * bufferLength, singlePacket.Length - 8);
                            }
                        }
                        else if (singlePacket.Length == 16) //Trailer packet size=16, Header Packet Size=44
                        {
                            if (finalPacketID == 20000)
                            {
                                finalPacketID = ((singlePacket[6] << 8) | singlePacket[7]) - 1;
                            }
                            FrameReady?.Invoke(this, rawBytes);
                        }
                    }
                }
                IsStreaming = false;
            }
            catch (Exception ex)
            {
                IsStreaming = false;
                await StopStream().ConfigureAwait(false);
            }
        }

        private void SetupRxSocket()
        {
            try
            {
                SocketRx.Client.Close();
                SocketRx.Close();
            }
            catch (Exception) { }
            try
            {
                SocketRx = new UdpClient((int)Port);
                endPoint = new IPEndPoint(IPAddress.Any, (int)Port);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            SocketRx.Client.ReceiveTimeout = 3000;
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