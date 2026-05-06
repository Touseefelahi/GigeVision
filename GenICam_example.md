# GenICam and GigE Vision End-to-End Example

This document is a repository-accurate example for working with a compliant GenICam GigE camera through this project.

It covers:

1. Discovering cameras on the network
2. Connecting to a camera
3. Grabbing a single frame
4. Streaming continuously
5. Reading and setting GenICam features
6. Converting raw frame bytes to a displayable image
7. Handling the common GVSP pixel format cases discussed for this repository

## Important Notes About This Repository

The root README is slightly out of date for direct camera construction. In the current codebase you do this:

```csharp
var camera = new Camera();
camera.IP = "192.168.10.224";
```

not this:

```csharp
var camera = new Camera("192.168.10.224");
```

Also note:

1. `StartStreamAsync` already loads the camera XML, synchronizes width, height, offset, and pixel format, takes control, configures the GVSP destination, and starts acquisition.
2. `FrameReady` gives you one complete frame, not a packet.
3. The receiver reuses buffers internally, so copy the frame bytes inside the callback if you need to keep them.
4. On machines with multiple NICs, set `camera.RxIP` to the host NIC that is on the same subnet as the camera.

## Namespaces Used In The Examples

```csharp
using GigeVision.Core;
using GigeVision.Core.Enums;
using GigeVision.Core.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
```

If you want Bayer and YUV conversion examples, also add Emgu CV, which is already used elsewhere in this repository:

```csharp
using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GvPixelFormat = GigeVision.Core.Enums.PixelFormat;
```

## 1. Discover Cameras

This is the standard entry point.

```csharp
NetworkService.AllowAppThroughFirewall();

var camera = new Camera();
var devices = await camera.Gvcp.GetAllGigeDevicesInNetworkAsnyc();

foreach (var device in devices)
{
    Console.WriteLine($"Camera IP: {device.IP}, Host NIC: {device.NetworkIP}, Model: {device.Model}");
}
```

If a device is found, the `CameraInformation.NetworkIP` value is the host interface this library discovered for that device. That is usually the safest value to assign to `camera.RxIP`.

## 2. Connect To The First Camera

```csharp
var camera = new Camera();
var devices = await camera.Gvcp.GetAllGigeDevicesInNetworkAsnyc();

var device = devices.FirstOrDefault()
    ?? throw new InvalidOperationException("No GigE camera found.");

camera.IP = device.IP;
camera.RxIP = device.NetworkIP;

await camera.SyncParameters();

Console.WriteLine($"Connected to {camera.IP}");
Console.WriteLine($"Width: {camera.Width}");
Console.WriteLine($"Height: {camera.Height}");
Console.WriteLine($"PixelFormat: {camera.PixelFormat}");
```

## 3. Grab A Single Frame

This is the practical "single frame" pattern for this repository. There is no separate one-shot public grab API on `ICamera`; you start the stream, wait for the first completed frame, then stop.

```csharp
public static async Task<byte[]> GrabSingleFrameAsync(Camera camera, TimeSpan timeout)
{
    var frameTcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

    void OnFrameReady(object? sender, byte[] frameBytes)
    {
        var copy = new byte[frameBytes.Length];
        Buffer.BlockCopy(frameBytes, 0, copy, 0, frameBytes.Length);
        frameTcs.TrySetResult(copy);
    }

    camera.FrameReady += OnFrameReady;

    try
    {
        bool started = await camera.StartStreamAsync();
        if (!started)
        {
            throw new InvalidOperationException("StartStreamAsync returned false.");
        }

        Task completed = await Task.WhenAny(frameTcs.Task, Task.Delay(timeout));
        if (completed != frameTcs.Task)
        {
            throw new TimeoutException("Timed out waiting for the first frame.");
        }

        return await frameTcs.Task;
    }
    finally
    {
        camera.FrameReady -= OnFrameReady;

        if (camera.IsStreaming)
        {
            await camera.StopStream();
        }
    }
}
```

Usage:

```csharp
var camera = new Camera();
var devices = await camera.Gvcp.GetAllGigeDevicesInNetworkAsnyc();
var device = devices.First();

camera.IP = device.IP;
camera.RxIP = device.NetworkIP;

byte[] firstFrame = await GrabSingleFrameAsync(camera, TimeSpan.FromSeconds(3));

Console.WriteLine($"Bytes received: {firstFrame.Length}");
Console.WriteLine($"Width: {camera.Width}");
Console.WriteLine($"Height: {camera.Height}");
Console.WriteLine($"PixelFormat: {camera.PixelFormat}");
```

## 4. Stream Continuously

This is the minimal continuous-stream pattern.

