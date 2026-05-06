# GigeVision.Core Audit

Date: 2026-04-21

## Scope

This review focused on the `GigeVision.Core` library, especially the GVCP control path, stream-receiver implementations, camera startup/shutdown flow, register-map definitions, and the `GenPort` bridge used by the GenICam layer.

I also did two external cross-checks where the code looked spec-sensitive:

- PFNC / pixel-format handling was checked against the structure of the existing `PixelFormat` enum values.
- The `GevSCSP` register address was cross-checked against the public Aravis GigE Vision implementation, which maps the source stream port register to `0x0D1C`.

Build status:

- `GigeVision.Core` builds successfully.
- The wider solution still emits a very large warning set (mostly nullability/style debt in `GenICam` plus a smaller set in `GigeVision.Core`), so compile success should not be treated as proof of runtime correctness.

## Highest-Risk Areas

- `GigeVision.Core/Services/Gvcp.cs`: shared control socket, request IDs, XML download, discovery and broadcast helpers.
- `GigeVision.Core/Services/StreamReceiverBase.cs`: this is the effective default streaming path because `StreamReceiverBufferswap` is only an empty subclass.
- `GigeVision.Core/Services/Camera.cs`: stream setup, control ownership, payload selection, and event wiring.
- `GigeVision.Core/GenPort.cs`: the GenICam port bridge, where address width and byte-order rules must be exact.

## Findings

### 1. Critical - GVCP control traffic is not serialized, request IDs are shared, and replies are not correlated back to the command that sent them

Affected code:

- `GigeVision.Core/Services/Gvcp.cs:28`
- `GigeVision.Core/Services/Gvcp.cs:539`
- `GigeVision.Core/Services/Gvcp.cs:656`
- `GigeVision.Core/Services/Gvcp.cs:690-696`
- `GigeVision.Core/Services/Gvcp.cs:1071-1076`
- `GigeVision.Core/Models/GvcpReply.cs:180`

Problem:

- `gvcpRequestID` is mutable shared state and is incremented from many public methods without any synchronization.
- `RunHeartbeatThread()` sends GVCP traffic on the same shared `ControlSocket` used by normal read/write/control operations.
- `SendGvcpCommand()` accepts the first received datagram and never checks that `AcknowledgementID` matches `command.RequestId`.
- If `Task.WhenAny(...)` times out, the pending `ReceiveAsync()` is left alive; later packets can complete the stale receive instead of the command that is actually waiting for them.

Why this matters:

- Two overlapping control operations can consume each other's replies.
- Heartbeat traffic can race with normal register reads/writes.
- Under timeouts or retries, replies can be silently mis-associated instead of being rejected.

Suggested improvement:

- Make the control channel single-flight with a `SemaphoreSlim` or equivalent lock around every `ControlSocket` send/receive pair.
- Generate request IDs atomically (`Interlocked.Increment` on an `int` backing field, then cast to `ushort`).
- Reject replies whose `AcknowledgementID` does not match the sent command.
- Do not fire-and-forget GVCP commands on the control socket.
- Consider using a dedicated socket for heartbeat if control traffic must remain concurrent.

### 2. Critical - `GetControlAsync()` checks `CameraIp`, not the IP/socket passed by the caller

Affected code:

- `GigeVision.Core/Services/Gvcp.cs:579-641`
- `GigeVision.Core/Services/Gvcp.cs:824-833`

Problem:

- The `WriteRegisterAsync(string Ip, ...)` overloads create a socket that targets the caller-provided `Ip`.
- `GetControlAsync(UdpClient socket)` then reads the control register with `ReadRegisterAsync(Converter.RegisterStringToByteArray(...))`, which resolves through `CameraIp`, not through the target socket or the `Ip` parameter.

Why this matters:

- If `CameraIp` is empty or points to a different device, the control check runs against the wrong camera.
- The string-IP overloads can fail even when the target camera is valid, or worse, check one device and then send the write to another.

