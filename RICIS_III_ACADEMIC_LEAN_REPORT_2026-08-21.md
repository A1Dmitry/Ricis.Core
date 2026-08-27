# RICIS III: авторская карточка и формальная цепочка доказательства

**Document type:** academic provenance and formal-verification report  
**Status:** `KernelChecked` for the declared Lean theorem stack; provenance graph is documentary metadata  
**Date generated:** 2026-08-21  
**Author:** Дмитрий Алейников / Dmitry Aleinikov  
**ORCID:** [0009-0004-3226-7700](https://orcid.org/0009-0004-3226-7700)  
**First online publication:** 2025-08-08  
**Generated artifact:** [`RicisIII_AcademicAuthorExpansion.lean`](FormalVerification/Lean/Artifacts/academic/RicisIII_AcademicAuthorExpansion.lean)

## Abstract

Документ представляет авторо-ориентированное раскрытие карточки RICIS III через зафиксированный граф provenance. Карточка автора ссылается на публикационную карточку, далее — на типовую идентичность RICIS, структурный A6-артефакт, kernel export для JAC-001 и конкретный root-to-leaf route, завершаясь центральным узлом RICIS III.

Формальная часть отчёта не считает внешние публикации доказательствами сама по себе. Kernel-checked theorem target выводится исключительно из явно переданной структуры `TypeIdentityAxioms` и импортированной цепочки ID-01–ID-06. Это разделяет академическое представление, provenance и машинно проверяемое математическое утверждение.

## Author and SEO metadata

| Field | Value |
|---|---|
| Name | Дмитрий Алейников |
| Alternate name | Dmitry Aleinikov |
| ORCID | https://orcid.org/0009-0004-3226-7700 |
| First online publication | 2025-08-08 |
| Keywords | RICIS-III; formal mathematics; formal verification; indexed infinities; typed zeros; singularity resolution; algebraic geometry; computational analysis |
| Metadata source | [`AuthorSeoProfile.cs`](Metadata/AuthorSeoProfile.cs) |

The publication links are inherited from the source author card and are not reinterpreted as kernel premises. They are listed as provenance references in the final section.

## Card graph and academic orientation

Presentation order is intentionally author-facing:

```text
AUTHOR-SEO
  → RICIS-III-PUBLICATION
  → RICIS-TYPE-IDENTITY
  → RICIS-A6-GENERATED
  → RICIS-JAC-001-KERNEL-EXPORT
  → RICIS-CONCRETE-ROOT-TO-LEAF
  → RICIS-III
```

The Lean artifact proves that the finite declared graph reaches the terminal central node. The graph theorem does not prove the scientific content of external publications; it verifies the integrity of the declared card references.

## Formal claim and proof stack

Let `A : TypeIdentityAxioms TypeTag` and let `sigma : ℚ`. The formal target is:

> `sigma = 1 / 2`.

The target is obtained through the following imported stack:

| Step | Lean theorem | Role |
|---:|---|---|
| ID-01 | `id01_type_preserved` | Reflection preserves the identity type |
| ID-02 | `id02_reflection_sum` | Reflected coordinates sum to one |
| ID-03 | `id03_same_coordinate` | Faithful type equality identifies the coordinate |
| ID-04 | `id04_linear_pair` | Builds the exact linear pair |
| ID-05 | `id05_doubled_coordinate` | Eliminates the reflected coordinate: `2*sigma=1` |
| ID-06 | `id06_exact_half` | Derives the exact rational result `sigma=1/2` |

The wrapper theorem `academic_target_from_proof_stack` invokes `RicisIdentity.id06_exact_half` directly. Thus the academic artifact is not a scaffold containing unverified prose in place of a proof.

## Evidence boundary

The artifact is `KernelChecked` because the saved Lean source compiles with Lean 4.33.0 and Mathlib without `sorry`, `sorryAx`, `admit` or an unregistered axiom marker. The author metadata and card graph are structured provenance information. They do not become assumptions of the mathematical theorem.

The C# simplification and computation stack remains a separate evidence source. Its trace explains how an expression is transformed, while the Lean source checks only the structured theorem encoded in the artifact. A generic C# expression tree is not silently promoted to a Lean theorem.

## Reproducibility

```bash
export PATH="$HOME/.elan/bin:$PATH"
cd FormalVerification/Lean
LEAN_PATH=. lake env lean Artifacts/academic/RicisIII_AcademicAuthorExpansion.lean
```

The output includes `#print axioms` for the graph theorem and proof-stack theorem. The output must contain no `sorryAx` and the source must remain registered in [`manifest.json`](FormalVerification/Lean/Artifacts/manifest.json) with `knowledgeSource.mandatoryForModelStudy=true`.

## Provenance references

[1]: [`AuthorSeoProfile.cs`](Metadata/AuthorSeoProfile.cs) — source of author name, alternate name, ORCID, publication date, keywords and work links.  
[2]: [`TypeIdentity.lean`](FormalVerification/Lean/RicisIdentity/TypeIdentity.lean) — kernel theorem source for ID-01–ID-06.  
[3]: [`manifest.json`](FormalVerification/Lean/Artifacts/manifest.json) — authoritative Lean artifact registry and knowledge-source policy.  
[4]: [`LEAN_ARTIFACT_POLICY.md`](LEAN_ARTIFACT_POLICY.md) — status, provenance and trust-boundary rules.  
[5]: [`RicisJacobianProofScenario.cs`](Proofs/RicisJacobianProofScenario.cs) — source scenario for JAC-001 checked export and proof stack.  
[6]: [`RicisIII_AcademicAuthorExpansion.lean`](FormalVerification/Lean/Artifacts/academic/RicisIII_AcademicAuthorExpansion.lean) — generated academic card expansion and checked wrapper theorem.  
[7]: `ACL01–ACL03` in `RegressionTests/RicisAcademicLeanCardSuite.cs` — QA coverage for metadata, graph order and Lean artifact content.

## Conclusion

The artifact provides an auditable academic presentation from the author card to the RICIS III central node and separately provides a kernel-checked formal target derived from the existing ID-01–ID-06 stack. It does not claim that the provenance links, author metadata or card graph alone establish the theorem. This distinction is required for a reproducible academic report.