```csharp
var camera = new Camera();
var devices = await camera.Gvcp.GetAllGigeDevicesInNetworkAsnyc();
var device = devices.First();

camera.IP = device.IP;
camera.RxIP = device.NetworkIP;

camera.FrameReady += (sender, frameBytes) =>
{
    var copy = new byte[frameBytes.Length];
    Buffer.BlockCopy(frameBytes, 0, copy, 0, frameBytes.Length);

    Console.WriteLine($"Frame bytes: {copy.Length}");
};

bool started = await camera.StartStreamAsync();
Console.WriteLine($"Stream started: {started}");

Console.ReadLine();
await camera.StopStream();
```

## 5. Read And Set GenICam Features

The `Camera` class provides higher-level helpers that sit on top of the XML-backed GenICam node map.

### Read Common Features

```csharp
await camera.SyncParameters();

Console.WriteLine(camera.Width);
Console.WriteLine(camera.Height);
Console.WriteLine(camera.OffsetX);
Console.WriteLine(camera.OffsetY);
Console.WriteLine(camera.PixelFormat);
```

### Use Dedicated Helpers For Width, Height, And Offset

These are safer than generic writes because this repository already wraps the control flow for them.

```csharp
bool resolutionSet = await camera.SetResolutionAsync(1280, 1024);
bool offsetSet = await camera.SetOffsetAsync(0, 0);

await camera.SyncParameters();
```

### Generic Feature Read And Write

```csharp
long? widthValue = await camera.GetParameterValue(nameof(RegisterName.Width));
long widthMin = await camera.GetParameterMinValue(nameof(RegisterName.Width));
long widthMax = await camera.GetParameterMaxValue(nameof(RegisterName.Width));

Console.WriteLine($"Width value: {widthValue}");
Console.WriteLine($"Width range: {widthMin} - {widthMax}");
```

For Lucid and other cameras that expose engineering values through `Converter` nodes, use the typed overloads instead of forcing the feature through the integer path. For example, in the attached Lucid XML `AcquisitionFrameRate` is a `Float` whose `pValue` points at converter `N108`, and that converter writes the integer `AcquisitionFrameTime` register using `1e6 / FROM`.

```csharp
bool frameRateEnableSet = await camera.SetCameraParameter("AcquisitionFrameRateEnable", true);
bool frameRateSet = await camera.SetCameraParameter("AcquisitionFrameRate", 1.0);

bool? frameRateEnabled = await camera.GetParameterValue<bool>("AcquisitionFrameRateEnable");
double? frameRate = await camera.GetParameterValue<double>("AcquisitionFrameRate");

Console.WriteLine($"AcquisitionFrameRateEnable: {frameRateEnabled}");
Console.WriteLine($"AcquisitionFrameRate: {frameRate} Hz");
```

If you drop down to `camera.Gvcp.GetRegister(...)`, treat converter-backed features as `IDoubleValue` or `IFloat` feature categories, not as plain integer registers.

### Set Pixel Format Through The Generic Helper

```csharp
bool pixelFormatSet = await camera.SetCameraParameter(
    nameof(RegisterName.PixelFormat),
    (long)PixelFormat.Mono8);

await camera.SyncParameters();
```

### Probe Vendor-Specific Features Such As Exposure And Gain

Feature names like `ExposureTime`, `ExposureTimeAbs`, `ExposureTimeRaw`, `Gain`, and `GainRaw` depend on the camera XML.

```csharp
static async Task<string?> FindFeature(Camera camera, params string[] names)
{
    foreach (string name in names)
    {
        if (await camera.GetParameter(name) != null)
        {
            return name;
        }
    }

    return null;
}

string? exposureName = await FindFeature(camera, "ExposureTime", "ExposureTimeAbs", "ExposureTimeRaw");
if (exposureName != null)
{
    long min = await camera.GetParameterMinValue(exposureName);
    long max = await camera.GetParameterMaxValue(exposureName);
    long target = Math.Clamp(5000, min, max);

    bool ok = await camera.SetCameraParameter(exposureName, target);
    long? actual = await camera.GetParameterValue(exposureName);

    Console.WriteLine($"{exposureName}: set={ok}, value={actual}, range=[{min}, {max}]");
}

string? gainName = await FindFeature(camera, "Gain", "GainRaw", "AnalogGain", "DigitalGainAll");
if (gainName != null)
{
    long min = await camera.GetParameterMinValue(gainName);
    long max = await camera.GetParameterMaxValue(gainName);
    long target = Math.Clamp(10, min, max);

    bool ok = await camera.SetCameraParameter(gainName, target);
    long? actual = await camera.GetParameterValue(gainName);

    Console.WriteLine($"{gainName}: set={ok}, value={actual}, range=[{min}, {max}]");
}
```

