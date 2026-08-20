# SPRINT Composite Fermat Pruning — Step 3: QA Matrix

**Status:** Approved and implemented. The direct regression suite contains `CFP-01` through `CFP-20` (21 executed MSTest cases because `CFP-02` is parameterized).

## 1. QA purpose

This matrix verifies the approved architecture as a **conditional bounded search system**. It proves neither universal factorization completeness nor O(1) factorization. It verifies exact local certificates, trace conservation, and that each newly public method has its own direct regression test.

The suite is added to `Ricis.Numerics.UnitTests` in a dedicated `CompositeFermatPruningSuite`; it does not put a `Ricis.Numerics` project reference into `Ricis.Core`.

## 2. Fixed fixtures

| Fixture | Exact fact/data | Architecture role |
|---|---|---|
| `N=10201=101²` | `B=101`, `StartDelta=0` | Zero-gap exact-square geometry and no candidate-loop ambiguity. |
| `N=5959=59·101` | `B=77`, `StartX=78`, `StartDelta=125`, Fermat certificate `X=80,Y=21` | Two-intersection start, odd factor reconstruction, and existing baseline continuity. |
| `N=10403=101·103` | `B=101`, `StartX=102`, `StartDelta=1` | Minimal positive two-intersection delta and close balanced odd factors. |
| `N=15=3·5` | Odd small semiprime | Small boundary corpus; direct manual candidate coverage. |
| Invalid `N∈{0,-1,2,10}` | Non-positive or even | Fail-closed public-input validation. |

The implementation may add a deterministic wider corpus of known odd semiprimes. Each fixture records an independently checked multiplication identity, not an inferred factor value.

## 3. Public-API direct regression coverage

| ID | Public method/type contract | Direct test and expected certificate |
|---|---|---|
| `CFP-API-01` | `FermatStartGeometry.Create(N)` | For 10201, declares `ExactSquare`, `B=101`, `StartX=101`, `StartDelta=0`. |
| `CFP-API-02` | `FermatStartGeometry.Create(N)` | For 5959 and 10403, declares `TwoIntersections`, verifies `B²≤N<(B+1)²`, `StartX=B+1`, and positive `StartDelta`. |
| `CFP-API-03` | `FermatPruningProfile` public factory | Rejects incoherent/publicly invalid bounds; contains no supplied `P`, `Q` or answer field. |
| `CFP-API-04` | `FermatTangentBand.Create/Enumerate` | Emits an in-range sequence with successive candidates differing by exactly two and with odd parity. |
| `CFP-API-05` | `CompositeFermatSearch.Search` | For every valid fixture returns an immutable trace and either an exact `P·Q=N` certificate or the explicit conditional `NotFoundWithinDeclaredProfile`. |
| `CFP-API-06` | Baseline/composite API separation | Existing `FermatFactorizer.Search(5959)` remains unchanged; the immutable trace is owned only by `CompositeFermatSearchResult`. |

A reflection-based API-surface test additionally asserts that every new public result type is sealed and has no public writable property. It supplements but never replaces the direct tests above.

## 4. Geometry and fail-safe range tests

| ID | Condition | Required assertion |
|---|---|---|
| `CFP-GEO-01` | Zero-gap start | Candidate counts are zero; trace has explicit `ExactSquare` state and no division-by-zero relative ratio. |
| `CFP-GEO-02` | Two intersections | `StartDelta>0`, documented symmetric endpoints are preserved as geometry evidence, and no root is rounded to create a fake coordinate. |
| `CFP-GEO-03` | `GeometricSpan < CalculatedSpan` | `RangeExpansionApplied=true`; `EffectiveSpan=CalculatedSpan`; end is the first parity-aligned calculated point at/after the requested boundary. |
| `CFP-GEO-04` | `GeometricSpan > CalculatedSpan` | `RangeExpansionApplied=false`; `EffectiveSpan=GeometricSpan`. |
| `CFP-GEO-05` | Equal spans | `EffectiveSpan` equals that shared exact value; no off-by-one expansion. |
| `CFP-GEO-06` | Parity adjustment near end | Result endpoint has the declared parity, is not below raw effective end, and preceding candidate is below that raw end. |

## 5. Tangent-band and direct factor-candidate tests

| ID | Condition | Required assertion |
|---|---|---|
| `CFP-TAN-01` | Odd `N`, lower-factor orientation | Every emitted `P` is odd, within the immutable band and strictly advances by `+2`. |
| `CFP-TAN-02` | Odd `N`, upper-factor orientation | Symmetric `Q` enumeration satisfies the same parity and step contract. |
| `CFP-TAN-03` | Known contained factor | If the declared effective band contains 59 or 101 for 5959, the run reaches it and outputs exact quotient/reconstruction. |
| `CFP-TAN-04` | Non-divisor in band | It cannot certify; `DivisibilityChecks` increments exactly once for that visited candidate. |
| `CFP-TAN-05` | Factor outside declared profile | Outcome is only `NotFoundWithinDeclaredProfile`; it never reports “prime”, “not factorable”, or an unverified factor. |
| `CFP-TAN-06` | Result immutability | Mutating a copied list/enumerator does not mutate final band/trace evidence; public result state has no setters. |

