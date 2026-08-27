# RICIS stalled-task audit — 2026-08-20

**Iteration:** 1/5
**Role:** Analyst → Developer → QA
**Scope:** tracked project Markdown corpus, referenced C#/Lean sources, regression catalogs, Lean manifest, Finance backlog and Git state.

## Executive result

The repository has no uncommitted code changes after the latest detector commit, and the Release/project/Lean gates are green for implemented work. The project is not globally stuck. It has a small number of genuine blockers, a larger planned backlog, several intentionally deferred increments and several stale status lines that make completed work look open.

The most important distinction is that a **blocker** requires an external decision, contract, credential or host choice; an **open backlog item** is implementable after its prerequisites; a **deferred item** is deliberately postponed; and a **stale document** is an evidence/status synchronization defect rather than unfinished code.

## Classification table

| Priority | ID / area | Classification | Evidence-backed status | Dependency or unblock condition |
|---:|---|---|---|---|
| P0 | `FIN-02` bePaid inbound confirmation | **Blocker + open implementation** | Only application port and test stub are present; no production provider-specific verifier was found. | Official callback specification, merchant/test credentials and host webhook endpoint. |
| P0 | `FIN-03` persistence/outbox/reconciliation | **Open backlog** | Repository/application ports and services exist; concrete host persistence/outbox/reconciliation remains absent. | Host database and transactional-outbox decision. |
| P0 | `FIN-04` secure host checkout | **Open backlog** | `PaymentHandoff` DTOs and allow-list validation exist; completed host endpoint/UI flow is absent in inspected production paths. | FIN-01/FIN-02 plus host UI/API implementation. |
| P0 | `FIN-05` provider sandbox contract suite | **Blocker** | Requires provider sandbox access and test credentials; production money must not be used. | FIN-02, FIN-04 and provider sandbox. |
| P0 | `FIN-10` security/observability | **Open backlog** | Secret/observability/incident-drill acceptance remains in Finance backlog. | FIN-02 and FIN-03. |
| P0 | `FIN-11` production readiness review | **Downstream blocker** | Cannot close before route-specific FIN-02…FIN-10 evidence and owner approval. | Completion of selected payment route and explicit production switch approval. |
| P1 | `FIN-06` official NBRB FX adapter | **Open backlog** | Not implemented as a completed official-rate snapshot route. | FIN-03 and official API contract evidence. |
| P1 | `FIN-07` versioned tax policy/work queue | **Open backlog + domain-process blocker** | Policy ports exist; approved business process and effective-dated implementation remain open. | FIN-03 and confirmed business process. |
| P1 | `FIN-08` payout/bank fee routes | **Blocker + open backlog** | Requires one written provider/bank route, contract, credentials and effective fee schedule. | Route-specific official contract. |
| P1 | `FIN-09` refund lifecycle | **Open backlog** | Not completed in the inspected Finance production surface. | FIN-02 and selected provider route. |
| P2 | `FIN-12` CIS expansion | **Deferred/open backlog** | Must select one country and one official rail; universal CIS fallback is forbidden. | FIN-05/FIN-11 and new route evidence. |
| P1 | `BFR-E01…E08` bounded Fermat research | **Open research contract** | Document remains proposed/awaiting approval; H-O1/O(1) deliberately unproved. | Explicit approval, fixture corpus, soundness QA, complexity accounting and cross-bit-length evidence. |
| P1 | Generic Fermat for `INumber<T>` | **Open architecture increment** | Current N-only Fermat solver is BigInteger-only; ULong2048 probe correctly fails at that boundary. | Exact generic root/bit-length/residue capability contract without BigInteger fallback. |
| P2 | Composite Fermat visual renderer | **Deferred** | Core bounded path is approved/implemented; business spec explicitly defers visual rendering. | Separate presentation sprint if requested. |
| P1 | Full subject-matter root-to-leaf Lean proof | **Open research blocker** | Current artifacts prove engine invariant or conditional composition; external node propositions/bridges are absent. | Typed definitions and semantic theorem/bridge for every named domain node. |
| P1 | ReSharper remediation Batch A `A-01…A-07` | **Open governance backlog** | Plan is proposal for approval; no code removal performed. | Exact caller/reflection/serialization audit, direct tests and owner approval. |
| P1 | ReSharper decision nodes `B-01…B-03` | **Decision blocker** | Public/write-only/compatibility surfaces must not be deleted mechanically. | Product decision: preserve, expose snapshot, obsolete or major-version removal. |
| P2 | JSON/provider DTO preservation `C-01…C-03` | **Open QA backlog / preserve** | Contracts are intentionally retained; schema/serialization evidence is still a planned action. | Contract tests and source-generator gate; no deletion. |
| P2 | Universal INumber reduction proposal | **Open approval/status item** | Architecture/QA documents retain proposed or in-progress wording; implementation evidence exists for some generic paths but not the whole proposal. | Explicit scope approval and completion matrix. |

