using GigeVision.Core.Services;

namespace GigeVisionLibrary.Test.Con
{
    internal class Program
    {
        /// <summary>
        /// On new frame this method will trigger
        /// </summary>
        /// <param name="sender">Additional Info</param>
        /// <param name="e">Complete Image bytes</param>
        private static void FrameReady(object? sender, byte[] e)
        {
            Console.WriteLine($"{e.Length} Image data bytes received from the camera");
        }

        private static async void GetAllCamerasAndStartStream(Camera camera)
        {
            var listOfCameras = await camera.Gvcp.GetAllGigeDevicesInNetworkAsnyc().ConfigureAwait(false);
            Console.WriteLine($"{listOfCameras.Count} Camera found in the network");
            if (listOfCameras.Count > 0)
            {
                camera.IP = listOfCameras[0].IP;
            }
            var isStreamStarted = await camera.StartStreamAsync();
            Console.WriteLine($"Stream Started: {isStreamStarted}");
        }

        private static void Main(string[] args)
        {
            GigeVision.Core.NetworkService.AllowAppThroughFirewall();
            Camera camera = new();
            camera.FrameReady += FrameReady;
            camera.Updates += Updates;
            GetAllCamerasAndStartStream(camera);
            Thread.Sleep(10000);
            camera.StopStream();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Updates(object? sender, string e)
        {
            Console.WriteLine($"Update from Library: {e}");
        }
    }
}