## 6. Filter-layer soundness and rejection coverage

The current modulo-64 filter is tested as an exact finite predicate. Every candidate square residue is accepted; representative non-square residues are rejected. CRT tables are not enabled in a profile until their exhaustive table proof passes.

| ID | Layer | Soundness test | Rejection/trace test |
|---|---|---|---|
| `CFP-FLT-01` | Input | Valid positive odd values continue. | Non-positive/even values fail before geometry exists. |
| `CFP-FLT-02` | Relative bounds | A known contained factor is retained. | An outside candidate is excluded with `RelativeBounds` reason. |
| `CFP-FLT-03` | Tangent parity | Odd contained factor remains reachable via `+2`. | An even candidate is never emitted for odd `N`. |
| `CFP-FLT-04` | Mod-64 mask | Exhaustively: for `y=0…63`, mask accepts `(y² mod 64)`. | Each known non-square residue is rejected; counters balance. |
| `CFP-FLT-05` | CRT mod 7 | Exhaustively: for `y=0…6`, accepted set contains `y² mod 7`. | A residue outside square set is rejected and counted. |
| `CFP-FLT-06` | CRT mod 31 | Exhaustively: for `y=0…30`, accepted set contains `y² mod 31`. | A non-residue is rejected and counted. |
| `CFP-FLT-07` | CRT mod 127 | Exhaustively: for `y=0…126`, accepted set contains `y² mod 127`. | A non-residue is rejected and counted. |
| `CFP-FLT-08` | Exact root | All retained Fermat squares meet `Y²=Δ`. | A mask/CRT-passing non-square never becomes a certificate. |
| `CFP-FLT-09` | Reconstruction | Returned `P,Q>1`, canonical order and `P·Q=N`. | Any mismatch is rejected fail-closed. |

The mask/CRT proof scope is strictly the `Δ=X²−N` Fermat-coordinate path. The tangent P/Q path gets no residue filter until it has a separate predicate and this same no-false-negative matrix for its own parameter.

## 7. Trace conservation and report-graph tests

Every successful or bounded-not-found run must provide a complete `FermatRegionTrace`.

```text
InitialCandidates
 ≥ AfterRelativeBounds
 ≥ AfterParity
 ≥ AfterBitMask
 ≥ AfterCrtResidues
 ≥ ExactRootChecks
 ≥ ExactSquares
 ≥ FinalReconstruction
```

For the tangent ordering, the corresponding invariant is:

```text
TangentBandCandidateCount
 = PFactorCandidates + QFactorCandidates
 ≥ DivisibilityChecks
 ≥ FinalReconstruction.
```

Tests also preserve `CandidatePoints=MaskRejected+MaskPassed` on the existing baseline. The implemented trace exposes only exact integer values required by a future visual renderer: geometric boundary/inner band, parity-step P/Q candidates, and staged retained-candidate counts. Rendering itself is explicitly deferred; it must not use floating-point values as certificate data when separately requested.

## 8. Fixed-width, oracle and non-claim gate

| Gate | Required condition |
|---|---|
| Root primitive | The implemented `CompositeFermatSearch` uses an internal fail-closed 2048-bit gateway to `ULong2048.IntegerSquareRootFloor`; direct test `CFP-14` preserves `r²≤v<(r+1)²`. |
| Baseline compatibility | Existing `FermatFactorizer.Search(BigInteger)` remains a separate baseline/oracle and its 6 existing direct regressions pass unchanged. |
| Complexity wording | XML documentation, Markdown and test names contain no claim of O(1) factorization. Any fixed-width O(1) statement is limited to the individual mask lookup/bit operation. |
| Cross-width corpus | The current public boundary is `BigInteger`, while every new root is fixed-width. A native `ULong2048` public input overload remains separately approved work and requires multi-bit-length direct corpus coverage. |

## 9. Quality-gate acceptance

The implementation phase may begin only after all tests above are created or explicitly marked deferred with a reason, and the following gates pass after implementation:

```bash
python3 scripts/verify_solution_projects.py
dotnet build Ricis.Core.sln -c Release --no-restore --nologo
python3 scripts/generate_mstest_regression_adapter.py --check
python3 scripts/generate_ulong2048_integral_operators.py --check
dotnet test UnitTests/Ricis.Core.UnitTests.csproj -c Release --no-build
dotnet test Ricis.Numerics/Ricis.Numerics.UnitTests/Ricis.Numerics.UnitTests.csproj -c Release --no-build
dotnet run --project RegressionTests/Ricis.Core.RegressionTests.csproj -c Release --no-build
dotnet run --project Ricis.Finance/Ricis.Finance.RegressionTests/Ricis.Finance.RegressionTests.csproj -c Release --no-build
python3 scripts/verify_lean_artifacts.py
```

No production implementation, README or visual report may infer a global complexity or completeness property from this local matrix.
