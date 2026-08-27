# Shift-root benchmark evidence — 2026-08-20

**Status:** Measured. This document is evidence, not a performance claim.

## Protocol

The Release benchmark executed a deterministic large perfect square built from a 2047-bit root. The custom operation was `FermatFactorizer.IntegerSquareRootFloorByShift`, the binary restoring shift root with the explicit one-bit correction certificate. The reference operation was an independent `BigInteger` Newton floor-root implementation embedded in the benchmark executable. Before timing, the harness verifies exact equality of the two results.

The run used `.NET 8.0.30`, Ubuntu 24.04.4 LTS, x64, one warm-up operation and five timed iterations for the root operation. The raw structured result is [`shift-root-benchmark-2026-08-20.json`](shift-root-benchmark-2026-08-20.json).

## Result

| Operation | Iterations | Shift-root time | Newton reference time | Shift-root allocation | Newton allocation | Newton / shift-root |
|---|---:|---:|---:|---:|---:|---:|
| Exact floor root with one-bit correction | 5 | 16.076 ms | 0.965 ms | 11,359,560 B | 51,840 B | 0.060× |

The tested shift-root implementation satisfies the exact result contract but is **slower** than the Newton reference on this host and operand under this initial implementation. Accordingly, no speed claim is permitted. The valid present findings are limited to the following:

> The N-only shift-root path is integer-only, returns the exact floor root and retains an auditable one-bit correction certificate. It is not yet a demonstrated performance improvement over `BigInteger` Newton root extraction.

## Next optimization requirement

The allocation profile shows that the current `BigInteger` restoring loop is not the intended allocation-free `ULong2048` implementation. Any performance increment must introduce a fixed-width limb/shift root path for `ULong2048`, retain the exact floor certificate and rerun this protocol. The bit-mask prefilter remains independently valuable because it reduces the number of calls to **any** exact root implementation; its effect is captured by `FermatSearchResult` counters and must be measured separately from root latency.

## Reproducibility

```bash
dotnet run --project Ricis.Numerics/Ricis.Numerics.Benchmarks/Ricis.Numerics.Benchmarks.csproj \
  -c Release --no-restore -- \
  --quick \
  --output Ricis.Numerics/PerformanceEvidence/shift-root-benchmark-2026-08-20.json
```
