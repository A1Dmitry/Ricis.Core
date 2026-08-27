# Ricis.Numerics comparative performance evidence

- Timestamp UTC: `2026-08-20T09:10:35.1903378+00:00`
- Runtime: `.NET 8.0.30`
- OS: `Ubuntu 24.04.4 LTS`
- Architecture: `X64`
- Protocol: Fixed deterministic 2048-bit operands; Release build; one warmup operation; result equality is checked against BigInteger before timing.

| Operation | Iterations | Custom ms | BigInteger ms | Custom alloc. | BigInteger alloc. | BigInteger / custom |
|---|---:|---:|---:|---:|---:|---:|
| Int2048 addition | 25000 | 11.322 | 8.866 | 7000040 B | 6000040 B | 0.783× |
| Int2048 subtraction | 25000 | 41.965 | 7.009 | 28000040 B | 6000040 B | 0.167× |
| Int2048 multiplication (low 2048 bits) | 2000 | 43.148 | 0.044 | 560040 B | 40 B | 0.001× |
| Int2048 division | 40 | 20.690 | 0.066 | 44840 B | 6440 B | 0.003× |
| ULong2048 modular multiplication | 10 | 39.660 | 0.053 | 14350040 B | 7240 B | 0.001× |
| RSA public operation e=65537 | 3 | 201.280 | 0.818 | 116163640 B | 880 B | 0.004× |

> This is reproducible comparative evidence, not a CI pass/fail speed threshold. CPU frequency, JIT, allocator and host contention make wall-clock thresholds unsuitable for a correctness gate.

NUMERICS_BENCHMARK_EVIDENCE=/home/ubuntu/Ricis.Core/Ricis.Numerics/PerformanceEvidence/ULONG2048_PREINLINE_BASELINE_2026-08-20.json
