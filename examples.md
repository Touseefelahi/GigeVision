# Minimal Working Examples

Date: 2026-04-21

This file contains small examples that match the current APIs in this repository.

Important notes:

- Feature names are camera-XML dependent. The examples below use names that exist in `GenICam.Tests/Hikrobot.xml`, such as `Width`, `ReverseX`, and `AcquisitionFrameRate`.
- Some cameras expose engineering-unit features through `Converter` or `IntConverter` nodes instead of binding the feature directly to a register. The preferred public path is now `GetParameterValue<T>(...)` plus overloaded `SetCameraParameter(...)` calls.
- `Camera.FrameReady` reuses internal buffers. If you want to keep a frame after the callback returns, copy the `byte[]`.
- Float and converter-backed features now resolve through typed camera helpers without forcing callers down to the raw `PValue` path, but normal callers can stay on the unified generic read and overloaded write API.

## Namespaces

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using GenICam;
using GigeVision.Core;
using GigeVision.Core.Enums;
using GigeVision.Core.Models;
using GigeVision.Core.Services;
```

## 1. Open A Camera

This example discovers the first camera, sets the camera IP and host NIC, then loads the XML-backed parameters.

```csharp
public static async Task<Camera> OpenFirstCameraAsync()
{
    NetworkService.AllowAppThroughFirewall();

    var camera = new Camera();
    var devices = await camera.Gvcp.GetAllGigeDevicesInNetworkAsnyc().ConfigureAwait(false);

    var device = devices.FirstOrDefault()
        ?? throw new InvalidOperationException("No GigE camera found.");

    camera.IP = device.IP;
    camera.RxIP = device.NetworkIP;

    bool synced = await camera.SyncParameters().ConfigureAwait(false);
    if (!synced)
    {
        throw new InvalidOperationException("Failed to load camera parameters.");
    }

    Console.WriteLine($"Connected to {camera.IP}");
    Console.WriteLine($"Width={camera.Width}, Height={camera.Height}, PixelFormat={camera.PixelFormat}");

    return camera;
}
```

## 2. Read And Set An Integer Parameter

`Width` is a standard integer feature and is exposed directly by the high-level camera helpers.

```csharp
public static async Task ReadAndSetIntegerAsync(Camera camera)
{
    long? currentWidth = await camera.GetParameterValue<long>(nameof(RegisterName.Width)).ConfigureAwait(false);
    long minWidth = await camera.GetParameterMinValue(nameof(RegisterName.Width)).ConfigureAwait(false);
    long maxWidth = await camera.GetParameterMaxValue(nameof(RegisterName.Width)).ConfigureAwait(false);

    Console.WriteLine($"Width: current={currentWidth}, min={minWidth}, max={maxWidth}");

    long targetWidth = Math.Clamp(1280, minWidth, maxWidth);
    bool ok = await camera.SetCameraParameter(nameof(RegisterName.Width), targetWidth).ConfigureAwait(false);
    long? updatedWidth = await camera.GetParameterValue(nameof(RegisterName.Width)).ConfigureAwait(false);

    Console.WriteLine($"Width write status={ok}, new value={updatedWidth}");
}
```

If you want to change both width and height together, the camera class already provides a dedicated helper:

```csharp
bool resized = await camera.SetResolutionAsync(1280, 1024).ConfigureAwait(false);
Console.WriteLine($"Resolution write status={resized}");
```

## 3. Read And Set A Boolean Parameter

`ReverseX` is a boolean feature in the sample Hikrobot XML.

```csharp
public static async Task ReadAndSetBooleanAsync(Camera camera)
{
    bool? current = await camera.GetParameterValue<bool>("ReverseX").ConfigureAwait(false);
    if (current is null)
    {
        throw new InvalidOperationException("ReverseX is not available on this camera.");
    }

    Console.WriteLine($"ReverseX current value={current}");

    bool ok = await camera.SetCameraParameter("ReverseX", !current.Value).ConfigureAwait(false);
    bool? updated = await camera.GetParameterValue<bool>("ReverseX").ConfigureAwait(false);

    Console.WriteLine($"ReverseX write status={ok}, new value={updated}");
}
```

## 4. Read And Set A Float Feature

In this repository, float features and converter-backed engineering values can now be accessed through the generic getter and the overloaded `SetCameraParameter` method.

For example, the attached Lucid XML maps features like `ExposureTime` and `Gain` through converters such as:

- `ExposureTime -> Converter -> ExposureTimeRaw`
- `Gain -> Converter -> GainRaw`

That means the feature should be accessed through the typed parameter API, not by assuming the feature itself is a plain register node.

`ExposureTime` is a good converter-backed example because the Lucid XML converts microseconds to a raw register with `FROM * 125` and `TO / 125`.

```csharp
public static async Task ReadAndSetConverterBackedFeatureAsync(Camera camera)
{
    double? current = await camera.GetParameterValue<double>("ExposureTime").ConfigureAwait(false);
    Console.WriteLine($"ExposureTime current value={current} us");

    bool ok = await camera.SetCameraParameter("ExposureTime", 5000.0).ConfigureAwait(false);
    double? updated = await camera.GetParameterValue<double>("ExposureTime").ConfigureAwait(false);

    Console.WriteLine($"ExposureTime write status={ok}, new value={updated} us");
}
```

For plain integer-backed features like `Width`, the high-level helper is still fine:

```csharp
bool widthOk = await camera.SetCameraParameter(nameof(RegisterName.Width), 1280).ConfigureAwait(false);
long? width = await camera.GetParameterValue<long>(nameof(RegisterName.Width)).ConfigureAwait(false);

