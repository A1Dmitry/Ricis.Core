# SPRINT Composite Fermat Pruning — Step 1: Business Specification

**Status:** Approved and implemented through the bounded `CompositeFermatSearch` path; visual rendering remains a separately requested presentation increment.

## 1. Objective

Implement the user-specified **composite pruning system**, not a standalone alternating-mask check. The system begins with a relative min/max Fermat region, then applies independent exact filters in an auditable order. The principal result is the remaining portion of the original bounded interval after every filter layer.

## 2. Input and primary relative region

For an odd target `N`, the bounded balanced profile is expressed by Fermat coordinates:

```text
Δ(X) = X² − N,
P = X − Y,
Q = X + Y,
Δ(X) = Y².
```

The initial profile region has an exact **two-intersection start construction**. Let `B=floor(√N)` and use the integer certificate:

```text
B² ≤ N < (B+1)².
```

If `B²=N`, then `Y=0`, the curve has the zero-gap intersection and the factorization attempt ends as an exact-square case. Otherwise set `X0=B+1=ceil(√N)`. The vertical Fermat line `x=X0` intersects the real curve `x²−y²=N` at the symmetric points:

```text
Y0 = ±√(X0²−N).
```

The bounded area between this next-square boundary and the curve is the **start search region**. Its discrete projection is then bounded by:

```text
√N ≤ X < √(9N/8),
0 ≤ Y < √(N/8),
0 ≤ Y < X/3.
```

Candidates have the parity selected by `N mod 4` and advance by two. The trace records `B`, `X0`, whether the zero-gap case occurred, the initial delta `Δ(X0)` and the discrete candidate count. No mask is allowed to replace this region or claim a factorization answer independently.

A refined public order band provides the user’s min/max range:

```text
Pmin(N) ≤ P ≤ Q ≤ Pmax(N),
Pmin(N) ≤ √N ≤ Pmax(N).
```

The remaining interval must be reported relative to both the initial Fermat candidate count and the declared `Pmin/Pmax` band. `p,q` are fixture-oracle values only; N-only runtime filtering receives only `N` and public profile parameters.

## 3. Filter layers and soundness rule

| Order | Layer | Predicate | Soundness obligation | Trace quantity |
|---:|---|---|---|---|
| 1 | Input | `N>0`, odd | Invalid input is rejected before search. | Initial region status. |
| 2 | Start geometry | `B²≤N<(B+1)²`; `X0=B` for zero gap or `X0=B+1` otherwise | The real curve receives the exact zero-gap point or the two symmetric start intersections. | `B`, `X0`, `Δ(X0)`, zero-gap/two-intersection status. |
| 3 | Relative region | BFR-2/order-band bounds | The true fixture Fermat point lies in the declared interval when the profile holds. | Candidate count after bounds. |
| 4 | Parity | `X mod 2` from `N mod 4` | Eliminates only impossible Fermat coordinates. | Parity-rejected / retained. |
| 5 | Bit mask | Complementary mask and/or square-residue mask derived from the candidate representation | A known exact square `Y²` never fails. Alternating masks alone are not sufficient unless tied to `N,X,Δ`. | Mask-rejected / retained. |
| 6 | CRT/residue | Membership in precomputed square residues for selected coprime moduli | Every integer square has an accepted residue for every configured modulus. | Per-modulus and combined reject count. |
| 7 | Exact certificate | Fail-closed fixed-width `ULong2048.IntegerSquareRootFloor` gateway and `Y²=Δ` | Only exact square deltas pass. | Exact-root checks / accepted point. |
| 8 | Reconstruction | `P=X−Y`, `Q=X+Y`, `P·Q=N` | Returned result reconstructs N exactly. | Final certificate. |

A layer may reject a candidate only after a direct test proves it has **no false negative** on a deterministic positive corpus. A surviving candidate is not a factorization result until the final exact certificate and reconstruction pass.

## 4. Trace contract

The immutable trace reports at least:

```text
StartSquareFloor,
StartCandidateX,
StartDelta,
StartGeometryStatus,
InitialCandidates,
AfterRelativeBounds,
AfterParity,
AfterBitMask,
AfterCrtResidues,
ExactRootChecks,
ExactSquares,
FinalReconstruction,
RemainingFractionOfInitialRegion.
```

The remaining fraction is descriptive evidence, not an O(1) claim. For a fixed 2048-bit representation, individual mask operations are width-bounded; the total factorization work still includes the number of candidates that remain after all layers.

## 5. Business acceptance criteria

1. The system uses the relative min/max region as its first-class search domain.
2. Every layer has a separately reported retained/rejected count.
3. The trace distinguishes geometric/relative pruning from bit-mask and CRT pruning.
4. No known valid fixture target is rejected by any individual layer.
5. No document or code path claims constant-time factorization merely because a fixed-width mask predicate is O(1).
6. The design remains compatible with `Ricis.Core` symbolic proof trace and `Ricis.Numerics` numeric execution without a Core→Numerics dependency.
