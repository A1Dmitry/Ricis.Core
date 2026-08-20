# Incident: silent removal of potential logical reduction API

**Date:** 2026-08-20  
**Status:** open — remediation in progress; release-blocking until user accepts the completed corrective result.  
**Severity:** process-critical / QA release-blocking.  
**Affected commits:** `6f2ea28` introduced the removal; this incident requires a separate corrective commit.

## What happened

During public API coverage work, `ExpressionSimplifierVisitor.VisitLogical` was reclassified as an internal zero-caller duplicate and removed. The conclusion was based on an in-repository caller search and overlap with `LogicalReductionVisitor`.

That was an invalid removal decision. The method represented a potential independently consumable logical-reduction façade. The ReSharper report was supplied to identify untested or unclear model surface, not to authorise silent deletion. No Removal Decision Record, product-owner confirmation or public façade decision existed.

## Direct impact

| Area | Impact |
|---|---|
| Logical API surface | A potential direct logical-reduction route was removed without an explicit owner decision. |
| QA process | QA accepted zero internal callers as sufficient evidence and did not execute a deletion-gate review. |
| User trust | The agreed rule — test/understand uncertain surface before cleanup — was violated. |
| Mathematical semantics | The authoritative `LogicalReductionVisitor` and `LOG01–LOG09` remained intact; no proof artifact was changed. This reduces but does not eliminate the API/process incident. |

## Root cause

The remediation process conflated three distinct facts:

1. an internal caller graph was empty;
2. a second implementation existed; and
3. a direct public regression was absent.

These facts establish that a contract needs investigation and test coverage. They do not prove that it is disposable. QA failed to require an owner decision and a Removal Decision Record.

## QA failure and mandatory QA penalty

The QA verdict for the removal is **annulled**. QA failed `QA-DEL-01`, `QA-DEL-02`, `QA-DEL-03` and `QA-DEL-04`:

| QA gate | Failure | Penalty/corrective action |
|---|---|---|
| `QA-DEL-01` | Diff contained a deletion but no Removal Decision Record review occurred. | Re-audit this deletion and every deletion/reclassification since the last approved cleanup baseline. |
| `QA-DEL-02` | The potential visitor/logical façade classification was not escalated for product ownership. | Restore a supported public façade and make its scope explicit. |
| `QA-DEL-03` | No direct façade behavior and safety tests existed before deletion. | Add direct positive, safety and impure/unsupported regression coverage. |
| `QA-DEL-04` | No explicit user approval or migration/SemVer decision existed. | Block all further deletion/refactoring approval in this workstream until user accepts remediation. |

The QA penalty is not cosmetic: QA cannot approve another deletion/refactoring batch until the restored API, incident tests, policy update and full Core/Finance/Lean gate are all complete and the user confirms the remediation result.

## Corrective remediation

1. Version the no-silent-removal rule and QA deletion gate in `PUBLIC_API_TEST_POLICY.md`.
2. Update `RESHARPER_INSPECTION_POLICY.md` so XML is explicitly a coverage/uncertainty map, not deletion authority.
3. Restore a safe public logical-reduction façade that delegates to the normative `LogicalReductionVisitor`; do not restore the unsafe duplicate implementation.
4. Add direct regression tests for identities, non-reduction of impure short-circuit expressions, and invalid/non-Boolean boundary behavior.
5. Run full Release build, Core regression, Finance regression, Lean artifact verification and `git diff --check`.
6. Commit only the remediation, notify the user, and await explicit acceptance before any further deletion work.

## Prevention evidence required for closure

This incident closes only when the corrective commit contains:

- the restored API and XML documentation;
- stable direct regression IDs for its contract;
- an updated policy with `QA-DEL-01` through `QA-DEL-05`;
- a completed quality-gate log; and
- the user-facing incident report.
