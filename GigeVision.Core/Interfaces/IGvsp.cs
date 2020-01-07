using GigeVision.Core.Enums;
using GigeVision.Core.Models;
using System;
using System.Threading.Tasks;

namespace GigeVision.Core.Interfaces
{
    public interface IGvsp
    {
        MotorControl MotorController { get; set; }
        IGvcp Gvcp { get; }
        EventHandler<byte[]> FrameReady { get; set; }
        EventHandler<string> Updates { get; set; }
        uint Payload { get; set; }
        uint Width { get; set; }
        uint Height { get; set; }
        uint OffsetX { get; set; }
        uint OffsetY { get; set; }
        PixelFormat PixelFormat { get; set; }

        /// <summary>
        /// This method will get current PC IP and Gets the Camera ip from Gvcp
        /// </summary>
        /// <param name="rxIP">If rxIP is not provided, method will detect system IP and use it</param>
        /// <param name="rxPort">It will set randomly when not provided</param>
        /// <returns></returns>
        Task<bool> StartStreamAsync(string rxIP = null, int rxPort = 0);

        Task<bool> StopStream();

        Task<bool> SetResolutionAsync(uint width, uint height);

        Task<bool> SetResolutionAsync(int width, int height);

        Task<bool> SetOffsetAsync(int offsetX, int offsetY);

        Task<bool> SetOffsetAsync(uint offsetX, uint offsetY);

        Task<bool> MotorControl(LensCommand command, uint value = 0);

        Task<bool> ReadRegisters();
    }
}