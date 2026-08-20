# Public API coverage sprint — completed evidence

**Status:** Implemented and regression-verified; this document is synchronized to the current gate evidence below. Earlier local counts are retained only as historical context where explicitly labelled.

## Direct coverage added

| ID | Public contract |
|---|---|
| `API17` | `ExpressionSystem.IsStructuralZero` and `ToVector` expose exact structured-system state and vector interoperability. |
| `API18` | `RicisMatrixExpression.Rows` exposes a stable row/coordinate view. |
| `API19` | Public `Expression.Evaluate` overloads evaluate finite expressions by parameter and name. |
| `API20` | `ShouldCommute` and `FindParameter` retain deterministic structural utility contracts. |
| `API21` | `IsTranscendentalCandidate` and `ToBigInteger` retain classification/conversion behavior. |
| `API22` | `RicisPhasePipeline.SimplifyWithLog` produces normative result and typed audit lifecycle. |
| `API26` | `PolarConverter.ToPolarSector` renders a typed singularity/root diagnostic. |
| `API27` | `PolynomialZeroSolver.FindRootsInRange` returns bounded approximate roots. |
| `API28` | `ExponentialZeroSolver.Solve` returns supported exact root and no invented unsupported root. |
| `API29` | `LogSolver.Solve` returns supported exact root and no invented unsupported root. |
| `API30` | `AlgebraicSimplifier.Apply` compatibility façade retains safe arithmetic reduction. |
| `API31` | `RicisTransformPhase.Apply` compatibility façade preserves ordinary finite expression. |
| `FIN15` | `PaymentRailRegistry.GetCapabilities` normalizes country and never invents default routes. |
| `FIN16` | `ITaxPolicy.EvaluateAnnualPosition` accepts declared `Individual` partition without aggregate-owned tax policy. |
| `FIN17` | `IBankFeeSchedule.QuoteAsync` and `ITaxReceiptGateway.SubmitAsync` remain explicit asynchronous infrastructure ports. |
| `FIN18` | `PayoutRequest.Confirm` and `Reject` enforce submitted-only lifecycle transitions. |

Existing direct contracts `CHECKED01–CHECKED05` retain coverage for `ProveChecked` and `ProveDocumentChecked`; `INT01–INT07`/`QA08–QA09` retain coverage for compound-interest public extensions.

## Reclassification result and incident correction

`RicisProofDocumentTemplates` remains an internal implementation detail and is covered through document-format tests. The original conclusion about `ExpressionSimplifierVisitor.VisitLogical` was incorrect as an API decision: even though the method itself was internal and had zero callers, it represented a potential focused logical-reduction façade. Its silent removal triggered `INCIDENT_2026-08-20_SILENT_LOGICAL_API_REMOVAL.md`.

The remediation restores the supported public contract as `LogicalSimplifier.Apply`, which delegates to the normative `LogicalReductionVisitor` and is directly tested by `API32` as well as the authoritative `LOG01–LOG09` suite. The unsafe duplicate implementation is not restored; the public façade is the single supported route.

## Public surface decision

All actual public candidates are **preserved** in the current version. No `[Obsolete]` attribute or deletion is introduced. Any future deprecation requires a specific migration target, direct replacement equivalence test, release-version decision and major-version removal approval, as recorded in `PUBLIC_API_COVERAGE_DECISION_MATRIX.md`.

## Mandatory quality gates

| Gate | Result |
|---|---:|
| `dotnet build Ricis.Core.sln --configuration Release` | PASS, **0 warnings, 0 errors** |
| Core self-contained regression harness | PASS, **386/386** current catalog |
| Finance self-contained regression harness | PASS, **18/18** |
| `python3 scripts/verify_lean_artifacts.py` | PASS, **8 artifacts** |
| `git diff --check` | PASS |