## Status-drift and stale-document items

These are not necessarily code blockers, but they can mislead project planning and must be corrected.

| Document | Current text | Crosschecked evidence | Correct action |
|---|---|---|---|
| `SPRINT_RSA_PSS_STEP1–3` | `Proposed — awaiting explicit user approval` | Current context reports RSA-PSS implementation and 63/63 Numerics tests. | Mark implemented only after reconciling every QA criterion, or keep proposed and label implementation as an unauthorized drift. |
| `SPRINT_ULONG2048_SHIFT_ROOT_STEP2–3` | `Proposed — awaiting approval` | ROOT/FROOT direct tests and fixed-width root evidence exist; performance evidence says slower than Newton. | Refresh status and attach measured evidence; do not claim speed improvement. |
| `PUBLIC_API_COVERAGE_SPRINT.md` | `completed locally; ready for Git commit`, historical 370/370 and 6 Lean artifacts | Current adapter/gates are newer: 386 Core regression and 8 Lean artifacts. | Replace historical counts with links to current gate or mark historical snapshot. |
| `PUBLIC_API_CLI_AUDIT.md` | `аудит начат` | Same document records all commands `EXIT=0` and `--public-api-demo` smoke coverage. | Mark completed or split remaining output-contract gaps from completed smoke audit. |
| `RESHARPER_BATCH_A/B_SPRINT.md` | `completed locally; ready for Git commit` | Must be checked against current Git history/status before treating as open. | Refresh commit/status link; do not create duplicate work. |
| `SPRINT_BOUNDED_FERMAT_RESEARCH_STEP1_HYPOTHESIS.md` | Proposed/awaiting approval | Correctly contains unproved H-O1 and requests QA-design approval. | Keep open; do not label as completed factorization or O(1). |

## Done and should not be reopened

The following increments are supported by current source/test evidence and should be treated as completed unless a new regression appears: Jacobian proof artifact and its explicit scope; RSA-PSS implementation as reported by current Numerics evidence; ULong2048 fixed-width shift root; universal scalar `INumber<T>` Core integration; immutable `Semiprime<T>` migration; approved bounded Composite Fermat path and its filter trace; concrete engine-level route theorem; and RSA route-simulation detector RSA01–RSA05. Their external-claim boundaries remain important, but those boundaries are not unfinished code.

NuGet publication is intentionally not a stalled task because the owner explicitly prohibited publication. Direct tax/MNS, EasyStaff, universal CIS bank selector and universal bank fee routes are blocked by missing official contracts and must not be implemented speculatively.

## Recommended next Agile order

The next sprint should not start with more symbolic or Lean labels. The highest-value executable path is `FIN-02` only after the owner supplies or approves the official callback specification, test credentials and host webhook endpoint. In parallel, the project can perform the status-refresh increment for RSA/ULong/API/CLI documents and approve the ReSharper Batch A test-first plan. The generic Fermat/INumber and full subject-matter Lean programs should remain separate research increments rather than being silently merged into a completed sprint.

## Acceptance gates for closing an item

An item may move to `DONE` only when its document has a current status, implementation and direct test evidence are linked, external prerequisites are recorded, the relevant quality gate is rerun, and no stronger claim is made than the artifact proves. A proposed or blocked item must retain its blocker/approval condition in the backlog.

## References

[1]: `Ricis.Finance/BACKLOG.md` — FIN-01 through FIN-17 and BLOCK-01 through BLOCK-04.
[2]: `SPRINT_BOUNDED_FERMAT_RESEARCH_STEP1_HYPOTHESIS.md` — BFR-2, H-O1 and BFR-E01–E08.
[3]: `SPRINT_COMPOSITE_FERMAT_PRUNING_STEP1_BUSINESS_SPEC.md` — approved bounded path and deferred visual renderer.
[4]: `INUMBER_SEMIPRIME_PATH_AUDIT_2026-08-20.md` — BigInteger boundary and future generic Fermat increment.
[5]: `UNUSED_CODE_DEPENDENCY_REMEDIATION_PLAN.md` — A/B/C remediation gates.
[6]: `RICIS_MD_ADVERSARIAL_AUDIT_REPORT.md` — proof-simulation boundary and subject-matter blocker.
