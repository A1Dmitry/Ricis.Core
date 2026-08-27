# Task time evidence template

Copy this template into the task/sprint report before implementation starts. Do not fill planned values retrospectively.

## Estimate before start

| Field | Value |
|---|---|
| TaskId | `TASK-YYYY-MM-DD-NNN` |
| RequestedScope | `<one precise sentence>` |
| ComplexityClass | `XS / S / M / L / XL / XXL` |
| PlannedHours | `<decimal hours>` |
| DependencySet | `<code / approval / external API / credentials / none>` |
| DependencyRisk | `None / Low / Medium / High / Blocked` |
| DefinitionOfDone | `<tests, build, artifact, commit and notification criteria>` |
| PlannedPhases | `Analysis → Implementation → QA → Gate → Commit` |
| EstimateBasis | `<historical TaskId/class median or explicit first estimate>` |

## Measured execution

| Phase | Start UTC | End UTC | Active h | Waiting h | Notes/evidence |
|---|---|---|---:|---:|---|
| Analysis | `<timestamp>` | `<timestamp>` | `<h>` | `<h>` | `<requirements/decision file>` |
| Implementation | `<timestamp>` | `<timestamp>` | `<h>` | `<h>` | `<source/commit>` |
| QA | `<timestamp>` | `<timestamp>` | `<h>` | `<h>` | `<test IDs and failures>` |
| Quality gate | `<timestamp>` | `<timestamp>` | `<h>` | `<h>` | `<build/Lean/regression output>` |
| Commit/notification | `<timestamp>` | `<timestamp>` | `<h>` | `<h>` | `<commit/Telegram result>` |
| **Total** |  |  | **`<sum>`** | **`<sum>`** |  |

## Completion verdict

| Field | Value |
|---|---|
| IterationCount | `<number of Developer↔QA corrections>` |
| FinalStatus | `Done / Blocked / Partial / Deferred / Rejected` |
| ActualHours | `<active hours>` |
| VarianceHours | `<ActualHours - PlannedHours>` |
| VariancePercent | `<100 × variance / planned>` |
| NoSorryCheck | `PASS / FAIL` |
| DirectTestCheck | `PASS / FAIL / N/A with reason` |
| EvidenceLinks | `<relative paths>` |
| FollowUp | `<next task or None>` |

## Historical calibration row

Append one row to the project calibration table only after the final status and evidence are complete.

| TaskId | Complexity | Planned h | Active h | Waiting h | Iterations | Final status | Variance % | Primary dependency | Evidence |
|---|---:|---:|---:|---:|---:|---|---:|---|---|
| `<TASK-ID>` | `<class>` | `<h>` | `<h>` | `<h>` | `<n>` | `<status>` | `<%>` | `<dependency>` | `<report path>` |

## QA rule

A missing timestamp, missing gate output or missing variance is a process defect. A `Done` row with a failed `NoSorryCheck`, failed direct test or unverified mandatory artifact is invalid and must be reclassified as `Partial` or `Blocked`.
