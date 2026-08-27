# Generic Fermat `INumber<T>` capability boundary — 2026-08-20

**Status:** Deferred — capability contract mapped; generic production solver is not implemented.
**Evidence:** [`INUMBER_SEMIPRIME_PATH_AUDIT_2026-08-20.md`](../INUMBER_SEMIPRIME_PATH_AUDIT_2026-08-20.md), [`SPRINT_UNIVERSAL_INUMBER_REDUCTION_STEP2_ARCHITECTURE.md`](../SPRINT_UNIVERSAL_INUMBER_REDUCTION_STEP2_ARCHITECTURE.md), [`SPRINT_UNIVERSAL_INUMBER_SEMIPRIME_STEP3_QA_SPEC.md`](../SPRINT_UNIVERSAL_INUMBER_SEMIPRIME_STEP3_QA_SPEC.md), [`FermatSemiprimeSuite.cs`](Ricis.Numerics.UnitTests/FermatSemiprimeSuite.cs), and [`CompositeFermatPruningSuite.cs`](Ricis.Numerics.UnitTests/CompositeFermatPruningSuite.cs).
**Rule:** `INumber<T>` generic arithmetic alone does not imply exact square-root, bit-length, residue-mask, ordering-band or fixed-width conversion capabilities.

| Capability | Required by generic Fermat path | Current evidence | Status |
|---|---|---|---|
| Exact square root / correction | `ceil(sqrt(N))`, exact square test and one-bit correction | Current Fermat and ULong shift-root suites are concrete-type evidence | `Deferred` generic contract |
| Bit length / fixed-width bounds | Search interval and mask width | ULong2048 direct tests and fixed-width artifacts | `Deferred` generic contract |
| Square-residue filters | Exact modulo-64/CRT admissibility | Fermat and Composite Fermat bounded suites | `Deferred` generic contract |
| Ordering / range comparison | `p <= q`, order-band and fail-closed bounds | Semiprime domain tests | `Deferred` generic contract |
| Candidate-step semantics | Odd-factor parity step and bounded progression | Composite Fermat pruning suite | `Deferred` generic contract |
| Exact reconstruction | `P = x-y`, `Q = x+y`, verify `P*Q=N` | `FermatSemiprimeSuite` direct evidence | `Tested` for current concrete path |
| Fixed-width integration | Int2048/ULong2048 without Core→Numerics dependency | Universal-INumber integration boundary | `Tested` at integration boundary |

## BFR evidence gate

| Gate | Required evidence | Current status |
|---|---|---|
| `BFR-E01` | Exact generic square root and correction | `Deferred` |
| `BFR-E02` | Generic bit length and fixed-width bounds | `Deferred` |
| `BFR-E03` | Generic square-residue capability | `Deferred` |
| `BFR-E04` | Generic ordering and search-band comparison | `Deferred` |
| `BFR-E05` | Generic candidate-step semantics | `Deferred` |
| `BFR-E06` | Exact generic reconstruction and product verification | `Deferred` |
| `BFR-E07` | Direct Int2048/ULong2048 integration evidence | `Deferred` |
| `BFR-E08` | Complexity and candidate-accounting evidence | `Deferred` |

No `O(1)` or universal-factorization statement is authorized before all eight gates are present.

## Explicit non-claims

The current repository proves concrete bounded invariants and local pruning behavior. It does not provide a generic Fermat solver for every `T : INumber<T>`, does not hide a `BigInteger` fallback behind a generic API, and does not establish universal completeness or constant-time factorization.
