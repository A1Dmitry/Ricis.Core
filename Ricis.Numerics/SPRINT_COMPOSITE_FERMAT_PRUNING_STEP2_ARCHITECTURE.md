# SPRINT Composite Fermat Pruning — Step 2: Architecture Contract

**Status:** Approved and implemented.

## 1. Project boundary

`Ricis.Numerics.Factorization` owns numeric search geometry, candidate filtering and immutable counters. `Ricis.Core` remains a symbolic reduction/proof engine and must not reference `Ricis.Numerics`. A caller that needs a Core document passes immutable primitive evidence to a Core-neutral adapter boundary; no Numeric type crosses into Core production APIs.

```text
N-only caller
   │
   ▼
Ricis.Numerics.Factorization
   ├── FermatStartGeometry
   ├── FermatPruningProfile
   ├── FermatRegionTrace
   └── CompositeFermatSearch.Search(N, profile)
          │ immutable primitive evidence
          ▼
caller-side Core evidence adapter
          │
          ▼
Ricis.Core symbolic reduction / Log / JSON / LaTeX / Lean-oriented report
```

## 2. Immutable domain model

| Type | Construction | Responsibility |
|---|---|---|
| `FermatStartGeometry` | `Create(N)` | Stores `FloorRoot=B`, `StartX`, `StartDelta`, `GeometryStatus` and the two-intersection geometric span. It validates `B²≤N<(B+1)²`; distinguishes `ExactSquare` from `TwoIntersections`. |
| `FermatPruningProfile` | default deterministic BFR-2 profile derived from `N` | Holds selected ordering and explicit inner tangent-band offset. BFR-2 bounds and parity are deterministically derived from `N`; the profile never accepts `p`, `q`, `gap` or an answer. |
| `FermatTangentBand` | derived from geometry + profile | Represents the integral inside-of-curve P/Q candidate approximation and emits each side in odd `+2` steps. |
| `FermatSearchOrdering` | enum | Names the separate `FermatCoordinates`, `TangentLowerFactor` and `TangentUpperFactor` orderings. |
| `FermatRegionTrace` | internal data builder → immutable public result | Stores geometry, retained/rejected counters and each applied layer's exact evidence. |
| `CompositeFermatSearchResult` | `CompositeFermatSearch.Search` | Holds either a final exact certificate or `NotFoundWithinDeclaredProfile`, always with a frozen trace. |

All public result objects are sealed immutable records/classes. The builder is internal and cannot escape before search completion.

## 3. Exact geometry

For a positive odd `N` of at most 2048 bits, compute `B=floor(√N)` through `ULong2048.IntegerSquareRootFloor` behind an internal fail-closed gateway and establish:

```text
B² ≤ N < (B+1)².
```

| Geometry status | `StartX` | `StartDelta` | Meaning |
|---|---:|---:|---|
| `ExactSquare` | `B` | `0` | Zero-gap curve intersection; no two-point initial area. |
| `TwoIntersections` | `B+1` | `(B+1)²−N>0` | The real Fermat curve has symmetric start intersections `±√StartDelta`; its bounded discrete projection begins at the parity-correct `X`. |

This state is always traceable before parity/mask/CRT work begins.

### 3.1 Fail-safe effective range

For a two-intersection start, the geometric construction supplies a `GeometricSpan`: the maximum distance induced by the two symmetric curve intersections after the start square and side point are fixed. The declared relative/order-band calculation independently supplies a `CalculatedSpan`.

```text
EffectiveSpan = max(GeometricSpan, CalculatedSpan)
EffectiveEnd = first parity-aligned candidate at or beyond StartX + EffectiveSpan
```

This rule prevents a local geometric span from silently shrinking the declared search profile. When `GeometricSpan < CalculatedSpan`, the runner expands to `EffectiveEnd` using the next calculated parity-aligned point; it records `RangeExpansionApplied=true` and both source spans. When geometry is equal to or wider than the calculated range, it remains the effective boundary. `ExactSquare` has zero span and terminates before candidate iteration.

## 4. Discrete inner tangent band and factor candidates

The two-intersection construction is represented computationally as a **discrete inner tangent band**, not as an unbounded continuous line. It is derived from the curve-adjacent start geometry, BFR-2 y constraints and `EffectiveEnd`. The explicit profile offset is retained as immutable visual geometry evidence; it does not silently remove an integer candidate. The band is entirely integral and exposes no rounded floating-point coordinates.