Console.WriteLine($"Width write status={widthOk}, new value={width}");
```

For another converter-backed feature, such as Lucid `Gain`, use the same typed float path:

```csharp
double? gain = await camera.GetParameterValue<double>("Gain").ConfigureAwait(false);
bool gainOk = await camera.SetCameraParameter("Gain", 12.5).ConfigureAwait(false);

Console.WriteLine($"Gain old={gain}, write status={gainOk}, new={await camera.GetParameterValue<double>("Gain").ConfigureAwait(false)}");
```

## 5. Receive Frames

This example opens the stream, waits for the first frame, copies it, then stops the stream.

```csharp
public static async Task<byte[]> ReceiveOneFrameAsync(Camera camera)
{
    var frameTcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

    void OnFrameReady(object sender, byte[] frame)
    {
        var copy = new byte[frame.Length];
        Buffer.BlockCopy(frame, 0, copy, 0, frame.Length);
        frameTcs.TrySetResult(copy);
    }

    camera.FrameReady += OnFrameReady;

    try
    {
        bool started = await camera.StartStreamAsync().ConfigureAwait(false);
        if (!started)
        {
            throw new InvalidOperationException("Failed to start the stream.");
        }

        byte[] firstFrame = await frameTcs.Task.ConfigureAwait(false);
        Console.WriteLine($"Received frame with {firstFrame.Length} bytes.");
        return firstFrame;
    }
    finally
    {
        camera.FrameReady -= OnFrameReady;

        if (camera.IsStreaming)
        {
            await camera.StopStream().ConfigureAwait(false);
        }
    }
}
```

For continuous reception, subscribe once and keep the stream open:

```csharp
camera.FrameReady += (sender, frame) =>
{
    var copy = new byte[frame.Length];
    Buffer.BlockCopy(frame, 0, copy, 0, frame.Length);

    Console.WriteLine($"Frame received: {copy.Length} bytes");
};

bool started = await camera.StartStreamAsync().ConfigureAwait(false);
Console.WriteLine($"Stream started={started}");

// Keep the process alive here until you want to stop.
// await Task.Delay(...);

if (camera.IsStreaming)
{
    await camera.StopStream().ConfigureAwait(false);
}
```

## 6. Full Minimal Flow

```csharp
Camera camera = await OpenFirstCameraAsync().ConfigureAwait(false);

await ReadAndSetIntegerAsync(camera).ConfigureAwait(false);
await ReadAndSetBooleanAsync(camera).ConfigureAwait(false);
await ReadAndSetConverterBackedFeatureAsync(camera).ConfigureAwait(false);

byte[] frame = await ReceiveOneFrameAsync(camera).ConfigureAwait(false);
Console.WriteLine($"Done. Final frame size={frame.Length} bytes");
```