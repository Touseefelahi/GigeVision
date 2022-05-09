using Camera.Wpf.Enums;
using GigeVision.Core;
using GigeVision.Core.Enums;
using GigeVision.Core.Interfaces;
using GigeVision.Core.Models;
using GigeVision.Core.Services;
using System;
using System.Threading.Tasks;

namespace Camera.Wpf.Models
{
    public class GigeDaylightCamera : Camera.Wpf.Interfaces.ICamera
    {
        public string IpTx { get; set; }
        private string IpRx { get; set; }
        public GigeDaylightCamera(string cameraIP)
        {
            Gvcp = new Gvcp(cameraIP);
            IpTx = cameraIP;
            PortRx = new Random().Next(5000, 6000);
            IpRx = NetworkService.GetMyIp();
            Gvsp = new Gvsp(IpRx, PortRx);

            Gvcp.ElapsedOneSecond += UpdateFps;
        }

        private void UpdateFps(object? sender, EventArgs e)
        {
            Fps = Gvsp.NetworkFps;
            Gvsp.NetworkFps = 0;
        }

        private async Task Init()
        {
            Resoluation = new Axis2D();
            await Gvcp.ReadAllRegisterAddressFromCameraAsync(IpTx);
            Resoluation.SetX((int)await Gvcp.RegistersDictionary["Width"].GetValueAsync());
            Resoluation.SetY((int)await Gvcp.RegistersDictionary["Height"].GetValueAsync());
            Gvsp.SetPayloadSize((uint)Resoluation.X, (uint)Resoluation.Y);

        }

        public void FocusFar()
        {
            throw new NotImplementedException();
        }

        public void FocusNear()
        {
            throw new NotImplementedException();
        }

        public void SetZoomLevel(int level)
        {
            throw new NotImplementedException();
        }

        public async Task StartStreamAsync()
        {
            await Init();
            if (Gvcp.RegistersDictionary.ContainsKey(nameof(RegisterName.AcquisitionStart)))
            {
                if (await Gvcp.TakeControl(true).ConfigureAwait(false))
                {
                    Gvsp.StartRxThread();

                    if (((await Gvcp.RegistersDictionary[nameof(GvcpRegister.GevSCPHostPort)].SetValueAsync((uint)PortRx).ConfigureAwait(false)) as GvcpReply).Status == GvcpStatus.GEV_STATUS_SUCCESS)
                    {
                        await Gvcp.RegistersDictionary[nameof(GvcpRegister.GevSCDA)].SetValueAsync(Converter.IpToNumber(IpRx)).ConfigureAwait(false);
                        await Gvcp.RegistersDictionary[nameof(GvcpRegister.GevSCPSPacketSize)].SetValueAsync(Gvsp.PayloadSize).ConfigureAwait(false);
                        await Gvcp.RegistersDictionary[nameof(RegisterName.AcquisitionStart)].SetValueAsync(Gvsp.PayloadSize).ConfigureAwait(false);
                        if (((await Gvcp.RegistersDictionary[nameof(RegisterName.AcquisitionStart)].SetValueAsync(1).ConfigureAwait(false)) as GvcpReply).Status == GvcpStatus.GEV_STATUS_SUCCESS)
                        {
                            IsStreaming = true;
                        }
                        else
                        {
                            await StopStreamAsync().ConfigureAwait(false);
                        }
                    }
                }
            }
            else
            {
                //Updates?.Invoke(this, "Fatal Error: Acquisition Start Register Not Found");
            }
        }

        public async Task StopStreamAsync()
        {
            await Gvcp.WriteRegisterAsync(GvcpRegister.GevSCDA, 0).ConfigureAwait(false);
            if (await Gvcp.LeaveControl().ConfigureAwait(false))
            {
                IsStreaming = false;
            }
        }

        public void ZoomIn()
        {
            throw new NotImplementedException();
        }

        public void ZoomOut()
        {
            throw new NotImplementedException();
        }


        private CameraInfo CameraInfo { get; set; }
        internal int ZoomLevel { get; set; }
        internal bool IsStream { get; set; }
        public int Fps { get; set; }
        internal Status Status { get; set; }
        public Axis2D Resoluation { get; set; }
        public int PortRx { get; private set; }
        private IGvcp Gvcp { get; }
        private IGvsp Gvsp { get; set; }
        public bool IsStreaming { get; private set; }
        public EventHandler<byte[]> OnFrameRecieved { get => Gvsp.FrameReady; set => Gvsp.FrameReady = value; }

    }
}
