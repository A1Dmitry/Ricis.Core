# Longest RICIS Route — Lean Design Contract

**Route:** `math-singularity → Hodge → group-ring zero divisors → Weierstrass singularity → Atiyah–Singer → Poincaré → Morse → knot theory → spectral asymptotics → ReduceToRicisCore(spectral asymptotics)`.

## Formal claim

The Lean artifact proves a **route-composition theorem**. It does not state or prove the standard external mathematical theorems named by individual catalogue nodes. It proves that, if each declared node carries the explicit RICIS local certificate extracted by the engine and each declared dependency carries a transition certificate, then the invariant held at `math-singularity` is preserved through the exact nine-edge route to the spectral RICIS-core endpoint.

| Layer | Lean representation | Proof boundary |
|---|---|---|
| Route identity | `LongestRouteNode` inductive type with 10 constructors | Exact catalogue route only. |
| L1 identity | `l1Preserves` field | An explicit premise, not inferred from a label. |
| SP4 indexing | `sp4Preserves` field | An explicit premise for indexed singularity representation. |
| SP2 reduction | `sp2Preserves` field | An explicit premise for safe prior reduction. |
| A6 transform | `a6Preserves` field | An explicit premise for the accepted transform. |
| L1 verification | `verifyPreserves` field | An explicit premise for terminal invariant check. |
| Dependency edge | one named implication per graph edge | Each graph hop is explicit and individually reusable. |

## Kernel-checkable theorem sequence

1. Each local node theorem composes L1 → SP4 → SP2 → A6 → L1 verification.
2. Nine named dependency theorems encode every edge of the selected longest route.
3. Prefix theorems expose each depth as a direct proof checkpoint.
4. The terminal theorem composes all nine dependencies and all ten local node certificates.
5. A companion theorem proves the A6 payload product is structural and commutative over an arbitrary commutative semiring; it does not assign a numerical value to a singularity.

## Non-claims

The artifact must not contain `sorry`, `admit`, `axiom` or a theorem whose conclusion says that the Hodge conjecture, Poincaré conjecture, Atiyah–Singer theorem, knot theory or spectral asymptotics has been independently solved in its standard mathematical sense. Their catalogue names label nodes in an engine route; their standard-domain proofs remain separate work.