## 6. Convert Raw Bytes To A Displayable Image

The conversion path depends on `camera.PixelFormat`.

### Practical Format Categories

1. `Mono8`: bytes already represent a grayscale image
2. `RGB8Packed` or `BGR8Packed`: bytes already represent a 24-bit color image
3. `BayerGR8`, `BayerRG8`, `BayerGB8`, `BayerBG8`: raw sensor mosaic, must be demosaiced
4. `Mono10Packed`, `Mono12Packed`: packed mono data, must be unpacked and scaled for display
5. `BayerGR10Packed`, `BayerRG10Packed`, `BayerGB10Packed`, `BayerBG10Packed`, and the 12-bit packed Bayer variants: unpack, scale, then demosaic
6. `Mono10`, `Mono12`, `Mono14`, `Mono16`: unpacked high bit-depth mono, scale to 8-bit for display
7. `BayerGR10`, `BayerRG10`, `BayerGB10`, `BayerBG10`, `BayerGR12`, `BayerRG12`, `BayerGB12`, `BayerBG12`, `BayerGR16`, `BayerRG16`, `BayerGB16`, `BayerBG16`: unpacked high bit-depth Bayer, scale to 8-bit for display, then demosaic
8. `YUV422Packed` and `YUYVPacked`: convert through OpenCV

### Full WPF Bitmap Helper

This helper returns a `BitmapSource` suitable for display in WPF. It keeps the output simple: Gray8 or Bgr24.