Suggested improvement:

- Change `GetControlAsync()` to read via the same target that will be written to.
- Add a `ReadRegisterAsync(UdpClient socket, ...)` or `ReadRegisterAsync(string ip, ...)` path and use it consistently.

### 3. Critical - The default receiver path can publish the wrong frame buffer while the next frame is already being written

Affected code:

- `GigeVision.Core/Services/StreamReceiverBufferswap.cs:5`
- `GigeVision.Core/Services/StreamReceiverBase.cs:210-227`

Problem:

- `StreamReceiverBufferswap` inherits `StreamReceiverBase` unchanged, so the base implementation is the default stream path.
- On trailer reception, the code stores `bufferIndexClone = bufferIndex`, then swaps `bufferIndex`, but the callback uses `buffer[bufferIndex]` instead of `buffer[bufferIndexClone]`.
- The callback is also inside `Task.Run(...)`, so it captures the mutable `bufferIndex` field and can observe a later value.

Why this matters:

- The consumer can receive the next buffer instead of the completed one.
- That next buffer is already being filled by the receiver thread, so the frame exposed to the application can be corrupted and nondeterministic.

Suggested improvement:

- Publish the cloned buffer index, not the mutable live one.
- Avoid `Task.Run(...)` here unless there is an actual queue or immutable frame handoff.
- If asynchronous delivery is required, queue immutable frame descriptors or copy the completed buffer into a consumer-owned pool.

### 4. Critical - Pixel-format and bytes-per-pixel logic is wrong for packed and color formats

Affected code:

- `GigeVision.Core/Services/StreamReceiverBase.cs:138,163`
- `GigeVision.Core/Services/Camera.cs:715,743-747`
- `GigeVision.Core/Enums/PixelFormat.cs:41,91,196,216`

Problem:

- `DetectGvspType()` computes bytes per pixel with `Math.Ceiling((double)(singlePacket[21] / 8))`. Because the division happens before the cast, `10/8`, `12/8`, `14/8` all collapse to `1` before `Ceiling(...)` is applied.
- `PixelFormatToBytesPerPixel()` reduces all formats to `1` or `2` bytes solely by checking bit 20.
- The existing `PixelFormat` enum already shows formats that require very different occupied sizes:
  - `Mono10Packed = 0x010C0004` (`0x0C` occupied bits -> 12 bits per pixel)
  - `Mono10 = 0x01100003` (`0x10` occupied bits -> 16 bits per pixel)
  - `RGB8Packed = 0x02180014` (`0x18` occupied bits -> 24 bits per pixel)
  - `RGBA8Packed = 0x02200016` (`0x20` occupied bits -> 32 bits per pixel)

Why this matters:

- Raw image buffers can be undersized or oversized.
- Payload calculations become wrong.
- Any non-trivial PFNC format is at risk, especially packed monochrome/Bayer formats and color formats.

Suggested improvement:

- Derive occupied bits from the PFNC value instead of a one-bit heuristic.
- For example, `occupiedBits = ((int)pixelFormat >> 16) & 0xFF` is a far better starting point than `IsBitSet(..., 20)`.
- Distinguish packed formats from unpacked formats when allocating buffers and when deciding whether the consumer receives transport-packed bytes or unpacked pixels.

### 5. High - `GenPort` builds malformed addresses for short registers and blindly converts every write buffer to `UInt32`

Affected code:

- `GigeVision.Core/Services/GenPort.cs:25-35`
- `GigeVision.Core/Services/GenPort.cs:48-59`
- `GigeVision.Core/Services/GenPort.cs:75-84`

Problem:

- `GetAddressBytes()` returns a 2-byte address for `length == 2`, even though GVCP register addresses are 32-bit addresses on the wire.
- `ReadAsync()` decides between register-read and memory-read using `length >= 8`, which is too coarse for GenICam port semantics.
- `WriteAsync()` always does `BitConverter.ToUInt32(pBuffer)` regardless of the actual requested write length.

