# Public API coverage and decision matrix

**Status:** coverage sprint completed; no public API removal or deprecation is approved in this version.

## Decision summary

The ReSharper `UnusedMember.Global` inventory has been reviewed against direct regression evidence. The in-solution call graph is now supplemented by public API tests, not used as a removal criterion. Every public candidate remains **preserved** for the current release. Two candidates initially grouped with public surface were reclassified as internal implementation leaves and handled separately.

| API / candidate | Direct coverage | Decision | Rationale |
|---|---|---|---|
| `ExpressionSystem.IsStructuralZero`, `ToVector` | `API17` | Preserve | Structural-system interoperability is a stable public contract. |
| `RicisMatrixExpression.Rows` | `API18` | Preserve | Matrix row view is externally useful and has exact shape/identity coverage. |
| `ExpressionExtensions.Evaluate` | `API19` | Preserve | Finite expression evaluation is a public extension contract. |
| `ShouldCommute`, `FindParameter` | `API20` | Preserve | Deterministic normalization and parameter discovery are public structural utilities. |
| `IsTranscendentalCandidate`, `ToBigInteger` | `API21` | Preserve | Classification and tolerant numeric conversion are documented extension capabilities. |
| `RicisPhasePipeline.SimplifyWithLog` | `API22` | Preserve | Typed audit is the supported renderer-independent trace path. |
| `ProveChecked` | `CHECKED01–CHECKED04` | Preserve | Public proof verification compatibility entrypoint. |
| `ProveDocumentChecked` | `CHECKED05` | Preserve | Public checked-document compatibility entrypoint. |
| `RicisProofDocumentTemplates` | Existing format suites | Reclassify internal | The type is `internal`; it is not a public compatibility node. |
| `PolarConverter.ToPolarSector` | `API26` | Preserve | Public singularity visualization/diagnostic formatting API. |
| `PolynomialZeroSolver.FindRootsInRange` | `API27` | Preserve | Public bounded numerical root convenience API. |
| `ExponentialZeroSolver.Solve` | `API28` | Preserve | Public specialized root adapter with exact/unsupported contracts. |
| `LogSolver.Solve` | `API29` | Preserve | Public specialized root adapter with exact/unsupported contracts. |
| `AlgebraicSimplifier.Apply` | `API30` | Preserve | Existing public compatibility façade; no migration decision exists. |
| `RicisTransformPhase.Apply` | `API31` | Preserve | Existing public compatibility façade; no migration decision exists. |
| `LogicalSimplifier.Apply` | `API32`, `LOG01–LOG09` | Preserve | Restored explicit public logical-reduction façade over the normative `LogicalReductionVisitor`; safe identities are available independently from the full pipeline while impure short-circuit semantics are preserved. |
| `RicisCompoundInterestExtensions` | `INT01–INT07`, `QA08–QA09` | Preserve | Public symbolic finance expression extension. |
| Finance `GetCapabilities`, `EvaluateAnnualPosition`, `QuoteAsync`, `SubmitAsync`, `Confirm`, `Reject` | `FIN15–FIN18` | Preserve | Explicit capability/port/lifecycle contracts; no product decision authorises removal. |
| Finance domain enum states | `FIN16`, lifecycle regression | Preserve | Future policy/lifecycle state space; absence of current workflow use is not obsolescence. |

## Coverage index

| Range | Result |
|---|---|
| `API17–API22` | Added in this sprint. |
| `CHECKED01–CHECKED05` | Confirmed direct existing coverage for checked proof APIs. |
| `API26–API31` | Added in this sprint. |
| `API32` | Restored public `LogicalSimplifier.Apply` direct coverage. `LOG01–LOG09` remain authoritative normative logical-stage coverage. |
| `FIN15–FIN18` | Added in this sprint. |

## Future deprecation rule

A public member may move from **Preserve** to **Deprecate** only when all of the following are recorded in a separate approved migration document:

1. a public replacement API and direct equivalence/migration test exist;
2. an `[Obsolete]` phase and user-facing migration notice are defined;
3. SemVer impact is approved for the target release; and
4. the removal is scheduled only for the next allowed major version.

Until then, direct test coverage and continued preservation are the required outcome of the ReSharper cleanup process.
