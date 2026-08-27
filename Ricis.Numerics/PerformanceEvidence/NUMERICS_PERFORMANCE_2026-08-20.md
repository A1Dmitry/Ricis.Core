# Ricis.Numerics comparative performance evidence

- Timestamp UTC: `2026-08-20T09:04:30.2173026+00:00`
- Runtime: `.NET 8.0.30`
- OS: `Ubuntu 24.04.4 LTS`
- Architecture: `X64`
- Protocol: Fixed deterministic 2048-bit operands; Release build; one warmup operation; result equality is checked against BigInteger before timing.

| Operation | Iterations | Custom ms | BigInteger ms | BigInteger / custom |
|---|---:|---:|---:|---:|
| Int2048 addition | 25000 | 11.104 | 10.097 | 0.909× |
| Int2048 subtraction | 25000 | 40.761 | 7.087 | 0.174× |
| Int2048 multiplication (low 2048 bits) | 2000 | 42.822 | 0.119 | 0.003× |
| Int2048 division | 40 | 18.590 | 0.072 | 0.004× |
| ULong2048 modular multiplication | 10 | 38.432 | 0.060 | 0.002× |
| RSA public operation e=65537 | 3 | 200.864 | 0.720 | 0.004× |

> This is reproducible comparative evidence, not a CI pass/fail speed threshold. CPU frequency, JIT, allocator and host contention make wall-clock thresholds unsuitable for a correctness gate.

NUMERICS_BENCHMARK_EVIDENCE=/home/ubuntu/Ricis.Core/Ricis.Numerics/PerformanceEvidence/NUMERICS_PERFORMANCE_2026-08-20.json