```csharp
using Emgu.CV;
using Emgu.CV.CvEnum;
using GvPixelFormat = GigeVision.Core.Enums.PixelFormat;
using System;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public static class GenICamFrameConverter
{
    public static BitmapSource ToBitmapSource(byte[] frameBytes, int width, int height, GvPixelFormat pixelFormat)
    {
        if (frameBytes == null)
        {
            throw new ArgumentNullException(nameof(frameBytes));
        }

        return pixelFormat switch
        {
            GvPixelFormat.Mono8 => CreateGray8Bitmap(frameBytes, width, height),
            GvPixelFormat.Mono8Signed => CreateGray8Bitmap(OffsetSignedToUnsigned(frameBytes), width, height),

            GvPixelFormat.RGB8Packed => CreateRgb24Bitmap(frameBytes, width, height),
            GvPixelFormat.BGR8Packed => CreateBgr24Bitmap(frameBytes, width, height),

            GvPixelFormat.BayerRG8 => CreateBayer8Bitmap(frameBytes, width, height, ColorConversion.BayerRg2Bgr),
            GvPixelFormat.BayerBG8 => CreateBayer8Bitmap(frameBytes, width, height, ColorConversion.BayerBg2Bgr),
            GvPixelFormat.BayerGR8 => CreateBayer8Bitmap(frameBytes, width, height, ColorConversion.BayerGr2Bgr),
            GvPixelFormat.BayerGB8 => CreateBayer8Bitmap(frameBytes, width, height, ColorConversion.BayerGb2Bgr),

            GvPixelFormat.Mono10Packed => CreateGray8Bitmap(ScaleTo8Bit(UnpackToUShorts(frameBytes, 10, width * height), 10), width, height),
            GvPixelFormat.Mono12Packed => CreateGray8Bitmap(ScaleTo8Bit(UnpackToUShorts(frameBytes, 12, width * height), 12), width, height),

            GvPixelFormat.Mono10 => CreateGray8Bitmap(ScaleTo8Bit(BytesToUShorts(frameBytes), 10), width, height),
            GvPixelFormat.Mono12 => CreateGray8Bitmap(ScaleTo8Bit(BytesToUShorts(frameBytes), 12), width, height),
            GvPixelFormat.Mono14 => CreateGray8Bitmap(ScaleTo8Bit(BytesToUShorts(frameBytes), 14), width, height),
            GvPixelFormat.Mono16 => CreateGray8Bitmap(ScaleTo8Bit(BytesToUShorts(frameBytes), 16), width, height),

            GvPixelFormat.BayerRG10Packed => CreateBayerHighBitBitmap(UnpackToUShorts(frameBytes, 10, width * height), width, height, 10, ColorConversion.BayerRg2Bgr),
            GvPixelFormat.BayerBG10Packed => CreateBayerHighBitBitmap(UnpackToUShorts(frameBytes, 10, width * height), width, height, 10, ColorConversion.BayerBg2Bgr),
            GvPixelFormat.BayerGR10Packed => CreateBayerHighBitBitmap(UnpackToUShorts(frameBytes, 10, width * height), width, height, 10, ColorConversion.BayerGr2Bgr),
            GvPixelFormat.BayerGB10Packed => CreateBayerHighBitBitmap(UnpackToUShorts(frameBytes, 10, width * height), width, height, 10, ColorConversion.BayerGb2Bgr),

            GvPixelFormat.BayerRG12Packed => CreateBayerHighBitBitmap(UnpackToUShorts(frameBytes, 12, width * height), width, height, 12, ColorConversion.BayerRg2Bgr),
            GvPixelFormat.BayerBG12Packed => CreateBayerHighBitBitmap(UnpackToUShorts(frameBytes, 12, width * height), width, height, 12, ColorConversion.BayerBg2Bgr),
            GvPixelFormat.BayerGR12Packed => CreateBayerHighBitBitmap(UnpackToUShorts(frameBytes, 12, width * height), width, height, 12, ColorConversion.BayerGr2Bgr),
            GvPixelFormat.BayerGB12Packed => CreateBayerHighBitBitmap(UnpackToUShorts(frameBytes, 12, width * height), width, height, 12, ColorConversion.BayerGb2Bgr),

            GvPixelFormat.BayerRG10 => CreateBayerHighBitBitmap(BytesToUShorts(frameBytes), width, height, 10, ColorConversion.BayerRg2Bgr),
            GvPixelFormat.BayerBG10 => CreateBayerHighBitBitmap(BytesToUShorts(frameBytes), width, height, 10, ColorConversion.BayerBg2Bgr),
            GvPixelFormat.BayerGR10 => CreateBayerHighBitBitmap(BytesToUShorts(frameBytes), width, height, 10, ColorConversion.BayerGr2Bgr),
            GvPixelFormat.BayerGB10 => CreateBayerHighBitBitmap(BytesToUShorts(frameBytes), width, height, 10, ColorConversion.BayerGb2Bgr),

            GvPixelFormat.BayerRG12 => CreateBayerHighBitBitmap(BytesToUShorts(frameBytes), width, height, 12, ColorConversion.BayerRg2Bgr),
            GvPixelFormat.BayerBG12 => CreateBayerHighBitBitmap(BytesToUShorts(frameBytes), width, height, 12, ColorConversion.BayerBg2Bgr),
            GvPixelFormat.BayerGR12 => CreateBayerHighBitBitmap(BytesToUShorts(frameBytes), width, height, 12, ColorConversion.BayerGr2Bgr),
            GvPixelFormat.BayerGB12 => CreateBayerHighBitBitmap(BytesToUShorts(frameBytes), width, height, 12, ColorConversion.BayerGb2Bgr),

            GvPixelFormat.BayerRG16 => CreateBayerHighBitBitmap(BytesToUShorts(frameBytes), width, height, 16, ColorConversion.BayerRg2Bgr),
            GvPixelFormat.BayerBG16 => CreateBayerHighBitBitmap(BytesToUShorts(frameBytes), width, height, 16, ColorConversion.BayerBg2Bgr),
            GvPixelFormat.BayerGR16 => CreateBayerHighBitBitmap(BytesToUShorts(frameBytes), width, height, 16, ColorConversion.BayerGr2Bgr),
            GvPixelFormat.BayerGB16 => CreateBayerHighBitBitmap(BytesToUShorts(frameBytes), width, height, 16, ColorConversion.BayerGb2Bgr),

            GvPixelFormat.YUV422Packed => CreateYuvBitmap(frameBytes, width, height, ColorConversion.Yuv2BgrUyvy),
            GvPixelFormat.YUYVPacked => CreateYuvBitmap(frameBytes, width, height, ColorConversion.Yuv2BgrYuy2),

            _ => throw new NotSupportedException($"Display conversion is not implemented for {pixelFormat}.")
        };
    }

    private static BitmapSource CreateGray8Bitmap(byte[] grayBytes, int width, int height)
    {
        return BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Gray8,
            null,
            grayBytes,
            width);
    }

    private static BitmapSource CreateRgb24Bitmap(byte[] rgbBytes, int width, int height)
    {
        return BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Rgb24,
            null,
            rgbBytes,
            width * 3);
    }

    private static BitmapSource CreateBgr24Bitmap(byte[] bgrBytes, int width, int height)
    {
        return BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Bgr24,
            null,
            bgrBytes,
            width * 3);
    }

    private static BitmapSource CreateBayer8Bitmap(byte[] raw8, int width, int height, ColorConversion conversion)
    {
        using Mat raw = new Mat(height, width, DepthType.Cv8U, 1);
        using Mat color = new Mat(height, width, DepthType.Cv8U, 3);

        Marshal.Copy(raw8, 0, raw.DataPointer, raw8.Length);
        CvInvoke.CvtColor(raw, color, conversion);

        byte[] bgrBytes = ExtractBytes(color, width * height * 3);
        return CreateBgr24Bitmap(bgrBytes, width, height);
    }

    private static BitmapSource CreateBayerHighBitBitmap(ushort[] rawValues, int width, int height, int bitDepth, ColorConversion conversion)
    {
        byte[] normalized = ScaleTo8Bit(rawValues, bitDepth);
        return CreateBayer8Bitmap(normalized, width, height, conversion);
    }

    private static BitmapSource CreateYuvBitmap(byte[] yuvBytes, int width, int height, ColorConversion conversion)
    {
        using Mat raw = new Mat(height, width, DepthType.Cv8U, 2);
        using Mat color = new Mat(height, width, DepthType.Cv8U, 3);

        Marshal.Copy(yuvBytes, 0, raw.DataPointer, yuvBytes.Length);
        CvInvoke.CvtColor(raw, color, conversion);

        byte[] bgrBytes = ExtractBytes(color, width * height * 3);
        return CreateBgr24Bitmap(bgrBytes, width, height);
    }

    private static byte[] OffsetSignedToUnsigned(byte[] signedBytes)
    {
        byte[] result = new byte[signedBytes.Length];

        for (int i = 0; i < signedBytes.Length; i++)
        {
            result[i] = unchecked((byte)(signedBytes[i] + 128));
        }

        return result;
    }

    private static ushort[] BytesToUShorts(byte[] bytes)
    {
        if ((bytes.Length & 1) != 0)
        {
            throw new ArgumentException("Expected an even number of bytes for unpacked 16-bit data.", nameof(bytes));
        }

        ushort[] values = new ushort[bytes.Length / 2];
        Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
        return values;
    }

    private static ushort[] UnpackToUShorts(byte[] packedBytes, int bitsPerPixel, int pixelCount)
    {
        ushort[] values = new ushort[pixelCount];
        int bitOffset = 0;
        uint mask = (uint)((1 << bitsPerPixel) - 1);

        for (int i = 0; i < pixelCount; i++)
        {
            int byteIndex = bitOffset >> 3;
            int intraByteBitOffset = bitOffset & 7;

            uint chunk = 0;
            if (byteIndex < packedBytes.Length) chunk |= packedBytes[byteIndex];
            if (byteIndex + 1 < packedBytes.Length) chunk |= (uint)packedBytes[byteIndex + 1] << 8;
            if (byteIndex + 2 < packedBytes.Length) chunk |= (uint)packedBytes[byteIndex + 2] << 16;
            if (byteIndex + 3 < packedBytes.Length) chunk |= (uint)packedBytes[byteIndex + 3] << 24;

            values[i] = (ushort)((chunk >> intraByteBitOffset) & mask);
            bitOffset += bitsPerPixel;
        }

        return values;
    }

    private static byte[] ScaleTo8Bit(ushort[] values, int sourceBitDepth)
    {
        if (sourceBitDepth < 1 || sourceBitDepth > 16)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceBitDepth));
        }

        int maxValue = (1 << sourceBitDepth) - 1;
        byte[] result = new byte[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            result[i] = (byte)((values[i] * 255 + (maxValue / 2)) / maxValue);
        }

        return result;
    }

    private static byte[] ExtractBytes(Mat mat, int expectedLength)
    {
        byte[] bytes = new byte[expectedLength];
        Marshal.Copy(mat.DataPointer, bytes, 0, expectedLength);
        return bytes;
    }
}
```

