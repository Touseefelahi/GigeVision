# Implemented Changes

Date: 2026-04-21

This file summarizes the fixes implemented from the `GigeVision.Core` audit.

## GVCP Control Path

- Serialized shared `ControlSocket` traffic with a `SemaphoreSlim` to prevent concurrent send/receive races.
- Replaced mutable `ushort` request ID usage with atomic `Interlocked.Increment` generation.
- Correlated replies using `AcknowledgementID` so commands only accept their own response.
- Reconnected the shared control socket after timeouts to avoid stale pending receives poisoning later commands.
- Made heartbeat traffic use the same awaited GVCP command path instead of fire-and-forget socket usage.
- Fixed `GetControlAsync()` so it reads control state through the same socket/target being written to.
- Disposed temporary GVCP sockets consistently and propagated the configured timeout into helper-created sockets.
- Fixed XML read-header generation so the requested register address is actually encoded in the packet.
- Added minimum-length validation for discovery packets before parsing fixed offsets.

## Stream Reception

- Fixed the default receiver buffer handoff so completed frames publish the finished buffer, not the live write buffer.
- Removed the asynchronous callback race in the default receiver by invoking frame delivery on the completed buffer directly.
- Changed startup frame handling to skip the first partial frame boundary after GVSP type detection.
- Made `StopReception()` null-safe when the receive socket was never created.
- Captured the real bound receive port from the socket after binding, enabling safe port-0 receiver startup.
- Guarded firewall keepalive traffic so it only sends when camera IP and source port are valid.
- Fixed `StreamReceiverParallel` image allocation to use computed raw image size.
- Fixed `StreamReceiverParallel` final-packet handling for exact-multiple payload sizes.

## Pixel Format And Payload Sizing

- Added `PixelFormatHelper` to derive byte and frame sizes from PFNC effective bits instead of the old bit-20 heuristic.
- Updated GVSP leader parsing to compute bytes-per-pixel and raw image size from PFNC metadata.
- Updated camera payload calculation to use per-line PFNC size.
- Updated receive-buffer allocation to use exact PFNC-based frame size.

## Camera Control And Stream Startup

- Wrapped `SetOffsetAsync()` control ownership in `try/finally` so control is always released on failure.
- Wrapped `SetResolutionAsync()` control ownership in `try/finally` so control is always released on failure.
- Reworked `StartStreamAsync()` rollback behavior so receiver startup and control ownership are cleaned up on failed setup.
- Removed the temporary free-port probe race by allowing the receiver socket to bind and then reading back the assigned port.
- Ensured receiver setup occurs before multicast fallback startup.
- Propagated the discovered `SCSPPort` into the receiver before reception.

## GenICam Port Bridge

- Fixed `GenPort` to always encode GVCP register addresses as 32-bit addresses.
- Restricted direct register reads to aligned 4-byte accesses and used memory reads otherwise.
- Replaced the old `BitConverter.ToUInt32(pBuffer)` write path with chunked read-modify-write handling for short or unaligned writes.
- Preserved surrounding bytes correctly when writing sub-register values.

## GenICam Type Handling

- Made `Converter` evaluation stateless so repeated reads and writes do not corrupt stored formula templates.
- Made `IntSwissKnife` evaluation stateless so repeated formula reads remain deterministic across calls.
- Added `IDoubleValue` and implemented it on converter-backed values, enabling engineering-unit reads and writes without forcing everything through `long`.
- Completed the usable `GenFloat` path so float categories can read current values, min/max, units, and writes through converter-backed or integer-backed `PValue` implementations.
- Reworked XML `pValue` resolution to dispatch by node type instead of only naming suffixes, making converter-backed and SwissKnife-backed features more robust across different vendor XMLs.

## Camera Parameter APIs

- Added typed camera helpers for float and boolean parameters alongside the existing integer helpers.
- Added float-aware min/max accessors so engineering-unit features can be queried without truncation.
- Added `SetCameraParameter` overloads for `double` and `bool`, so callers can use a single write method name across integer, converter-backed, and boolean features.
- Added `GetParameterValue<T>` so callers can read `long`, `double`, and `bool` features through one typed entry point instead of separate getter names.
- Updated the `long` overload of `SetCameraParameter` to auto-dispatch to boolean or floating-point handling when the underlying feature is not an integer register.
- Kept the existing `long`-based parameter APIs intact for backward compatibility.

## Protocol Constants And Reply Parsing

- Corrected `GevSCSP` from `0x01C` to `0x0D1C`.
- Updated `GvcpReply` to capture `AcknowledgementID` before success short-circuiting.
- Tightened `ReadRegAck` parsing to distinguish valid single-register, multi-register, and malformed replies.

## Validation

- `dotnet build .\GigeVision.Core\GigeVision.Core.csproj -c Debug` succeeded.
- `GigeVision.CoreTests` was intentionally skipped per request.