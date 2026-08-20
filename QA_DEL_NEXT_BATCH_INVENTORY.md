# QA-DEL next batch inventory

**Status:** analysis only. No deletion, reduced visibility or reclassification is authorised by this inventory.

## Input and limitation

The source is the supplied solution-wide `IssuesReport.xml`. It predates completed Batches A/B, public-API coverage and the logical-API incident remediation. It therefore remains a useful project-model map, but it is not a current baseline. Before a finding can be closed, a fresh ReSharper report must be generated and compared by category.

## Classification

| Group | XML candidates | Current QA-DEL classification | Required next evidence | Allowed current action |
|---|---|---|---|---|
| Already remediated / stale rescan | `RicisPhaseTraceStep`, `PolarConverter`, `AlgebraicReductionVisitor` enumeration, `ExpressionSimplifierVisitor` nullability | `inspection_false_positive_or_stale` pending fresh scan | Current ReSharper scan plus existing regression/Release evidence | Do not edit solely from old offsets. |
| Source-generator C# errors | `RicisProofLogReportRenderer` `Default`/`JsonSerializerContext` errors | `inspection_false_positive_or_stale` | Current Release build 0 errors; future IDE rescan | Keep generated-code-compatible implementation. |
| Proof semantics condition | `RicisAcademicProofExtensions` `ConditionIsAlwaysTrueOrFalse` near old line 1723 | `proof_or_lean_contract` | Exact current code inspection, direct checked-proof/doc test and Lean boundary review | Preserve until a dedicated proof decision identifies redundancy. |
| Float equality in proof/math | `RicisAcademicProofExtensions`, `RicisVectorCalculusExtensions`, `RicisPreviousParameterIdentityProofCase`, `PolynomialParser`, `AlgebraicReductionVisitor` | `proof_or_lean_contract` or `exact_discrete_invariant` | Per-occurrence classification: exact structural constant vs approximation; test tolerance only where numerical contract requires it | No global epsilon rewrite. |
| Finance nullable conditions | `PaymentLaunch`, `BepaidPaymentLaunchPort` old findings | `provider_security_contract` | Current code review, FIN14/FIN15 and provider payload/security test | Preserve defensive URI/HTTPS/provider boundaries unless an exact redundant check is proved. |
| Regression-only float/style findings | 55 float comparisons plus ternary/nullable warnings in old test files | `test_quality_backlog` | Use central `AssertClose` only when numerical approximation is intended; leave exact integer/discrete assertions exact | Separate test-quality batch, no production semantic mix. |
| Remaining public global findings | public API surface already in `PUBLIC_API_COVERAGE_DECISION_MATRIX.md` | `public_compatibility` | API17–API32, FIN15–FIN18 and future SemVer decision | Preserve. |

## Next candidate: proof/document semantic coverage

The next non-deletion batch should start with the `RicisAcademicProofExtensions` old condition finding. It is chosen because it lies on proof/document surface and therefore needs the highest standard of evidence. The batch may add direct tests and a preserve/Remove Decision Record; it may not simplify the condition before the user accepts that record.

### Mandatory test-first scope

1. A direct checked-proof/document test that reaches the current condition and distinguishes both branches where they are observable.
2. A negative test proving unsupported Lean/general shape remains a controlled rejection rather than a generated theorem.
3. A trace/document test proving JSON, LaTeX and Lean review paths retain the same node-to-root evidence.
4. A current-code caller/contract graph containing public proof extensions, document factories, typed logs and Lean artifacts.

## QA-DEL disposition

| QA gate | Next batch disposition |
|---|---|
| `QA-DEL-01` | No removal planned; inventory creates no deletion decision. |
| `QA-DEL-02` | Proof/document surface is explicitly classified as potential API/Lean contract. |
| `QA-DEL-03` | Direct tests must be added before any condition change. |
| `QA-DEL-04` | User approval is required for any later removal/simplification. |
| `QA-DEL-05` | Full quality gate required after a future approved change. |
