# Adversarial MD/Proof Audit — Analytical Specification

**Iteration:** 1/5
**Role:** Analyst / Prompt-Architect
**Scope:** all tracked `*.md` files in the Ricis.Core repository, plus the source/tests/artifact files referenced by those documents.

## Objective

Determine whether the requested root-to-leaf proof work was actually completed or whether the implementation only simulated a proof route by composing unconstrained certificates, labels or payload-preservation lemmas.

## Corpus boundary

The normative corpus is every tracked project Markdown file returned by `git ls-files '*.md'`. Vendor documentation under `FormalVerification/Lean/.lake` is excluded from project requirements but may be used only as Lean toolchain documentation. The audit must include the current Lean manifest, all route-specific Lean sources, the concrete C# proof scenario, regression suites, and the map JSON where route claims originate.

## Required classification

Every relevant claim must receive exactly one status:

| Status | Meaning |
|---|---|
| `ProvedSubjectMatter` | The actual domain proposition is defined and its theorem is kernel-checked from explicit hypotheses/definitions, without unconstrained certificate fields. |
| `ProvedEngineInvariant` | The RICIS engine’s concrete structural invariant is kernel-checked, but this does not prove the external domain theorem named by a node. |
| `ConditionalTheorem` | The conclusion follows from explicit premises supplied as inputs, but those premises are not established by the artifact. |
| `TestedRuntime` | C# behavior or artifact contract is covered by direct regression tests; no mathematical theorem is implied. |
| `AuditOnly` | Documentation, trace, status or generated text is inspected, but no kernel theorem proves the claim. |
| `SimulatedRoute` | Node labels or generic payloads are carried through a graph while node semantics and domain transitions are not proved. |
| `Open` | Required expression, definition, hypothesis or bridge is absent. |

## Adversarial acceptance criteria

1. A route node must have a concrete proposition, typed definitions, explicit hypotheses and a local theorem.
2. An edge must prove a semantic transition, not merely rename a node field.
3. A terminal theorem must not accept the local/edge certificates as unconstrained structure fields when the requested result is full root-to-leaf proof.
4. Successful `lake env lean` compilation proves only the theorem actually stated in the source; it does not validate comments, labels or external theorem names.
5. C# regression PASS proves the tested contract, not the truth of an external mathematical proposition.
6. A map status such as `resolved` is operational metadata and cannot override missing subject-matter definitions.
7. The final report must identify every discovered overclaim and every remaining blocker.

## Scoring

The project score starts at `0`. The analyst receives `+100` only if the final implementation satisfies this unambiguous specification on the first implementation iteration. The developer receives `+50` only after QA acceptance. QA receives `+20` per unique confirmed defect and `-200` if an end-to-end validator would detect a defect after final acceptance. The score is a project audit ledger, not a substitute for evidence.

## Expected likely boundary

A concrete rank-one RICIS payload may prove a RICIS engine invariant from a root label to a leaf label. That is not automatically a subject-matter proof of Hodge, Poincaré, Atiyah–Singer, knot theory or spectral asymptotics. The audit must explicitly test this distinction instead of accepting a green status or theorem name as proof.
