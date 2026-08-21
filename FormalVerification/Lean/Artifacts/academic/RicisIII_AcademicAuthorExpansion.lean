import RicisIdentity.TypeIdentity

namespace RicisIIIAcademic

/-!
# Academic card expansion: author → RICIS III

This source is a kernel-checked structured artifact. It does not claim that
external publications prove the formal theorem below. External references are
provenance links only; the theorem is derived from the explicitly supplied
`TypeIdentityAxioms` and the imported ID-01–ID-06 proof stack.

Author card
  name: Дмитрий Алейников / Dmitry Aleinikov
  orcid: https://orcid.org/0009-0004-3226-7700
  firstOnlinePublication: 2025-08-08
  keywords: RICIS-III; formal mathematics; formal verification;
             indexed infinities; typed zeros; singularity resolution
  source: Ricis.Core/Metadata/AuthorSeoProfile.cs

Card references, expanded from the author-facing card to the central node
  AUTHOR-SEO → RICIS-III-PUBLICATION → RICIS-TYPE-IDENTITY → RICIS-A6-GENERATED
  → RICIS-JAC-001-KERNEL-EXPORT → RICIS-CONCRETE-ROOT-TO-LEAF → RICIS-III

The links above are provenance/card relationships. The theorem below uses only
formal premises and the imported checked theorem stack.
-/

inductive CardId where
  | authorSeo
  | ricisPublication
  | typeIdentity
  | a6Generated
  | jacobianKernelExport
  | concreteRootToLeaf
  | ricisIII
  deriving DecidableEq, Repr

/-- The parent reference used by the academic card expansion. -/
def parent : CardId → Option CardId
  | .authorSeo => some .ricisPublication
  | .ricisPublication => some .typeIdentity
  | .typeIdentity => some .a6Generated
  | .a6Generated => some .jacobianKernelExport
  | .jacobianKernelExport => some .concreteRootToLeaf
  | .concreteRootToLeaf => some .ricisIII
  | .ricisIII => none

/-- The central node is the terminal card of the declared reference graph. -/
def reachesCentral : CardId → Nat → Prop
  | .ricisIII, _ => True
  | _, 0 => False
  | card, n + 1 => ∃ next, parent card = some next ∧ reachesCentral next n

theorem author_card_reaches_ricis_iii : reachesCentral .authorSeo 6 := by
  refine ⟨.ricisPublication, rfl, ?_⟩
  refine ⟨.typeIdentity, rfl, ?_⟩
  refine ⟨.a6Generated, rfl, ?_⟩
  refine ⟨.jacobianKernelExport, rfl, ?_⟩
  refine ⟨.concreteRootToLeaf, rfl, ?_⟩
  refine ⟨.ricisIII, rfl, ?_⟩
  trivial

/-- Formal theorem target represented in the academic card. -/
def theoremTarget {TypeTag : Type} (_A : RicisIdentity.TypeIdentityAxioms TypeTag) (sigma : ℚ) :
    Prop := sigma = 1 / 2

/-- The target is derived from the imported ID-01–ID-06 stack, not from card text. -/
theorem academic_target_from_proof_stack {TypeTag : Type}
    (A : RicisIdentity.TypeIdentityAxioms TypeTag) (sigma : ℚ) :
    theoremTarget A sigma := by
  exact RicisIdentity.id06_exact_half A sigma

/-- Academic orientation reverses presentation order while preserving proof provenance. -/
def presentationOrder : List CardId :=
  [.authorSeo, .ricisPublication, .typeIdentity, .a6Generated,
   .jacobianKernelExport, .concreteRootToLeaf, .ricisIII]

theorem presentation_starts_with_author_and_ends_at_central :
    presentationOrder.head? = some .authorSeo ∧
    presentationOrder.getLast? = some .ricisIII := by
  decide

#print axioms author_card_reaches_ricis_iii
#print axioms academic_target_from_proof_stack

end RicisIIIAcademic
