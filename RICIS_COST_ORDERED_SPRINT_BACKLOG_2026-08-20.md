# RICIS cost-ordered Agile sprint backlog — 2026-08-20

**Iteration:** 1/5
**Priority rule:** от полностью контролируемых дешёвых исправлений к локальным code changes, затем к host/external integrations и только после этого к дорогостоящим research proofs.
**No silent deletion:** ни один public/contract member не удаляется без caller graph, direct regression, removal decision и approval.

## Ordered work packages

| Order | Package | Cost | External dependency | Acceptance criterion | Initial action |
|---:|---|---:|---|---|---|
| 0 | Status/evidence synchronization | XS | Нет | RSA/ULong/API/CLI/Universal-Inumber/Composite docs have current status, current gate counts and links to evidence; no `Proposed` remains on a completed increment unless explicitly marked historical. | Update MD only; no code change. |
| 0.5 | Silent logical API removal incident closure | XS–S | Explicit owner acceptance | Corrective façade, direct tests, policy and full gate are present; incident status changes from release-blocking only after the owner explicitly accepts the remediation. | Keep incident open; prepare acceptance evidence, do not self-approve. |
| 1 | ReSharper Batch A audit/test-first | S–M | Owner approval for removal | A-01…A-07 each has caller/reflection/serialization result and direct test before any eligible private cleanup; public/contract items remain preserved. | Prepare decision matrix and tests, do not delete automatically. |
| 2 | Finance host-boundary preparation | M | Host architecture choice | FIN-02/03/04 interface and DTO gaps are mapped; production verifier, persistence and checkout work are separated from test stubs; no fake provider adapter is added. | Produce prerequisite checklist. |
| 3 | FIN-02 provider webhook | L | Official callback spec, credentials, endpoint | Only valid signature/status/amount/currency/reference creates payment fact; malformed/duplicate callbacks fail closed or are idempotent. | Block until external prerequisites arrive. |
| 4 | FIN-03/04/05/10/11 production path | XL | DB, provider sandbox, host UI and operations decisions | Persistence/outbox, secure checkout, sandbox contract, observability and readiness evidence pass route-specific gates. | Do not start production code before prerequisites. |
| 5 | Generic Fermat `INumber<T>` | XL | Capability architecture and cross-type QA | No BigInteger fallback; exact root/bit-length/residue capability contract; Int2048/ULong2048 direct evidence; no O(1) claim without BFR-E01…E08. | Keep as separate research increment. |
| 6 | Full subject-matter Lean root-to-leaf proof | XXL | Typed definitions and semantic bridges for every external domain | Each named node has a genuine proposition, hypotheses, local theorem and semantic edge bridge; route detector reports `ProvedSubjectMatter`, not `SimulatedRoute`. | Keep open; engine invariant is not sufficient. |

## First package definition of done

The current sprint begins with package 0. It is complete only when each corrected Markdown status names one of: `Implemented`, `KernelChecked`, `Tested`, `Deferred`, `Blocked`, `Proposed` or `Historical`. Every `Implemented`/`KernelChecked` status links to its actual source/test/manifest evidence. Historical counts are labelled with a date; stale counts are not silently replaced by current counts without provenance.

The package must not modify production C# or Lean semantics. It may add or update audit/status documents and must pass `git diff --check`, project verification, generated adapter check and Lean artifact verification. A separate QA pass must search again for stale status contradictions.

## Agile rule after package 0

A package cannot be marked done because its interface exists or its document compiles. The next package is entered only after the previous package’s direct acceptance criteria and quality gate pass. External blockers are recorded as blockers, not simulated with test stubs or placeholder adapters.

## Scoring ledger

| Role | Current iteration score |
|---|---:|
| Analyst | +100 provisional for unambiguous cost ordering and acceptance criteria |
| Developer | 0 until implementation package is accepted |
| Adversarial QA | +20 per unique confirmed defect; no critical E2E defect may pass silently |