Why this matters:

- A 1-byte or 2-byte write can throw if `pBuffer.Length < 4`.
- A longer write silently truncates to the first 4 bytes.
- Short-register accesses can send an invalid 2-byte address instead of a 4-byte register address.

Suggested improvement:

- Always encode GVCP register addresses as 4 bytes.
- Respect the requested `length` when reading/writing the GenICam port.
- For non-4-byte operations, prefer memory transactions or a proper stacked read/write strategy instead of assuming "anything under 8 bytes is a register read/write".

### 6. High - Camera control ownership is not released reliably on failure or exception

Affected code:

- `GigeVision.Core/Services/Camera.cs:367-389`
- `GigeVision.Core/Services/Camera.cs:399-423`
- `GigeVision.Core/Services/Camera.cs:514-535`
- `GigeVision.Core/Services/Camera.cs:550-566`

Problem:

- `SetOffsetAsync()` acquires control and releases it only on the success path; any exception between those awaits leaves control held.
- `SetResolutionAsync()` catches and returns `false`, but the `catch` block does not release control.
- `StartStreamAsync()` acquires control with keep-alive enabled, but several non-success paths fall out of the nested `if` chain without calling `StopStream()` or `LeaveControl()`.

Why this matters:

- A failed setup can leave the heartbeat thread running and keep the camera locked until the device heartbeat times out.
- That can block the same process on the next call, or block a different client entirely.

Suggested improvement:

- Treat control ownership as a scoped resource.
- Use `try/finally` around every path that calls `TakeControl()`.
- Only start heartbeat after the rest of the stream setup has succeeded, or guarantee rollback on every early return.

### 7. High - `StartStreamAsync()` has a port-selection race and a multicast fallback null-path

Affected code:

- `GigeVision.Core/Services/Camera.cs:490-492`
- `GigeVision.Core/Services/Camera.cs:532-533`
- `GigeVision.Core/Services/Camera.cs:550`

Problem:

- The code opens a temporary `UdpClient(0)` to discover a free port, closes it, then later asks the stream receiver to bind the same port.
- In the multicast fallback branch, `SetupRxThread()` is called without first calling `SetupReceiver()`.

Why this matters:

- The selected port can be claimed by another process between discovery and the real bind.
- The multicast fallback can dereference a null `StreamReceiver`.

Suggested improvement:

- Let the actual receive socket bind to port `0`, then read back the assigned port and program the camera with that real port.
- Ensure `SetupReceiver()` runs before every `SetupRxThread()` path.

### 8. High - Several GVCP helpers leak sockets, and some overloads ignore the configured timeout

Affected code:

- `GigeVision.Core/Services/Gvcp.cs:579-641`
- `GigeVision.Core/Services/Gvcp.cs:854-921`
- `GigeVision.Core/Services/Gvcp.cs:1124-1144`
- `GigeVision.Core/Services/Gvcp.cs:356,383,580,599,619,638,1053`

Problem:

- Multiple `WriteRegisterAsync(string Ip, ...)` overloads allocate `UdpClient` instances and never dispose them.
- `GetRawXmlFileFromCamera()` creates its own `UdpClient` and never disposes it.
- `SendBroadCastPacket()` allocates a `UdpClient` per interface and never disposes it.
- Some methods honor `ReceiveTimeoutInMilliseconds`; others hard-code `1000` ms.

Why this matters:

- Repeated control calls can leak sockets and exhaust ephemeral ports or OS handles.
- Timeout behavior becomes inconsistent across API surfaces, which is especially bad for discovery and XML download.

Suggested improvement:

- Dispose temporary clients with `using`/`await using`.
- Propagate the configured timeout to every code path that creates a socket.

### 9. High - GVSP type detection consumes live packets and can corrupt or partially accept the first frame

Affected code:

- `GigeVision.Core/Services/StreamReceiverBase.cs:128-145`
- `GigeVision.Core/Services/StreamReceiverBase.cs:172`
- `GigeVision.Core/Services/StreamReceiverBase.cs:186-193`

