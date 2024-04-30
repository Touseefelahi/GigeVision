using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Emgu.CV;
using Emgu.CV.Reg;
using GigeVision.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GigeVisionLibrary.Avalonia.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private const double dpi = 96;
        private Camera camera;
        private int counterHeartbeat = 0;
        [ObservableProperty] private int fps, lossCount;
        private int fpsCounter;
        [ObservableProperty] private WriteableBitmap image = null!;
        private WriteableBitmap[] imageBuffers = null!;
        private string imagePath;
        [ObservableProperty] private string ip = "IP";
        private bool isRecording;
        [ObservableProperty] private bool isRecordingStarted;
        [ObservableProperty] private uint packetSize = 8000;
        [ObservableProperty] private TimeSpan recordingTime;
        private Stopwatch stopwatch;
        private Services.StreamReceiverParallelOpencv streamReceiver = new(2);
        private Thread threadProcessingPipeline = null!;
        [ObservableProperty] private long totalFrames;

        public MainWindowViewModel()
        {
            camera = new Camera
            {
                StreamReceiver = streamReceiver
            };
            camera.Gvcp.ElapsedOneSecond += OneSecondElapsed;
            Task.Run(RefreshCameraList);
        }

        private void OneSecondElapsed(object? sender, EventArgs e)
        {
            Fps = fpsCounter;
            LossCount = (int)streamReceiver.lossCount;
            TotalFrames = streamReceiver.imageIndex;
            fpsCounter = 0;
            if (isRecording)
            {
                RecordingTime = stopwatch.Elapsed;
            }
        }

        private unsafe void ProcessingPipeLine()
        {
            int localBufferIndex = 0;
            long frameOutCounter = 0;
            Mat coloredMat = new Mat(streamReceiver.height, streamReceiver.width, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            while (streamReceiver.IsReceiving)
            {
                streamReceiver.waitHandleFrame.Wait();
                int imageSize = streamReceiver.image[localBufferIndex].Width * streamReceiver.image[localBufferIndex].Height * 3;
                while (frameOutCounter < streamReceiver.frameInCounter)
                {
                    using (var frameBuffer = imageBuffers[localBufferIndex].Lock())
                    {
                        CvInvoke.CvtColor(streamReceiver.image[localBufferIndex], coloredMat, Emgu.CV.CvEnum.ColorConversion.BayerRg2Rgb);
                        Buffer.MemoryCopy(coloredMat.DataPointer.ToPointer(), frameBuffer.Address.ToPointer(), imageSize, imageSize);
                    }

                    if (isRecording)
                    {
                        string path = Path.Combine(imagePath, DateTime.Now.ToString("H mm ss fff") + ".bmp");
                        var cloned = streamReceiver.image[localBufferIndex].Clone();
                        cloned.Save(path);
                        cloned.Dispose();
                    }

                    localBufferIndex++;
                    if (localBufferIndex == streamReceiver.TotalBuffers)
                    {
                        localBufferIndex = 0;
                    }
                    Image = imageBuffers[localBufferIndex];
                    fpsCounter++;
                    frameOutCounter++;
                }
                if (counterHeartbeat++ >= 100)
                {
                    camera.Gvcp.ReadRegisterAsync(GigeVision.Core.Enums.GvcpRegister.GevCCP);
                    counterHeartbeat = 0;
                }
            }
        }

        private async Task RefreshCameraList()
        {
            var cameraIPs = await camera.Gvcp.GetAllGigeDevicesInNetworkAsnyc();
            if (cameraIPs != null && cameraIPs.Count > 0)
            {
                camera.IP = cameraIPs[0].IP;
                camera.RxIP = cameraIPs[0].NetworkIP;
                Ip = cameraIPs[0].IP;
            }
        }

        private void SaveFrame(Mat mat, string path)
        {
            mat.Save(path);
            mat.Dispose();
        }

        [RelayCommand]
        private async Task StartStream()
        {
            if (string.IsNullOrEmpty(camera.IP))
            {
                await RefreshCameraList();
            }

            if (camera.IsStreaming)
            {
                await camera.StopStream();
                return;
            }

            if (camera.Gvcp.IsXmlFileLoaded)
            {
                await camera.SyncParameters();
                Image = new WriteableBitmap(new PixelSize((int)camera.Width, (int)camera.Height), new Vector(dpi, dpi), PixelFormats.Bgr24);

                imageBuffers = new WriteableBitmap[streamReceiver.TotalBuffers];
                for (int i = 0; i < streamReceiver.TotalBuffers; i++)
                {
                    imageBuffers[i] = new WriteableBitmap(new PixelSize((int)camera.Width, (int)camera.Height), new Vector(dpi, dpi), PixelFormats.Bgr24);
                }
                camera.Payload = PacketSize;
                _ = await camera.StartStreamAsync();

                threadProcessingPipeline = new Thread(ProcessingPipeLine)
                {
                    Priority = ThreadPriority.Highest,
                    IsBackground = true
                };
                threadProcessingPipeline.Start();
            }
        }

        [RelayCommand]
        private void ToggleRecording()
        {
            if (IsRecordingStarted)//Recording Started
            {
                stopwatch ??= new Stopwatch();
                imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Captures", DateTime.Now.ToString("yy MM dd HH mm ss"));
                Directory.CreateDirectory(imagePath);
                isRecording = true;
                stopwatch.Restart();
            }
            else
            {
                isRecording = false;
                stopwatch.Stop();
            }
        }
    }
}
