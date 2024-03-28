using GigeVision.Core.Services;

public class Program
{
    static async Task Main()
    {
        var cameraStreamReader = new CameraStreamReceiver.CameraStreamReader();
        await cameraStreamReader.Receiver();
    }
}