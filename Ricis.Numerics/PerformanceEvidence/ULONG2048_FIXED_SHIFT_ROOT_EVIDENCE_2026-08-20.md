# ULong2048 fixed-width shift-root evidence — 2026-08-20

**Status:** Measured correctness and allocation success; wall-clock optimization not achieved.

## Protocol

The benchmark used a deterministic 2048-bit perfect square whose root fits in 1024 bits. `ULong2048.IntegerSquareRootFloor` uses the production fixed-width inline-limb restoring algorithm. Its output is verified for equality against an independent `BigInteger` Newton floor-root result before timing. The Run was Release/.NET 8.0.30/Ubuntu 24.04.4/x64 with one warm-up and 100 timed root calls.

The raw output is [`ulong-fixed-root-benchmark-2026-08-20.json`](ulong-fixed-root-benchmark-2026-08-20.json).

## Result

| Operation | Iterations | Fixed-width time | Newton reference time | Fixed-width allocation | Newton allocation | Newton / fixed-width |
|---|---:|---:|---:|---:|---:|---:|
| `ULong2048.IntegerSquareRootFloor` | 100 | 161.473 ms | 7.239 ms | 40 B | 526,440 B | 0.045× |

The test suite independently verifies the exact certificate `r²≤N<(r+1)²`, the `2^2048−1` overflow boundary, deterministic oracle corpus and zero managed allocation hot path. Therefore **correctness and allocation requirements pass**.

> The current inline-limb restoring root is not a measured wall-clock improvement over the independent `BigInteger` Newton reference on this host. No speed claim is permitted.

The 40-byte total allocation is benchmark harness noise over 100 calls; the direct 128-call allocation regression test reports zero managed bytes on the calling thread.

## Interpretation

The implementation has established the required no-fallback, exact fixed-width primitive and can serve as the correct numeric baseline for a later ULong2048 Fermat search API. It must not replace the faster `BigInteger` baseline merely on a speed premise. Any further performance sprint should first profile the 1024 restoring iterations and reduce limb-pass count or introduce a validated alternate fixed-width algorithm, retaining the same certificate and direct tests.

## Reproduction

```bash
dotnet run --project Ricis.Numerics/Ricis.Numerics.Benchmarks/Ricis.Numerics.Benchmarks.csproj \
  -c Release --no-restore -- \
  --quick \
  --output Ricis.Numerics/PerformanceEvidence/ulong-fixed-root-benchmark-2026-08-20.json
```
