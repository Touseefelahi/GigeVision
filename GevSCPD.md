The value is not arbitrary — it's derived from the physical properties of the camera's stream and where Windows drops packets.

The "cold burst" problem at 1 Hz

At 33 Hz there is ~30 ms between frames. The Windows NIC driver, NDIS interrupt DPC, and socket receive path are all continuously active ("warm"). Packets arrive at a pace the kernel is already scheduled to handle.

When you switch to 1 Hz there is a ~1-second gap of silence. During that gap:

The NIC's interrupt coalescing timer resets to its maximum holdoff (typically 100–200 µs on Intel NICs)
The kernel NDIS receive DPC de-schedules
The socket application thread blocks on Receive()
Then the camera sends all ~540 packets (≈780 KB) in a burst. With GevSCPD = 0 the camera transmits at full line rate: each 1472-byte packet takes ~11.8 µs at 1 Gbps, so the entire frame arrives in ~6.4 ms. The NIC interrupt coalescing fires once, batching potentially hundreds of packets into a single DPC. The NIC on-chip ring is small (often 256–512 descriptors), so it overflows before the kernel can drain it — those drops are invisible to the socket receive buffer.

Why 100 000 works

On a Lucid camera the timestamp counter runs at 1 GHz (1 tick = 1 ns), so:

$$GevSCPD=100000⇒100μs per packet$$

$$540 packets × 100μs = 54 ms total spread$$

At 100 µs spacing the kernel processes each packet (or small batch) before the next arrives. The NIC ring never overflows. At 1 Hz this 54 ms is only 5.4% of the 1000 ms frame interval, so it costs nothing in practice.

Why 10 000 was marginal

$$10000⇒10μs/packet,540×10μs=5.4ms$$

That is barely wider than the raw line-rate burst (~6.4 ms), barely enough to beat the NIC coalescing holdoff. Under light system load it works; under any CPU contention it reverts to drops — which is why it was unstable.

Rule of thumb for the value

$$GevSCPD≥\frac{NIC coalescing holdoff}{packets per frame}×clock frequency$$

For a 200 µs NIC holdoff and 540 packets: $200μs/540≈0.37μs$ minimum spacing. But with Windows scheduling jitter you need headroom; empirically 100 µs/packet (100 000 ticks) is a safe working point. If you want the smallest delay that reliably works, you can bisect between 10 000 and 100 000 — the threshold is likely around 30 000–50 000 for a typical Intel NIC at 1 Gbps.

**The derivable formula**

There are two constraints on GevSCPD:

1. **Floor** — inter-packet spacing must be large enough for Windows to service the NIC even when cold (coming out of a long idle gap). Empirically this is ~100 µs. For a 1 GHz camera clock: `d_min = 100_000 ticks`.

2. **Ceiling** — you cannot delay packets longer than the inter-frame period divided by packet count, or the next frame's burst starts before the current one finishes:

$$d_{\max} = \left\lfloor \frac{F_c}{f \cdot N} \right\rfloor$$

where $F_c$ = camera clock frequency (ticks/s), $f$ = frame rate (Hz), $N$ = `GvspInfo.FinalPacketID` (packets per frame).

Taking the minimum of both:

$$\boxed{\text{GevSCPD}(f, N, F_c) = \min\!\left(100{,}000,\ \left\lfloor \frac{F_c}{f \cdot N} \right\rfloor\right)}$$

**Validation against your observations**

| f (Hz) | N | $F_c/(f \cdot N)$ | GevSCPD | Total burst |
|---|---|---|---|---|
| 1 | 540 | 1,851,852 | **100,000** | 54 ms |
| 10 | 540 | 185,185 | **100,000** | 54 ms |
| 18.5 | 540 | ~100,000 | **100,000** | ~54 ms (crossover) |
| 33 | 540 | 56,166 | **56,166** | 30 ms (= full frame period → continuous) |
| 100 | 540 | 18,519 | **18,519** | 10 ms |

The formula recovers exactly 100,000 at your 1 Hz case. At ≥33 Hz the NIC is warm and the reduced delay is safe; the ceiling also ensures you never delay the stream into the next frame.

**Two unknowns needed at runtime**

- $N$ = `camera.StreamReceiver.GvspInfo.FinalPacketID` — available after `DetectGvspType()`, so it can only be used for the *next* `StartStreamAsync` call, not the first. For the first call you can estimate it from `Width`, `Height`, `BytesPerPixel`, and `Payload`.
- $F_c$ — read from `GevTimestampTickFrequency` via GVCP, or assume `1_000_000_000` for Lucid (1 GHz). 

If you want, I can implement a `CalculateSuggestedStreamPacketDelay(double frameRateHz)` method on `Camera` using the estimated $N$ from current camera geometry.