Problem:

- `DetectGvspType()` performs two real `Receive(...)` calls before the main receive loop starts.
- The main receiver initializes `packetRxCount = 1`, apparently trying to compensate for the already-consumed packet.
- The consumed packet is not copied into the frame buffer.

Why this matters:

- The first frame can be emitted with one packet missing but still pass the default missing-packet tolerance.
- At best the first frame is dropped; at worst it is accepted but contains zeroed/garbled data where the consumed packet belonged.

Suggested improvement:

- Capture the packets consumed during type detection and feed them into the normal frame assembly path.
- Alternatively, do not start accepting frames until the next full frame boundary after detection.

### 10. High - `StreamReceiverParallel` can drop the last packet of exact-multiple frames and can overwrite unread packet chunks

Affected code:

- `GigeVision.Core/Services/StreamReceiverParallel.cs:58-75`
- `GigeVision.Core/Services/StreamReceiverParallel.cs:105-129`
- `GigeVision.Core/Services/StreamReceiverParallel.cs:156`

Problem:

- `finalPacketLength` is calculated as `image.Length % GvspInfo.PayloadSize`.
- If the image size is an exact multiple of the payload size, that remainder is `0`, so the final packet copies zero bytes.
- The writer rotates through a fixed array of packet buffers and releases semaphores, but there is no ownership/backpressure mechanism that prevents the writer from wrapping around and reusing a packet buffer before the decoder has finished reading it.

Why this matters:

- Exact-multiple payload cases lose the final packet of every frame.
- Under load, decoded frames can be built from overwritten packet chunks.

Suggested improvement:

- Use `finalPacketLength = remainder == 0 ? GvspInfo.PayloadSize : remainder`.
- Replace the current wraparound scheme with a proper producer/consumer queue or buffer-pool lease model.

### 11. Medium - Camera events are copied into the receiver once instead of being forwarded dynamically

Affected code:

- `GigeVision.Core/Services/Camera.cs:71,278,797-798`
- `GigeVision.Core/Services/StreamReceiverBase.cs:44,94`

Problem:

- `SetupReceiver()` copies the current values of `Camera.FrameReady` and `Camera.Updates` into the receiver.
- Later subscriptions or unsubscriptions on `Camera` are not propagated.

Why this matters:

- If a consumer subscribes after `StartStreamAsync()`, it can miss all stream events.
- This is an API behavior trap because the `Camera` object appears to own the events.

Suggested improvement:

- Forward events instead of copying delegates.
- For example, keep the receiver private and relay its events through `Camera`, or subscribe the receiver once to internal handlers that then raise the camera-level events.

### 12. Medium - The pipeline-based receivers are incomplete and unsafe to expose as production options

Affected code:

- `GigeVision.Core/Services/StreamReceiverPipeLine.cs:33,54,60,78`
- `GigeVision.Core/Services/CameraStreamReceiverPipeline.cs:27,53,78,86,111`

Problem:

- `StreamReceiverPipeLine` has the actual payload copy and `FrameReady` invocation commented out, so it does not complete frames.
- `CameraStreamReceiverPipeline` allocates `FinalPacketID * packetSize` instead of raw image size, copies payloads into slices sized with `packetLength`, and swallows exceptions silently.

Why this matters:

- These types are effectively experimental/incomplete and can mis-size buffers or fail silently.

Suggested improvement:

- Either remove them from the supported surface until they are finished, or make it explicit in code/docs that they are not production-ready.
- Add tests before shipping any pipeline receiver as a selectable default.

### 13. Medium - Discovery parsing trusts fixed offsets without validating packet length

Affected code:

- `GigeVision.Core/Services/Gvcp.cs:764-804`

Problem:

- `DecodeDiscoveryPacket()` indexes fixed offsets for IP, MAC, manufacturer, model, version, serial number and user-defined name without checking `discoveryPacket.Length` first.
- The current offsets require at least 256 bytes of payload to be safe.