## 7. Use The Helper In A Single-Frame Workflow

```csharp
var camera = new Camera();
var devices = await camera.Gvcp.GetAllGigeDevicesInNetworkAsnyc();
var device = devices.First();

camera.IP = device.IP;
camera.RxIP = device.NetworkIP;

byte[] firstFrame = await GrabSingleFrameAsync(camera, TimeSpan.FromSeconds(3));

BitmapSource bitmap = GenICamFrameConverter.ToBitmapSource(
    firstFrame,
    (int)camera.Width,
    (int)camera.Height,
    camera.PixelFormat);
```

## 8. Use The Helper In A WPF Streaming Workflow

```csharp
camera.FrameReady += (sender, frameBytes) =>
{
    var copy = new byte[frameBytes.Length];
    Buffer.BlockCopy(frameBytes, 0, copy, 0, frameBytes.Length);

    BitmapSource bitmap = GenICamFrameConverter.ToBitmapSource(
        copy,
        (int)camera.Width,
        (int)camera.Height,
        camera.PixelFormat);

    Dispatcher.Invoke(() => imageControl.Source = bitmap);
};

await camera.StartStreamAsync();
```

## 9. When To Use The OpenCV Stream Receiver Instead

The repository already contains an OpenCV-based receiver in `GigeVision.OpenCV`.

