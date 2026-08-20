# Ricis.Numerics comparative performance evidence

- Timestamp UTC: `2026-08-20T09:13:50.4089486+00:00`
- Runtime: `.NET 8.0.30`
- OS: `Ubuntu 24.04.4 LTS`
- Architecture: `X64`
- Protocol: Fixed deterministic 2048-bit operands; Release build; one warmup operation; result equality is checked against BigInteger before timing.

| Operation | Iterations | Custom ms | BigInteger ms | Custom alloc. | BigInteger alloc. | BigInteger / custom |
|---|---:|---:|---:|---:|---:|---:|
| Int2048 addition | 25000 | 11.995 | 10.673 | 7000040 B | 6000040 B | 0.890× |
| Int2048 subtraction | 25000 | 47.726 | 7.162 | 28000040 B | 6000040 B | 0.150× |
| Int2048 multiplication (low 2048 bits) | 2000 | 42.394 | 0.047 | 560040 B | 40 B | 0.001× |
| Int2048 division | 40 | 19.149 | 0.064 | 44840 B | 6440 B | 0.003× |
| ULong2048 modular multiplication | 10 | 158.477 | 0.061 | 40 B | 7240 B | 0.000× |
| RSA public operation e=65537 | 3 | 42.222 | 0.688 | 40 B | 880 B | 0.016× |

> This is reproducible comparative evidence, not a CI pass/fail speed threshold. CPU frequency, JIT, allocator and host contention make wall-clock thresholds unsuitable for a correctness gate.

NUMERICS_BENCHMARK_EVIDENCE=/home/ubuntu/Ricis.Core/Ricis.Numerics/PerformanceEvidence/ULONG2048_DIRECT_LIMBS_2026-08-20.json
