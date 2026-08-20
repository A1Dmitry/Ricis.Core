# ReSharper Batch A decision matrix — 2026-08-20

**Iteration:** 1/5
**Status:** Tested / Deferred where the historical candidate cannot be mapped to current source.
**Result:** No safe deletion authorized in this increment. The caller graph identified false positives and already-remediated candidates.
**Evidence:** [`RegressionTests/RicisReSharperBatchASuite.cs`](RegressionTests/RicisReSharperBatchASuite.cs), [`PUBLIC_API_TEST_POLICY.md`](PUBLIC_API_TEST_POLICY.md), and [`RICIS_TASK_TIME_PRIORITY_SPRINT_2026-08-20.md`](RICIS_TASK_TIME_PRIORITY_SPRINT_2026-08-20.md).

The available repository checkout does not contain the original ReSharper XML snapshot. Therefore reflection/serialization results for absent historical symbols remain `Deferred`, not inferred.

| ID | Candidate from remediation plan | Current source evidence | Direct regression evidence | Decision | Reflection / serialization gate |
|---|---|---|---|---|---|
| A-01 | `ExpressionSimplifierVisitor._parameters` | Must be rechecked against current constructor/state; no deletion based only on IDE reachability. | `RSH01` typed lambda reduction | `Preserve` pending exact graph | `Deferred` until current XML/caller graph is supplied |
| A-02 | `ExpressionSimplifierVisitor.SimplifyFraction` | Current code calls `SimplifyFractionSum` and `SimplifyFractionProduct`; exact candidate name is absent, while fraction helper behavior is live. | `RSH02` positive rational Pow/root behavior | `Preserve`; stale/misidentified candidate | `Deferred` until refreshed XML mapping |
| A-03 | Private `ToBigInteger` | Candidate name is not present in current production source; remaining matches are intentional numeric/test helpers. | `RSH03` method-call traversal preserves method metadata | `Historical` / already remediated or stale | `Deferred`; no deletion from absence alone |
| A-04 | `SingularitySolver.IsTranscendentalComposite` | Symbol is absent from current source. | `RSH04` immutable engine snapshot contract | `Historical` / already remediated or stale | `Deferred`; refresh report baseline |
| A-05 | `RicisAcademicProofExtensions.solutionX/solutionY` | Values are used for finite checks, proof expressions, substitutions and rendered proof text. | `RSH05` multivariate exact subtraction contract | `Preserve` live semantic state | `Deferred` until current caller/serialization graph is supplied |
| A-06 | `ProviderPayment.NormalizeCurrency.parameterName` | Current helper signature is `NormalizeCurrency(string value)` and callers use it for both source/target currencies; candidate parameter is absent. | No honest direct mapping in `RSH01`–`RSH05` | `Historical` / already remediated or stale | `Deferred`; retain currency regression and refresh XML |
| A-07 | `SingularitySolver.TryGetPositiveConstant(..., out value)` | Symbol is absent from current source. | No honest direct mapping in `RSH01`–`RSH05` | `Historical` / already remediated or stale | `Deferred`; do not infer deletion from absence alone |

## QA conclusion

The Batch A source graph prevents a false cleanup: three candidates are live or structurally represented by different current helper names, while four candidate names are absent and therefore cannot be safely edited from the old ReSharper snapshot. No public API or contract member was deleted. Existing logical, proof, fraction, solver and Finance regression suites remain the required behavioral evidence.

## Remainder sprint review — 2026-08-20

The matrix was re-reviewed as the first unresolved package after the owner-acceptance blocker. The current checkout still does not contain the original ReSharper XML snapshot, and no owner approval for deletion is present. Therefore A-01…A-07 remain an audit/test-first result: live candidates are preserved, stale or absent candidates are Deferred/Historical, and no cleanup is authorized. This review is not owner approval and does not close the package.

## Next action

Supply the current XML/caller/reflection/serialization evidence and record owner approval before any eligible private cleanup; otherwise continue to the next priority package without deleting or silently downgrading API surface.

This matrix closes only the **audit/test-first** layer. It does not authorize public removal, private deletion, or a new ReSharper batch. A future cleanup may begin only from a refreshed current XML snapshot and a new owner-approved decision record.
