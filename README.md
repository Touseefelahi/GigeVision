# GigeVision

Simple GigE Vision implementation with GVSP and GVCP support.

## Basic Usage

### Discover Cameras

```csharp
var camera = new Camera();
var listOfDevices = await camera.Gvcp.GetAllGigeDevicesInNetworkAsnyc().ConfigureAwait(false);
```

### Read Register

```csharp
var gvcp = new Gvcp
{
    CameraIp = "192.168.10.99"
};

var reply = await gvcp.ReadRegisterAsync("0x0D04").ConfigureAwait(false);
```

### Write Register

To write a register you must take control by writing `0x02` to the CCP register. You can do that manually or use `TakeControl()` from the library.

```csharp
var gvcp = new Gvcp
{
    CameraIp = "192.168.10.99"
};

if (await gvcp.TakeControl().ConfigureAwait(false))
{
    var reply = await gvcp.WriteRegisterAsync("0x0D04", 1000).ConfigureAwait(false);
}
else
{
    throw new Exception("Camera is already in control");
}
```

### Read Memory

```csharp
var gvcp = new Gvcp
{
    CameraIp = "192.168.10.99"
};

var reply = await gvcp.ReadMemoryAsync("0x0D04", 500).ConfigureAwait(false);
```

The `Gvcp` class has many overloads for register and memory operations. The implementation is in `GigeVision.Core/Services/Gvcp.cs`.

## Streaming

The current camera setup flow is property-based.

```csharp
var camera = new Camera();
camera.IP = "192.168.10.224";
camera.RxIP = "192.168.10.221"; // Optional, but recommended on multi-NIC systems

bool synced = await camera.SyncParameters().ConfigureAwait(false);
if (!synced)
{
    throw new InvalidOperationException("Failed to load camera parameters.");
}
```

Subscribe to the frame callback before starting the stream:

```csharp
camera.FrameReady += (sender, frame) =>
{
    // frame contains the raw image bytes
};
```

Start the stream:

```csharp
bool isStarted = await camera.StartStreamAsync(camera.RxIP).ConfigureAwait(false);
```

## Unified Parameter API

The preferred public API is now:

```csharp
long? width = await camera.GetParameterValue<long>(nameof(RegisterName.Width)).ConfigureAwait(false);
bool? reverseX = await camera.GetParameterValue<bool>("ReverseX").ConfigureAwait(false);
double? exposure = await camera.GetParameterValue<double>("ExposureTime").ConfigureAwait(false);

await camera.SetCameraParameter(nameof(RegisterName.Width), 1280L).ConfigureAwait(false);
await camera.SetCameraParameter("ReverseX", true).ConfigureAwait(false);
await camera.SetCameraParameter("ExposureTime", 5000.0).ConfigureAwait(false);
```

The older typed helpers like `GetFloatParameterValue`, `SetFloatParameter`, `GetBooleanParameterValue`, and `SetBooleanParameter` are still present for compatibility, but new code should prefer the generic getter and the overloaded `SetCameraParameter` methods.

## Force IP

This example sets the IP of the first detected camera to `192.168.10.243`.

```csharp
var camera = new Camera();
var listOfDevices = await camera.Gvcp.GetAllGigeDevicesInNetworkAsnyc().ConfigureAwait(false);

if (listOfDevices.Count > 0)
{
    await camera.Gvcp.ForceIPAsync(listOfDevices[0].MacAddress, "192.168.10.243").ConfigureAwait(false);
}
```

## Examples

- Minimal working samples are in `examples.md`.
- A WPF sample app is in `GigeVisionLibrary.Test.Wpf/MainWindow.xaml.cs`.