That path is useful when:

1. You want `Mat` buffers directly instead of `byte[]`
2. You want to process frames with OpenCV immediately
3. You want the same general approach used by the Avalonia sample
4. You want a high-throughput processing loop that avoids allocating a new `byte[]` per frame

### What The OpenCV Receiver Actually Gives You

`StreamReceiverParallelOpencv` allocates a rotating array of OpenCV Mats:

1. `streamReceiver.image[i]` is a ring buffer of raw image Mats
2. The Mats are created as single-channel `Cv8U` or `Cv16U`
3. `waitHandleFrame` is released whenever a full frame has been assembled
4. `frameInCounter` tracks how many complete frames have been published into the ring buffer
5. `imageIndex` tracks total frames seen
6. `lossCount` tracks frames dropped because packet loss exceeded tolerance

In other words, the OpenCV receiver does not expose a `FrameReady` callback. You consume frames by waiting on `waitHandleFrame` and reading from `streamReceiver.image[bufferIndex]`.

### Important Limitation Of The OpenCV Receiver

The current OpenCV receiver creates raw Mats as either 8-bit single-channel or 16-bit single-channel depending on `GvspInfo.BytesPerPixel`.

That makes it a good fit for:

1. `Mono8`
2. `BayerRG8`, `BayerBG8`, `BayerGR8`, `BayerGB8`
3. Unpacked high-bit-depth formats like `Mono16` or unpacked Bayer 10, 12, and 16-bit formats that arrive as 16-bit samples

It is not by itself a complete solution for packed 10-bit and 12-bit formats such as:

1. `Mono10Packed`
2. `Mono12Packed`
3. `BayerRG10Packed`
4. `BayerBG10Packed`
5. `BayerGR10Packed`
6. `BayerGB10Packed`
7. The packed 12-bit Bayer variants

For those packed formats, prefer the `byte[]` conversion path from section 6 or add an unpack step before wrapping the data in a Mat.

### Project Setup For OpenCV

If you are using the OpenCV path in your own application project, make sure your app references Emgu CV and the Windows runtime package, not only `GigeVision.OpenCV`.

Typical package references for a Windows app look like this:

```xml
<ItemGroup>
    <PackageReference Include="Emgu.CV" Version="4.9.0.5494" />
    <PackageReference Include="Emgu.CV.runtime.windows" Version="4.9.0.5494" />
</ItemGroup>
```

Example setup:

```csharp
var streamReceiver = new GigeVision.OpenCV.StreamReceiverParallelOpencv(2);

var camera = new Camera
{
    StreamReceiver = streamReceiver
};
```

### OpenCV Setup With Camera Discovery

```csharp
using Emgu.CV;
using Emgu.CV.CvEnum;
using GigeVision.Core;
using GigeVision.Core.Enums;
using GigeVision.Core.Services;
using GigeVision.OpenCV;

NetworkService.AllowAppThroughFirewall();

var streamReceiver = new StreamReceiverParallelOpencv(totalBuffers: 3);
var camera = new Camera
{
    StreamReceiver = streamReceiver
};

var devices = await camera.Gvcp.GetAllGigeDevicesInNetworkAsnyc();
var device = devices.FirstOrDefault()
    ?? throw new InvalidOperationException("No camera found.");

camera.IP = device.IP;
camera.RxIP = device.NetworkIP;

await camera.SyncParameters();

Console.WriteLine($"Width: {camera.Width}");
Console.WriteLine($"Height: {camera.Height}");
Console.WriteLine($"PixelFormat: {camera.PixelFormat}");
```

### Single-Frame Example Using The OpenCV Receiver

This pattern is the OpenCV equivalent of the one-shot byte-array example.

```csharp
public static async Task<Mat> GrabSingleRawMatAsync(Camera camera, StreamReceiverParallelOpencv streamReceiver, TimeSpan timeout)
{
    int localBufferIndex = 0;

    bool started = await camera.StartStreamAsync();
    if (!started)
    {
        throw new InvalidOperationException("StartStreamAsync returned false.");
    }

    try
    {
        using var cts = new CancellationTokenSource(timeout);
        await streamReceiver.waitHandleFrame.WaitAsync(cts.Token);

        return streamReceiver.image[localBufferIndex].Clone();
    }
    finally
    {
        if (camera.IsStreaming)
        {
            await camera.StopStream();
        }
    }
}
```

If the camera is Bayer and you want a color image immediately:

