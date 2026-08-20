# ReSharper Batch A decision matrix — 2026-08-20

**Iteration:** 1/5
**Result:** No safe deletion authorized in this increment. The caller graph identified false positives and already-remediated candidates.

| ID | Candidate from remediation plan | Current source evidence | Decision | Required next gate |
|---|---|---|---|---|
| A-01 | `ExpressionSimplifierVisitor._parameters` | Must be rechecked against current constructor/state; no deletion based only on IDE reachability. | Preserve pending exact graph. | Direct simplifier regression and reflection/serialization check. |
| A-02 | `ExpressionSimplifierVisitor.SimplifyFraction` | Current code calls `SimplifyFractionSum` and `SimplifyFractionProduct`; the exact candidate name is absent, while fraction helper behavior is live. | **Do not delete.** ReSharper candidate is stale/misidentified. | Keep fraction/singularity tests; refresh XML candidate mapping. |
| A-03 | Private `ToBigInteger` | Candidate name is not present in the current production source; remaining `ToBigInteger` matches are intentional numeric/test helpers. | **Already remediated or stale report entry.** No code change. | Refresh report baseline. |
| A-04 | `SingularitySolver.IsTranscendentalComposite` | Symbol is absent from current source. | **Already remediated or stale report entry.** No code change. | Refresh report baseline. |
| A-05 | `RicisAcademicProofExtensions.solutionX/solutionY` | Values are used for finite checks, proof expressions, substitutions and rendered proof text. | **Live semantic state; preserve.** | Existing academic/Jacobian proof regressions remain mandatory. |
| A-06 | `ProviderPayment.NormalizeCurrency.parameterName` | Current helper signature is `NormalizeCurrency(string value)` and callers use it for both source/target currencies; the candidate parameter is absent. | **Already remediated or stale report entry.** No code change. | Refresh report baseline and retain currency regression. |
| A-07 | `SingularitySolver.TryGetPositiveConstant(..., out value)` | Symbol is absent from current source. | **Already remediated or stale report entry.** No code change. | Refresh report baseline; do not infer deletion from absence alone. |

## QA conclusion

The Batch A source graph prevents a false cleanup: three candidates are live or structurally represented by different current helper names, while four candidate names are absent and therefore cannot be safely edited from the old ReSharper snapshot. No public API or contract member was deleted. Existing logical, proof, fraction, solver and Finance regression suites remain the required behavioral evidence.

This matrix closes only the **audit/test-first** layer. It does not authorize public removal, private deletion, or a new ReSharper batch. A future cleanup may begin only from a refreshed current XML snapshot and a new owner-approved decision record.
