# Ricis.Numerics comparative performance evidence

- Timestamp UTC: `2026-08-20T09:18:25.8421072+00:00`
- Runtime: `.NET 8.0.30`
- OS: `Ubuntu 24.04.4 LTS`
- Architecture: `X64`
- Protocol: Fixed deterministic 2048-bit operands; Release build; one warmup operation; result equality is checked against BigInteger before timing.

| Operation | Iterations | Custom ms | BigInteger ms | Custom alloc. | BigInteger alloc. | BigInteger / custom |
|---|---:|---:|---:|---:|---:|---:|
| Int2048 addition | 25000 | 9.867 | 7.382 | 7000040 B | 6000040 B | 0.748× |
| Int2048 subtraction | 25000 | 40.569 | 5.987 | 28000040 B | 6000040 B | 0.148× |
| Int2048 multiplication (low 2048 bits) | 2000 | 41.565 | 0.051 | 560040 B | 40 B | 0.001× |
| Int2048 division | 40 | 22.791 | 0.102 | 44840 B | 6440 B | 0.004× |
| ULong2048 modular multiplication | 10 | 72.897 | 0.059 | 40 B | 7240 B | 0.001× |
| RSA public operation e=65537 | 3 | 13.902 | 0.604 | 40 B | 880 B | 0.043× |

> This is reproducible comparative evidence, not a CI pass/fail speed threshold. CPU frequency, JIT, allocator and host contention make wall-clock thresholds unsuitable for a correctness gate.

NUMERICS_BENCHMARK_EVIDENCE=/home/ubuntu/Ricis.Core/Ricis.Numerics/PerformanceEvidence/ULONG2048_MONTGOMERY_2026-08-20.json