```csharp
Mat raw = await GrabSingleRawMatAsync(camera, streamReceiver, TimeSpan.FromSeconds(3));
Mat display = ConvertRawMatToBgr(raw, camera.PixelFormat);

display.Save("frame.png");

raw.Dispose();
display.Dispose();
```

### Continuous OpenCV Processing Loop

This is the most important OpenCV pattern in this repository.

The receiver writes incoming frames into a ring buffer. Your processing loop should:

1. Keep its own `localBufferIndex`
2. Keep its own `frameOutCounter`
3. Wait on `waitHandleFrame`
4. Process frames until `frameOutCounter == frameInCounter`
5. Advance `localBufferIndex` modulo `TotalBuffers`

That is the same model used by the Avalonia sample.

```csharp
int localBufferIndex = 0;
long frameOutCounter = 0;

bool started = await camera.StartStreamAsync();
if (!started)
{
    throw new InvalidOperationException("Failed to start stream.");
}

try
{
    while (streamReceiver.IsReceiving)
    {
        await streamReceiver.waitHandleFrame.WaitAsync();

        while (frameOutCounter < streamReceiver.frameInCounter)
        {
            Mat raw = streamReceiver.image[localBufferIndex];

            using Mat display = ConvertRawMatToBgr(raw, camera.PixelFormat);

            Console.WriteLine($"Frame {frameOutCounter}, size={display.Width}x{display.Height}, loss={streamReceiver.lossCount}");

            localBufferIndex++;
            if (localBufferIndex == streamReceiver.TotalBuffers)
            {
                localBufferIndex = 0;
            }

            frameOutCounter++;
        }
    }
}
finally
{
    if (camera.IsStreaming)
    {
        await camera.StopStream();
    }
}
```

### Detailed Raw-Mat To Display-Mat Conversion

This helper is the OpenCV-side equivalent of the byte-array bitmap helper.

It converts the raw single-channel Mat produced by `StreamReceiverParallelOpencv` into a BGR Mat suitable for display, saving, or further CV processing.

```csharp
using Emgu.CV;
using Emgu.CV.CvEnum;
using GvPixelFormat = GigeVision.Core.Enums.PixelFormat;

public static Mat ConvertRawMatToBgr(Mat raw, GvPixelFormat pixelFormat)
{
    if (raw == null)
    {
        throw new ArgumentNullException(nameof(raw));
    }

    return pixelFormat switch
    {
        GvPixelFormat.Mono8 => Gray8ToBgr(raw),
        GvPixelFormat.Mono10 => Gray16ToBgr(raw, 10),
        GvPixelFormat.Mono12 => Gray16ToBgr(raw, 12),
        GvPixelFormat.Mono14 => Gray16ToBgr(raw, 14),
        GvPixelFormat.Mono16 => Gray16ToBgr(raw, 16),

        GvPixelFormat.BayerRG8 => Bayer8ToBgr(raw, ColorConversion.BayerRg2Bgr),
        GvPixelFormat.BayerBG8 => Bayer8ToBgr(raw, ColorConversion.BayerBg2Bgr),
        GvPixelFormat.BayerGR8 => Bayer8ToBgr(raw, ColorConversion.BayerGr2Bgr),
        GvPixelFormat.BayerGB8 => Bayer8ToBgr(raw, ColorConversion.BayerGb2Bgr),

        GvPixelFormat.BayerRG10 => Bayer16ToBgr(raw, 10, ColorConversion.BayerRg2Bgr),
        GvPixelFormat.BayerBG10 => Bayer16ToBgr(raw, 10, ColorConversion.BayerBg2Bgr),
        GvPixelFormat.BayerGR10 => Bayer16ToBgr(raw, 10, ColorConversion.BayerGr2Bgr),
        GvPixelFormat.BayerGB10 => Bayer16ToBgr(raw, 10, ColorConversion.BayerGb2Bgr),

        GvPixelFormat.BayerRG12 => Bayer16ToBgr(raw, 12, ColorConversion.BayerRg2Bgr),
        GvPixelFormat.BayerBG12 => Bayer16ToBgr(raw, 12, ColorConversion.BayerBg2Bgr),
        GvPixelFormat.BayerGR12 => Bayer16ToBgr(raw, 12, ColorConversion.BayerGr2Bgr),
        GvPixelFormat.BayerGB12 => Bayer16ToBgr(raw, 12, ColorConversion.BayerGb2Bgr),

        GvPixelFormat.BayerRG16 => Bayer16ToBgr(raw, 16, ColorConversion.BayerRg2Bgr),
        GvPixelFormat.BayerBG16 => Bayer16ToBgr(raw, 16, ColorConversion.BayerBg2Bgr),
        GvPixelFormat.BayerGR16 => Bayer16ToBgr(raw, 16, ColorConversion.BayerGr2Bgr),
        GvPixelFormat.BayerGB16 => Bayer16ToBgr(raw, 16, ColorConversion.BayerGb2Bgr),

        _ => throw new NotSupportedException(
            $"OpenCV Mat conversion is not implemented for {pixelFormat}. Packed 10/12-bit formats require an unpack step before Mat conversion.")
    };
}

private static Mat Gray8ToBgr(Mat raw)
{
    Mat color = new Mat();
    CvInvoke.CvtColor(raw, color, ColorConversion.Gray2Bgr);
    return color;
}

private static Mat Gray16ToBgr(Mat raw, int bitDepth)
{
    using Mat normalized8 = Normalize16To8(raw, bitDepth);
    return Gray8ToBgr(normalized8);
}

private static Mat Bayer8ToBgr(Mat raw, ColorConversion conversion)
{
    Mat color = new Mat();
    CvInvoke.CvtColor(raw, color, conversion);
    return color;
}

private static Mat Bayer16ToBgr(Mat raw, int bitDepth, ColorConversion conversion)
{
    using Mat normalized8 = Normalize16To8(raw, bitDepth);
    return Bayer8ToBgr(normalized8, conversion);
}

private static Mat Normalize16To8(Mat raw16, int bitDepth)
{
    double scale = 255.0 / ((1 << bitDepth) - 1);
    Mat normalized8 = new Mat();
    raw16.ConvertTo(normalized8, DepthType.Cv8U, scale);
    return normalized8;
}
```