Why this matters:

- A truncated or malformed discovery reply can throw and abort discovery.

Suggested improvement:

- Validate minimum packet size up front.
- Prefer explicit parsing with named constants and reject malformed discovery packets gracefully.

### 14. Medium - ForceIP/XML helper code contains hard-coded protocol values that are either wrong or too rigid

Affected code:

- `GigeVision.Core/Services/Gvcp.cs:193`
- `GigeVision.Core/Services/Gvcp.cs:863`
- `GigeVision.Core/Services/Gvcp.cs:931-944`

Problem:

- `ForceIPAsync()` derives the gateway by copying the target IP and forcing the last octet to `1`.
- `GetReadMessageHeader(int register)` computes `tempRegister` but never uses the `register` parameter; the payload is hard-coded to `0x0200`.
- `Task.Delay(100);` in `GetRawXmlFileFromCamera()` is not awaited and has no effect.

Why this matters:

- The ForceIP implementation is wrong on networks where the default gateway is not `x.x.x.1`.
- The read-header helper only works accidentally for the one register it is currently used for.
- The unawaited delay makes the code look timing-sensitive without actually enforcing any timing.

Suggested improvement:

- Accept an explicit gateway parameter (or write `0.0.0.0` when gateway is unknown).
- Use the `register` parameter when building the read-memory header.
- Remove the dead delay or `await` it if the timing is genuinely required.

### 15. Medium - The `GevSCSP` register constant looks like a typo and points at the wrong address

Affected code:

- `GigeVision.Core/Enums/GevRegisters.cs:23`

Problem:

- `GevSCSP` is currently defined as `0x01C`.
- That does not fit the surrounding stream-channel register block (`0x0D00`, `0x0D04`, `0x0D08`, `0x0D18`).
- Cross-checking against the Aravis GigE Vision implementation places the source stream port register at `0x0D1C`.

Why this matters:

- Any code that uses the enum value directly will target the wrong address.
- The current `Camera.StartStreamAsync()` path only avoids this because it looks the feature up by name through the XML helper instead of using the enum value.

Suggested improvement:

- Change the constant to `0x0D1C` after confirming against the target GigE Vision register map / standard used by this library.

## Additional Cleanup Items

- `GigeVision.Core/Services/Gvcp.cs:227`: `GetAllGigeDevicesInNetworkAsnyc(Action<...>)` is `async void`; it should return `Task` so failures can be observed.
- `GigeVision.Core/Services/Gvcp.cs:328`: `throw ex;` destroys the original stack trace; use `throw;`.
- `GigeVision.Core/Services/Gvcp.cs:477,1047-1060,1144`: several `catch (Exception)` blocks silently swallow failures and make network/debug problems much harder to diagnose.
- `GigeVision.Core/Models/GvcpReply.cs:146-153`: `ReadRegAck` parsing should validate buffer length before indexing `buffer[8]..buffer[11]`; truncated replies can still reach that code path.
- `GigeVision.Core/Models/GvcpCommand.cs:113,163,264`: per-call `Random` allocation and generic `Exception` throws are not correctness bugs by themselves, but they add avoidable nondeterminism and weak diagnostics.
- `GigeVision.Core/Services/StreamReceiverBase.cs:115-119`: `StopReception()` calls `socketRxRaw.Dispose()` without a null-conditional; calling it before the socket is created will throw.

## Suggested Fix Order

1. Fix the GVCP control path first: single-flight access, atomic request IDs, and reply/request correlation.
2. Fix the default receiver handoff bug in `StreamReceiverBase` because it affects the shipping stream path immediately.
3. Rework pixel-format and payload sizing so buffers are correct for packed and color formats.
4. Make control acquisition/release exception-safe in `Camera`.
5. Repair `GenPort` so address width, read/write size and byte order follow the transport rules exactly.
6. Only then revisit the experimental pipeline/parallel receivers.