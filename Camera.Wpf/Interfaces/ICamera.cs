using Camera.Wpf.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Camera.Wpf.Interfaces
{
    public interface ICamera
    {
        public int Fps { get; }
        bool IsStreaming { get; }
        Axis2D Resoluation { get; set; }
        public EventHandler<byte[]> OnFrameRecieved { get; set; }

        void ZoomIn();
        void ZoomOut();
        void FocusFar();
        void FocusNear();
        void SetZoomLevel(int level);
        Task StartStreamAsync();
        Task StopStreamAsync();
    }
}