### Save Every Nth Frame To Disk

```csharp
int localBufferIndex = 0;
long frameOutCounter = 0;
long saveEvery = 30;

await camera.StartStreamAsync();

try
{
    while (streamReceiver.IsReceiving)
    {
        await streamReceiver.waitHandleFrame.WaitAsync();

        while (frameOutCounter < streamReceiver.frameInCounter)
        {
            Mat raw = streamReceiver.image[localBufferIndex];

            if (frameOutCounter % saveEvery == 0)
            {
                using Mat display = ConvertRawMatToBgr(raw, camera.PixelFormat);
                display.Save($"frame_{frameOutCounter:D6}.png");
            }

            localBufferIndex = (localBufferIndex + 1) % streamReceiver.TotalBuffers;
            frameOutCounter++;
        }
    }
}
finally
{
    await camera.StopStream();
}
```

### Display In A UI Without Extra Byte Copies

The Avalonia sample demonstrates the intended pattern:

1. Receive raw Mats into `streamReceiver.image`
2. Demosaic or normalize into a display Mat
3. Copy the final BGR bytes into the UI bitmap buffer

That is usually better than converting each frame back into managed `byte[]` unless you specifically need a managed buffer for other reasons.

### When To Choose The Byte Array Path Instead Of OpenCV

Prefer the `byte[]` path from section 6 when:

1. You need a framework-agnostic helper that returns `BitmapSource`
2. You are handling packed 10-bit or 12-bit formats
3. You do not want to take a dependency on Emgu CV in your application
4. You only need a single frame occasionally and do not need CV processing

## 10. Troubleshooting

### `StartStreamAsync` Returns `false`

Check these first:

1. The camera IP and host NIC are on the same subnet
2. `camera.RxIP` is set correctly on multi-NIC machines
3. Firewall is not blocking GVSP traffic
4. Another process does not already control the camera

### The Frame Size Looks Wrong

Call:

```csharp
await camera.SyncParameters();
```

before converting the bytes, and make sure you are using `camera.Width`, `camera.Height`, and `camera.PixelFormat` from the active camera state.

### The Image Is Garbled

Most often this means one of these:

1. The pixel format dispatch is wrong
2. The format is Bayer and you displayed it as Mono8
3. The format is packed 10-bit or 12-bit and you treated it as 8-bit or 16-bit data
4. The stride passed to the bitmap is wrong

### The First Frame Looks Fine But Later Frames Corrupt

Copy the frame bytes in `FrameReady`. Do not store the original callback array reference for later use.

## 11. Recommended Safe Workflow

If you want the least surprising end-to-end path in this repository:

1. Discover with `GetAllGigeDevicesInNetworkAsnyc`
2. Set `camera.IP` and `camera.RxIP`
3. Call `SyncParameters`
4. Grab one frame with `GrabSingleFrameAsync`
5. Convert using `GenICamFrameConverter.ToBitmapSource`
6. If needed, switch to continuous streaming after the single-frame path is working

That isolates connection issues from conversion issues and makes debugging much simpler.