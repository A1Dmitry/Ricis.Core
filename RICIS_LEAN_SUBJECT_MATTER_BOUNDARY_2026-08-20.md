# Lean subject-matter root-to-leaf boundary — 2026-08-20

**Status:** Deferred — engine and route artifacts are kernel-checked; subject-matter semantic bridges remain open.
**Evidence:** [`ROUTE_PROOF_ADVERSARIAL_QA.md`](ROUTE_PROOF_ADVERSARIAL_QA.md), [`RICIS_MD_ADVERSARIAL_AUDIT_REPORT.md`](RICIS_MD_ADVERSARIAL_AUDIT_REPORT.md), [`RICIS3_LONGEST_ROUTE_LEAN_AUDIT.md`](RICIS3_LONGEST_ROUTE_LEAN_AUDIT.md), [`RegressionTests/RicisConcreteRouteLeanSuite.cs`](RegressionTests/RicisConcreteRouteLeanSuite.cs), and [`RegressionTests/RicisLongestRouteLeanSuite.cs`](RegressionTests/RicisLongestRouteLeanSuite.cs).
**Current verified artifact gate:** 8/8 Lean artifacts compile/kernel-check under the existing route/engine contract.

## Proof classification

| Layer | Current result | What is genuinely established | What is not established |
|---|---|---|---|
| Engine invariant | `KernelChecked` | Concrete rank-one engine invariant and local algebraic obligations | No external-domain theorem |
| Route composition | `KernelChecked` | Ten-node route composition and checked artifact chain | No semantic meaning for an external node without a bridge |
| Concrete route regression | `Tested` | C# route/evidence integration and artifact presence | Test execution is not a subject-matter proof |
| Subject-matter nodes | `Deferred` | Names and dependency positions can be catalogued | Hodge, Poincare and other named domains lack genuine typed propositions/bridges here |

## Root-to-leaf open-node contract

| Open node class | Required genuine proposition | Required hypotheses | Required semantic bridge | Current status |
|---|---|---|---|---|
| Geometric/topological theorem | Typed theorem in the intended domain | Explicit manifold/space/regularity assumptions | Map RICIS representation to the theorem's objects | `Deferred` |
| Analytic/singularity theorem | Typed analytic proposition | Domain, continuity/limit/differentiability assumptions | Prove representation preserves the analytic predicates | `Deferred` |
| Algebraic reconstruction | Typed equality/structure theorem | Algebraic structure and non-degeneracy assumptions | Connect engine expression to the mathematical object | `Deferred` |
| External named result | Imported theorem or local formalization | Exact theorem statement and all library hypotheses | Verify each route edge transports the hypotheses | `Deferred` |

## Adversarial acceptance rule

A compiled Lean file or passing route test may be classified only as `KernelChecked` or `Tested` for the artifact/invariant it actually contains. It may not be promoted to `ProvedSubjectMatter` without a typed proposition, hypotheses, local theorem proof and semantic edge bridge. This document introduces no `sorry`, no placeholder theorem and no simulated completion.
