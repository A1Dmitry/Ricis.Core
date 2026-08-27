# Preserve Decision — checked proof / document-format route

**Decision ID:** `PRESERVE-2026-08-20-PROOF-DOCUMENT-FORMATS`  
**Status:** accepted for preservation; no deletion proposed  
**Scope:** `ProveDocumentsCheckedWithLog<T>`, `RicisCheckedProofArtifacts<T>`, typed proof log, JSON/LaTeX document factories and the generic Lean rejection boundary.

## Decision

The checked multi-format proof route is a **public compatibility and proof-audit contract**. It must be preserved. The old ReSharper condition finding in `RicisAcademicProofExtensions` cannot be used as an authority to remove, merge or make inaccessible any route in this graph.

No SemVer change is made. No API is removed, deprecated or reclassified by this decision.

## Current caller and contract graph

```text
public ProveDocumentsCheckedWithLog<T>
  -> DeriveUnaryProof(..., ILog<RicisProofOrchestrationStage>)
  -> CreateCheckedProofResult(...)
  -> AppendAcademicProtocol + AppendVerificationProtocol + AppendTypedProofLog
  -> ResolveDocumentConstructor(format)
      -> Academic protocol renderer
      -> RicisProofDocumentTemplates.ResolveFactory(format)
          -> Log / Json / LaTeX adapters
          -> Lean: controlled RicisUnsupportedLeanProofShapeException
  -> RicisCheckedProofArtifacts<T>
      -> immutable Proof + Trace + Documents
      -> public GetDocument(format)
```

The graph represents one structural derivation, then several presentational exports. It must not be replaced with independent re-derivations per document because that would break the single trace, create proof drift and violate DRY.

## Protected invariants

| ID | Invariant | Compatibility reason |
|---|---|---|
| PPDF-01 | Conditions and constraints remain expression trees and are not executed. | Proof APIs audit structure, not runtime hypotheses. |
| PPDF-02 | Expected expression is normalized structurally after a single claim derivation. | Verification is a structural contract, not an additional derivation. |
| PPDF-03 | Each distinct requested document format derives from one canonical node-to-root trace. | JSON and LaTeX must describe the same proof run. |
| PPDF-04 | Generic C# expression shapes requested as Lean are rejected with `RicisUnsupportedLeanProofShapeException`. | A report must never be presented as a Lean theorem without the structured Lean bridge. |
| PPDF-05 | `RicisCheckedProofArtifacts<T>` exposes immutable proof, trace and documents. | Consumers require reproducible audit material. |

## Direct regression evidence

| Test | Evidence |
|---|---|
| `CHECKED01–CHECKED05` | Structural expected-expression verification, parameter rebinding, non-execution of conditions and public checked document verification. |
| `PDF03` | Generic Lean export is a controlled rejection, never a generated unsupported theorem. |
| `PDF04–PDF05` | JSON and LaTeX retain the node-to-root derivation. |
| `PDF08` | Injected typed log records stage-aware node-to-root evidence. |
| `PDF09` | `ProveDocumentsCheckedWithLog` performs a verified proof, deduplicates requested formats and sends the same verification/node-to-root route to JSON and LaTeX. |

## Relationship to the old inspection finding

The source report is a coverage/uncertainty map. Its old `ConditionIsAlwaysTrueOrFalse` entry near a historical `RicisAcademicProofExtensions` offset is **not** a removal instruction. The current route has a format validation boundary, one controlled Lean rejection path and one canonical derivation-to-rendering flow. A new scan may identify a local simplification candidate only after it is mapped to the current source, classified as non-contractual and independently approved under `QA-DEL-01` through `QA-DEL-05`.

> **QA verdict:** preserve the route. Any later proposal to simplify, remove or restrict a branch requires a separate Removal Decision Record, current caller graph, direct branch tests, explicit user approval and SemVer decision.

## Next permitted work

The next batch may classify individual float-equality and nullable findings. It must keep production math/proof semantics and Finance provider boundaries separate, and it must never perform a global epsilon or null-check removal.
