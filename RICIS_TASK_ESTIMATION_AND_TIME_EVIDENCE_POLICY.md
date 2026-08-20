# RICIS task estimation and time evidence policy

**Status:** Approved project process rule, effective 2026-08-20.
**Purpose:** calibrate future task estimates from measured evidence and prevent an incomplete or placeholder result from being reported as finished.

## Core rule

Every new task receives an estimate before implementation and a time-evidence record at completion. The record is stored in a project Markdown file or in the sprint report. A task is `DONE` only when its acceptance criteria, tests, quality gates and evidence are complete. `sorry`, `admit`, `TODO` placeholders in the delivered path, skipped mandatory tests and unverified claims are not completion evidence.

> A fixed time box defines the maximum scope to be completed, not permission to lower correctness standards. If the scope cannot be completed within the box, split it, stop at a truthful `Blocked/Partial` boundary and record the remaining work.

## Complexity scale

| Class | Nominal effort | Typical scope | Default risk |
|---|---:|---|---|
| `XS` | 0.25–0.5 h | One document/status correction or deterministic inspection | Low |
| `S` | 0.5–1 h | One isolated test, small policy/template update or narrow local fix | Low–medium |
| `M` | 1–2 h | One bounded public contract, several tests or a small self-contained refactor | Medium |
| `L` | 2–4 h | Multi-file implementation with architecture and QA iteration | Medium–high |
| `XL` | 4–8 h | Cross-project feature, external boundary preparation or capability design | High |
| `XXL` | 8+ h / multi-sprint | New formal domain, external integration, research proof or broad migration | Very high |

The class is selected from the complete acceptance scope, not from the first coding action. A task with an external prerequisite is at least `L` even when its local stub is small.

## Mandatory estimate record

Before starting, every sprint item records:

| Field | Meaning |
|---|---|
| `TaskId` | Stable task/sprint identifier. |
| `RequestedScope` | Exact deliverable, exclusions and definition of done. |
| `ComplexityClass` | `XS` through `XXL`. |
| `PlannedHours` | Numeric estimate before implementation. |
| `DependencySet` | Code, project, external API, credentials, owner approval or research dependencies. |
| `DependencyRisk` | `None`, `Low`, `Medium`, `High` or `Blocked`. |
| `PlannedPhases` | Analysis, implementation, QA, gate, commit/notification. |
| `StartUtc` / `EndUtc` | Measured execution interval. |
| `ActiveHours` | Active work time, excluding waiting for user/external services. |
| `WaitingHours` | Blocked or queued time, recorded separately. |
| `IterationCount` | Number of developer↔QA corrections. |
| `FinalStatus` | `Done`, `Blocked`, `Partial`, `Deferred` or `Rejected`. |
| `Evidence` | Test IDs, build output, Lean artifact, commit and report paths. |
| `Variance` | `ActiveHours − PlannedHours` and percentage variance. |

## Complexity-to-time calibration

The project maintains this table and appends one row per completed task. Values must come from recorded evidence; no historical duration is invented retroactively.

| Task/sprint | Complexity | Planned h | Active h | Waiting h | Iterations | Status | Variance | Evidence |
|---|---:|---:|---:|---:|---:|---|---:|---|
| First measured task after policy adoption | — | — | — | — | — | Pending measurement | — | This policy |

After at least five measured tasks, estimates are recalibrated by complexity class using the median active hours and the 80th percentile. External waiting time is never mixed into coding complexity; it remains a separate dependency metric.

## Fixed time-box rule

For a requested two-hour sprint, the Analyst selects only a scope whose **P80 active estimate is at most two hours**, normally `XS`, `S` or a tightly bounded `M`. The scope must include implementation, direct tests and the applicable quality gate. Commit/notification time is included in the box unless explicitly excluded before start.

A two-hour request cannot honestly include a new external payment integration, a new formal subject-matter proof, a cross-project generic numeric capability or an unbounded refactor. Those are `L`–`XXL` or blocked by dependencies and must be split into a two-hour preparation increment with a truthful boundary.

| Two-hour request | Valid two-hour scope | Invalid scope for the same box |
|---|---|---|
| Finance | Map FIN-02 callback prerequisites, define DTO/port acceptance tests and record blockers. | Implement and production-validate a provider webhook without official spec/credentials/endpoint. |
| Lean | Audit one existing artifact, add one concrete local lemma and kernel-check it. | Formalize ten external subject domains from prose with no typed propositions. |
| Numerics | Add one bounded filter or one direct public API test with full gate. | Build a generic Fermat solver, new root capability and cross-bit-length proof corpus. |
| ReSharper | Audit one private candidate family and add behavior tests before a decision. | Remove an entire public/serialization/payment surface based only on IDE warnings. |
| Documentation | Synchronize statuses, counts, evidence links and a QA report. | Rewrite the entire project specification corpus and infer unrecorded historical durations. |

## Stop and completion conditions

At the time box boundary, the agent performs a stop check. If all acceptance criteria and gates pass, status is `Done`. If only a bounded subset passes, status is `Partial` with a remaining-work record. If an external prerequisite is missing, status is `Blocked` and the prerequisite is named. No status is upgraded to `Done` because the planned time expired.

## QA enforcement

QA checks that the estimate existed before coding, actual times are recorded, dependencies are listed, every public method has direct tests, no mandatory artifact contains a placeholder proof, and the final report does not claim more than the evidence proves. A missing time record is a process defect; a false `Done` claim is a release-blocking defect.
