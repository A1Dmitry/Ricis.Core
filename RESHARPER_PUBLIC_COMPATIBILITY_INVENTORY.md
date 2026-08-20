# ReSharper public compatibility inventory

**Status:** preservation inventory. `UnusedMember.Global` means that no current in-solution caller was found; it does not authorise a patch-level API deletion.

## Decision rule

Each public, protected, interface or extension member remains part of the compatibility surface until it has: a direct API regression, a documented migration target, a SemVer/deprecation decision and, for a later removal, a major-version approval. No reflection-based reachability exception is used; preservation follows the published API and documented domain contract.

| Group | Candidates | Classification | Direct-test owner | Current decision |
|---|---|---|---|---|
| Expression system | `ExpressionSystem.IsStructuralZero`, `ToVector`; `RicisMatrixExpression.Rows` | Public structural/vector API | `ExpressionSystemSuite`, `RicisMatrixExpressionSuite` | Preserve; add direct API cases before any deprecation. |
| Expression utilities | `Evaluate`, `ShouldCommute`, `FindParameter`, `IsTranscendentalCandidate`, `ToBigInteger` extension methods | Public extension API; may be invoked through extension syntax by external consumers | `RicisPublicUtilitySuite` / dedicated extension suite | Preserve. Test each extension directly. |
| Proof compatibility aliases | `ProveChecked`, `ProveDocumentChecked`; `SimplifyWithLog` | Versioned proof/log/document compatibility API | `RicisCheckedProofSuite`, `RicisTypedProofLogSuite` | Preserve. Aliases are intentionally compatibility-facing. `RicisProofDocumentTemplates` is internal and covered as implementation. |
| Financial expression extension | `RicisCompoundInterestExtensions` | Public symbolic finance expression API | `RicisCompoundInterestSuite` | Preserve; potentially external Console/API client contract. |
| Solver and polar utilities | `PolarConverter.ToPolarSector`, `PolynomialZeroSolver.FindRootsInRange`, `ExponentialZeroSolver.Solve`, `LogSolver.Solve` | Public calculator/solver API | `RicisPublicUtilitySuite`, solver regression suites | Preserve. Add edge/result/rejection tests. |
| Legacy simplifier façades | `AlgebraicSimplifier.Apply`, `RicisTransformPhase.Apply` | Potentially redundant public façade over pipeline | `RicisPublicCompatibilitySuite` | Preserve now; only deprecate after a migration decision to `RicisPhasePipeline`. `VisitLogical` was internal, zero-caller and removed as a duplicate of `LogicalReductionVisitor`. |
| Finance application ports | `PaymentRailRegistry.GetCapabilities`, `IAnnualTaxPolicy.EvaluateAnnualPosition`, `IBankFeeSchedule.QuoteAsync`, `ITaxReceiptGateway.SubmitAsync` | Future-capability/compliance port | Finance regression suite and FIN backlog | Preserve as documented FIN capability. |
| Finance domain lifecycle | `CounterpartyKind.Individual`, `SettlementStatus.Reconciled`, `PayoutStatus.Allocated`, `Settlement.Confirm`, `Settlement.Reject`, tax status enum states | Domain state model, not current usage metric | Finance domain regression suite | Preserve; usage absence is expected before later FIN workflows. |

## Required direct API regression backlog

| ID range | Scope | Required evidence |
|---|---|---|
| `API17–API21` | Expression system, matrix and public utility extensions | Extension/direct invocation and positive/negative structural cases. |
| `API22–API25` | Proof aliases and `SimplifyWithLog` | Same derivation/trace/doc output and controlled Lean boundary; internal templates stay covered by document-format suites. |
| `API26–API29` | Polar, polynomial, exponential and logarithm public solvers | Exact roots, non-root, invalid shape and deferred-expression behavior. |
| `API30–API31` | Legacy simplifier façade APIs | Result equivalence with normative pipeline. `API32` was retired after internal zero-caller duplicate removal. |
| `FIN15–FIN18` | Finance capabilities, port contracts and lifecycle transitions | Explicit not-supported/reserved behavior, domain transition guards and no payment-fact fabrication. |

The numbers reserve regression identifiers only. Actual tests are added in subsequent atomic batches, with the policy that public API changes require direct tests before modification.

## Explicit exclusions

This inventory does not deprecate or remove source-generated JSON DTO properties, bePaid request fields or nullable security guards. It does not claim that uncalled domain capability means obsolete domain capability. It also does not mix public API migration with the C# Core-backed proof endpoint sprint.
