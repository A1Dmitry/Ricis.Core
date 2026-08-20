# Silent Logical API Removal — Acceptance Package

**TaskId:** `SILENT-API-REMOVAL-INCIDENT-ACCEPTANCE-2026-08-20-01`
**Status:** `Blocked — explicit owner acceptance required`
**Incident severity:** Release-blocking until owner acceptance
**Rule:** Remediation evidence is not owner approval. This document does not self-close the incident.

## Incident statement

A prior cleanup path removed or threatened to remove logical/publicly useful API surface without first completing caller-graph analysis, direct regression coverage and an explicit removal decision. The project rule now forbids silent deletion and requires direct tests for every new or changed public method.

## Remediation evidence

| Control | Evidence | Result |
|---|---|---|
| Public API direct-test policy | `PUBLIC_API_TEST_POLICY.md` and `PUBLIC_API_CLI_AUDIT.md` | Present |
| Public utility direct tests | API01–API08 and API26 in `RegressionTests/RicisPublicUtilitySuite.cs` / generated MSTest adapter | 386/386 regression gate includes them |
| ReSharper Batch A preservation audit | `RESHARPER_BATCH_A_DECISION_MATRIX_2026-08-20.md` | No deletion authorized; stale/missing candidates remain preserved or blocked |
| Full Core regression | Current quality gate evidence | 386/386 PASS |
| Finance regression | Current project evidence | 18/18 PASS |
| Lean artifact verification | Current project evidence | 8/8 PASS |

## Acceptance boundary

The corrective controls are present, but the incident cannot be marked `Accepted` or removed from the release-blocking list by the automation agent. The explicit owner must review the matrix, confirm that no required public/contract member was lost, and record acceptance in a subsequent owner-authored change.

Until that occurs, the truthful state is:

> **Remediated controls: present. Incident closure: blocked on owner acceptance.**

## Prohibited shortcuts

No public API was deleted in this increment. No fake provider, placeholder proof, reflection-based discovery, or unsupported historical claim is introduced. No owner approval is inferred from passing tests or from a successful Git commit.

## Next action

Owner reviews this package and either accepts the remediation with a dated decision or returns a named defect for a new Analyst–Developer–QA iteration.