For an odd `N`, it emits factor coordinates directly, with the required parity-preserving step:

```text
P₀ = first odd tangent-band candidate in EffectiveRange
Pᵢ₊₁ = Pᵢ + 2
Qᵢ = N / Pᵢ only after N mod Pᵢ = 0
```

The symmetric orientation scans `Q` candidates with exactly the same `+2` step. The profile states which orientation is primary; the canonical band and trace are shared. The band is only a bounded search ordering: it does not assume a geometrically adjacent discrete point is a factor. Certification remains fail-closed: exact divisibility, exact quotient, then `P·Q=N`.

The trace includes `TangentBandOffset`, `TangentBandCandidateCount`, `PFactorCandidates`, `QFactorCandidates`, `ParityStep=2`, `DivisibilityChecks` and selected orientation. Missing both factors in the effective profile yields `NotFoundWithinDeclaredProfile`, not a claim about compositeness or primality.

## 5. Filter pipeline

The pipeline has one canonical candidate loop. Each layer receives a candidate and returns a named immutable decision:

```text
Retain | Reject(reason) | Certify(result)
```

A layer may never mutate geometry, remove prior trace events or imply a later certificate. The trace names the search ordering, so the existing Fermat-coordinate baseline and the new tangent-band factor ordering cannot be conflated.

The Fermat-coordinate baseline performs:

1. Validate N and create `FermatStartGeometry`.
2. Derive fail-safe effective range and profile parity.
3. Visit only parity-aligned `X` candidates in the declared region.
4. Apply a configured bit-mask predicate to `Δ=X²−N`.
5. Apply CRT square-residue predicates to the same `Δ`.
6. Run exact square-root certification only for retained candidates.
7. Reconstruct `P=X−Y`, `Q=X+Y`, and validate `P·Q=N`.

The distinct tangent-band ordering performs the `+2` P/Q scan from section 4 and certifies with exact divisibility and reconstruction. Mask/CRT filters may be added to that ordering only after direct soundness evidence for the tangent parameter—not by reusing a proof concerning `Δ=X²−N`.

The current modulo-64 square mask becomes the first `BitMask` implementation. CRT moduli are configured only after each has its precomputed square-residue oracle and direct no-false-negative QA.

## 6. Trace and relative-area evidence

`FermatRegionTrace` exposes integer counters and exact rational reporting inputs, never floating-point percentages:

```text
GeometricSpan,
CalculatedSpan,
EffectiveSpan,
RangeExpansionApplied,
TangentBandOffset,
TangentBandCandidateCount,
PFactorCandidates,
QFactorCandidates,
ParityStep,
DivisibilityChecks,
InitialCandidates,
AfterRelativeBounds,
AfterParity,
AfterBitMask,
AfterCrtResidues,
ExactRootChecks,
ExactSquares,
FinalReconstruction.
```

The renderer calculates a fraction `remaining/initial` from integers. It may format a percentage for presentation, but the immutable trace stores numerator and denominator. A zero initial region and zero-gap exact-square status are explicit states, not divide-by-zero fallbacks.

## 7. Soundness constraints

* A square-residue mask is sound only if every `Y²` passes it.
* A CRT predicate is sound only if every `Y² mod m` is in its accepted residue set for every configured `m`.
* The tangent band is sound as an ordering only conditionally: if the declared profile contains an actual factor, the `+2` scan preserves its odd parity and reaches it; otherwise a trace reports `NotFoundWithinDeclaredProfile`, never `NotFactorable`.
* Relative min/max profile filtering is conditional: the effective end uses the fail-safe `max(GeometricSpan, CalculatedSpan)` rule, and a trace reports `NotFoundWithinDeclaredProfile`, never `NotFactorable`.
* The complete system may claim only measured candidate reduction. Fixed-width mask operation cost does not establish total O(1) factorization complexity.

## 8. Compatibility

The existing `FermatFactorizer.Search(BigInteger)` remains an unchanged baseline/oracle path. `CompositeFermatSearch` is a separate bounded result API: it accepts `BigInteger` at its public boundary but delegates every new production square-root operation to `ULong2048.IntegerSquareRootFloor` through a 2048-bit fail-closed gateway. No Core project reference is introduced.
