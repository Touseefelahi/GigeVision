# GenICam Chunk Data Mode — Implementation Notes

## What It Is

When `ChunkModeActive = true` is set on the camera, each GVSP frame is extended with
a **chunk data region** appended after the last pixel byte. This region contains named
records (timestamp, exposure time, gain, white balance, ROI, etc.) defined per-camera
in the GenICam XML under the `ChunkDataControl` category.

The GVSP trailer still arrives as a normal data-end packet. The chunk region sits inside
the data packets of the frame — it is additional payload that comes after the image bytes.

---

## Wire Format (GigE Vision 2.0, Section 4.8)

Each frame's chunk area is a sequence of records followed by a terminator:

```
[ChunkData_0]
  4 bytes  ChunkID      (big-endian uint32, e.g. 0x00000001 = Timestamp)
  4 bytes  ChunkLength  (big-endian uint32, number of data bytes following)
  N bytes  ChunkData

[ChunkData_1]
  ...

[Terminator]
  4 bytes  0xD0000001   (end-of-chunks marker)
  4 bytes  0x00000000
```

The chunk area begins at byte offset `RawImageSize` within the assembled frame buffer.
The terminator is always the last 8 bytes of the extended buffer.

Standard ChunkIDs are defined in the SFNC; vendor-specific ones are listed in the
camera's GenICam XML under `ChunkID` enum entries.

Lucid TRI032S standard chunks (from XML `ChunkDataControl`):
- `ChunkTimestamp`     — `ChunkID = 0x00000001` — 8 bytes, uint64, camera ticks
- `ChunkExposureTime`  — `ChunkID = 0x00010006` — 8 bytes, double, microseconds
- `ChunkGain`          — `ChunkID = 0x00010007` — 8 bytes, double, dB
- `ChunkWidth` / `ChunkHeight` / `ChunkOffsetX` / `ChunkOffsetY` — 4 bytes each, uint32
- `ChunkPixelFormat`   — `ChunkID = 0x00010002` — 4 bytes, uint32 (PFNC value)

> Actual ChunkIDs and lengths must be verified from the live camera XML at runtime;
> they can differ between firmware versions.

---

## Camera Configuration (GenICam)

Call these before `StartStreamAsync()`:

```csharp
await camera.SetCameraParameter("ChunkModeActive", true);

// Enable timestamp chunk
await camera.SetCameraParameter("ChunkSelector", "Timestamp");
await camera.SetCameraParameter("ChunkEnable", true);

// Enable exposure chunk (optional)
await camera.SetCameraParameter("ChunkSelector", "ExposureTime");
await camera.SetCameraParameter("ChunkEnable", true);
```

To disable:
```csharp
await camera.SetCameraParameter("ChunkModeActive", false);
```

---

## Required Library Changes

### 1 — Detect chunk mode and extended frame size

After syncing parameters, read `ChunkModeActive` from the camera and, if true, read
`PayloadSize` (GevSCSP / the actual GVSP payload size register) to determine the full
per-frame byte count including chunk data.

`Camera.SyncParameters()` should set a new property `ChunkAreaSize`:

```csharp
if ((bool)await GetParameterValue("ChunkModeActive"))
{
    ulong fullPayload = (ulong)await GetParameterValue("PayloadSize");
    ChunkAreaSize = (int)(fullPayload - (ulong)PixelFormatHelper.GetFrameSize(...));
}
```

### 2 — Grow the receive buffer

`SetRxBuffer()` must allocate `RawImageSize + ChunkAreaSize` when chunk mode is active.
`GvspInfo.RawImageSize` should remain the image-only size so existing pixel consumers
are unaffected.

```csharp
int totalFrameSize = GvspInfo.RawImageSize + ChunkAreaSize;
rawBytes = new byte[totalFrameSize];
```

The `StreamReceiverBase` buffers in `Receiver()` also need the larger size:
```csharp
buffer[0] = new byte[GvspInfo.RawImageSize + chunkAreaSize];
buffer[1] = new byte[GvspInfo.RawImageSize + chunkAreaSize];
```

Either pass `chunkAreaSize` into the receiver or expose it through `GvspInfo`.

### 3 — Chunk parser

Add `GigeVision.Core.Services.ChunkParser`:

```csharp
public static class ChunkParser
{
    private const uint ChunkTerminator = 0xD0000001;

    /// <summary>
    /// Parses the chunk region that starts at <paramref name="imageSize"/> bytes
    /// into <paramref name="frameBuffer"/>.
    /// </summary>
    public static IReadOnlyList<ChunkRecord> Parse(byte[] frameBuffer, int imageSize)
    {
        var result = new List<ChunkRecord>();
        int offset = imageSize;

        while (offset + 8 <= frameBuffer.Length)
        {
            uint chunkId = ReadUInt32BE(frameBuffer, offset);
            if (chunkId == ChunkTerminator) break;

            uint chunkLength = ReadUInt32BE(frameBuffer, offset + 4);
            offset += 8;

            if (offset + (int)chunkLength > frameBuffer.Length) break;

            result.Add(new ChunkRecord(chunkId, frameBuffer, offset, (int)chunkLength));
            offset += (int)chunkLength;
        }

        return result;
    }

    private static uint ReadUInt32BE(byte[] buf, int pos) =>
        ((uint)buf[pos] << 24) | ((uint)buf[pos+1] << 16) |
        ((uint)buf[pos+2] <<  8) |  (uint)buf[pos+3];
}

public readonly struct ChunkRecord
{
    public uint ChunkId { get; }
    private readonly byte[] _buffer;
    private readonly int _offset;
    public int Length { get; }

    public ChunkRecord(uint chunkId, byte[] buffer, int offset, int length)
    {
        ChunkId = chunkId; _buffer = buffer; _offset = offset; Length = length;
    }

    public ulong ReadUInt64() => ...  // big-endian helpers
    public double ReadDouble() => BitConverter.ToDouble(...) // verify endianness
    public uint ReadUInt32() => ...
}
```

### 4 — Surface chunk data to callers

Two options (pick one):

**Option A** — extend `GvspFrameInfo`:
```csharp
public readonly struct GvspFrameInfo
{
    public ulong FrameId { get; }
    public ulong Timestamp { get; }
    public IReadOnlyList<ChunkRecord> Chunks { get; } // null when chunk mode off
}
```

**Option B** — fire a separate event `ChunkDataReady` with the parsed chunk list.

Option A is simpler. When chunk mode is off, `Chunks` is null and existing code is unchanged.

---

## Endianness Note

GVSP chunk data bytes are big-endian (network order). The `double` type for
`ExposureTime` and `Gain` is IEEE 754 big-endian. On Windows (little-endian),
`BitConverter.ToDouble` needs the bytes reversed before use:

```csharp
var bytes = chunkRecord.GetBytes();
if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
double value = BitConverter.ToDouble(bytes, 0);
```

---

## Testing Checklist

- [ ] `ChunkModeActive` is readable and writable via GenICam on Lucid TRI032S
- [ ] `PayloadSize` reflects the enlarged per-frame size when chunk mode is on
- [ ] Frame buffer large enough — no `IndexOutOfRangeException` at high frame rates
- [ ] `FinalPacketID` is still computed from image pixels only, not total payload
- [ ] Chunk terminator `0xD0000001` is present at expected offset
- [ ] Timestamps from chunk data match timestamps from GVSP leader (same clock)
- [ ] Disabling chunk mode restores original buffer sizing without restart
