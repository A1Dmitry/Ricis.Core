# Рекурсивная модель доказательного LaTeX-документа RICIS

**Status:** `Implemented design contract`  
**Source exemplar:** `Knowledge/LaTexExamples/NavierStokes-Ricis.structural-exemplar.tex` (user-supplied source, 2026-08-21; SHA-256 recorded beside the source)  
**Purpose:** model and template contract for semantic LaTeX reports; mandatory project context for any embedded reporting agent.

## Scope and evidence boundary

The supplied Navier–Stokes document is treated as a **structural exemplar**, not as independent confirmation of its external scientific conclusion. The report model preserves a strict distinction among source claims, RICIS-internal transformations, generated evidence, and kernel-checked theorems. A template must never promote a narrative conclusion or an input assertion into a verified external-domain theorem.

> A report may describe a declared claim and the proof obligations required for it. It may mark a claim `Deferred`, `Conditional`, `RegressionChecked`, or `KernelChecked` only when the attached evidence supports the corresponding status.

## Recursive decomposition extracted from the exemplar

| Node ID | Parent | Block kind | Role | Evidence boundary |
|---|---|---|---|---|
| `NS-DOC` | — | Document | Metadata, bilingual abstracts, scope and trust boundary | Presentation |
| `NS-INTRO` | `NS-DOC` | Narrative section | Motivation and paradigm comparison | Narrative only |
| `NS-FOUNDATION` | `NS-DOC` | Foundation section | Limit prohibition, L0/L1, monolith definitions | Declared axioms/definitions |
| `NS-PROTOCOLS` | `NS-FOUNDATION` | Axiom group | SP1–SP4 safety protocol | Declared normative rules |
| `NS-AXIOMS` | `NS-FOUNDATION` | Axiom group | A1–A7/A10 indexed-singularity rules | Declared normative rules |
| `NS-LOCAL-ENSTROPHY` | `NS-DOC` | Derivation section | Direct indexing, shared index, A6 reduction | RICIS transformation; needs inputs |
| `NS-VORTICITY-DYNAMICS` | `NS-DOC` | Derivation section | Scale decomposition and typed terms | RICIS transformation; needs inputs |
| `NS-FRACTAL-EXPANSION` | `NS-DOC` | Recursive explanation | `R(Q)` decomposition across projection levels | Definition plus declared traversal |
| `NS-CLAIM` | `NS-DOC` | Theorem claim | Global smoothness statement | External-domain claim; not promoted automatically |
| `NS-PROOF-OBLIGATIONS` | `NS-CLAIM` | Proof-step group | Contradiction, indexing, A6, finiteness, dynamics, conclusion | Each obligation has its own status |
| `NS-TYPE-VERIFICATION` | `NS-DOC` | Validation table | homogeneous/compatible/incompatible type cases | Structured explanation |
| `NS-GLOSSARY` | `NS-DOC` | Appendix | Terms and symbol meanings | Reference material |

Every `RicisLatexSectionViewModel` has recursive `Children`. A parent may therefore represent a document, theorem, derivation, appendix, or any nested proof block. A leaf has an optional equation, evidence reference, and status, but never an executable expression tree or raw journal entry.

## Generalised MVVM contract

```text
Domain/event layer
  ILog<TSender> → classifier → semantic event model
                           ↓
Report ViewModel layer
  RicisLatexReportViewModel
    └── Sections : IReadOnlyList<RicisLatexSectionViewModel>
          ├── BlockKind, Heading, Body, Equation, Status
          ├── Claims : IReadOnlyList<RicisLatexClaimViewModel>
          ├── ProofSteps : IReadOnlyList<RicisLatexProofStepViewModel>
          ├── ValidationRows : IReadOnlyList<RicisLatexValidationRowViewModel>
          └── Children : IReadOnlyList<RicisLatexSectionViewModel>
                           ↓
View layer
  External `latex.<culture>.template` + restricted renderer
                           ↓
Artifact
  Escaped LaTeX source; optional PDF compilation is a separate gate
```

| MVVM layer | Contract | Prohibited data/behaviour |
|---|---|---|
| **Model** | Immutable semantic facts, statuses, stable resource keys, equations already declared as presentation strings and provenance IDs | `ILog`, `RicisLogEntry`, expression visitors, runtime CLR objects, raw Trace, proof execution |
| **ViewModel** | Flattened fields needed by an external template; recursive nodes transformed into deterministic outline/sections | Reclassification, theorem proving, arbitrary reflection, file/network access |
| **View** | LaTeX layout, section ordering and rendering of already escaped values | Business rules, status escalation, direct use of `Trace` |
| **Controller/Factory** | Converts classified events and declared report metadata into a view model; decides `IncludeTechnicalAppendix` explicitly | Mutation of computation, rerunning visitor/proof path |

The initial production V1 intentionally renders a **semantic outline** from classified public proof steps and limitations. Rich authored sections such as the Navier–Stokes foundation, equations and glossary are represented by the recursive model and are added through a declared source-specific builder rather than by parsing arbitrary LaTeX at runtime.

## Agent knowledge contract

An embedded reporting agent must apply the following rules when it receives a proof document or a classified log:

1. Decompose the source into a tree of `Document → Section → Claim/Derivation/Definition → ProofStep/Validation/Appendix`.
2. Preserve the original source claim as a claim; do not upgrade it to `KernelChecked` or a proven external theorem without an eligible Lean artifact and manifest provenance.
3. Represent each RICIS transformation with its rule ID, inputs, output, preconditions and evidence status.
4. Keep technical Trace exclusively in `Text` or an explicitly requested LaTeX appendix. The semantic LaTeX default is public-only.
5. Render only escaped, immutable ViewModel fields through external templates. The template never receives an executable calculation or raw source object.
6. If a source lacks a title, scope, claim status, provenance/evidence or minimum proof-step structure, request an additional document or metadata before generating an academic artifact.

## Sufficiency of the supplied exemplar

The single Navier–Stokes document is sufficient for the V1 general structure because it contains narrative context, definitions, axioms, equations, recursive decomposition, a theorem claim, enumerated proof steps, validation data and an appendix. It is **not** sufficient to validate every future domain-specific equation or a second language's terminology. Additional examples become necessary only for: (a) a non-RICIS mathematical domain with a different proof grammar, (b) a final bilingual terminology corpus, or (c) a claim intended for `KernelChecked` external-domain status.

## Implementation acceptance criteria

| ID | Requirement |
|---|---|
| `LATEX01` | LaTeX model is independent of `RicisLogEntry` and excludes Trace by default. |
| `LATEX02` | ViewModel supports recursive sections and typed claims, proof steps and validation rows. |
| `LATEX03` | External template receives a restricted, escaped projection only. |
| `LATEX04` | Public limitations and evidence boundary are rendered. |
| `LATEX05` | User-controlled technical appendix is explicit and defaults to `false`. |
| `LATEX06` | Navier–Stokes source claim is not labelled as a kernel-checked external theorem absent its own typed Lean bridge. |
| `LATEX07` | QA proves Trace exclusion, recursive hierarchy, escaping and external template loading. |

## References

[1]: `Knowledge/LaTexExamples/NavierStokes-Ricis.structural-exemplar.tex` — supplied structural exemplar; `NavierStokes-Ricis.structural-exemplar.sha256` fixes its exact input identity.  
[2]: `RICIS_LOGGING_REPORT_ARCHITECTURE_BUSINESS_AUDIT_2026-08-21.md` — semantic-report visibility and model separation contract.  
[3]: `RICIS_TEMPLATE_BEST_PRACTICES_RESEARCH_2026-08-21.md` — external template, localization and sandbox requirements.  
[4]: `RICIS_LEAN_SUBJECT_MATTER_BOUNDARY_2026-08-20.md` — external-domain proof boundary.  
[5]: `Extensions/RicisNavierStokesProofExtensions.cs` — existing NS-01 through NS-07 symbolic identity proof decomposition.

## Academic source-matching frame v2

The initial semantic template was intentionally minimal. Version 2 preserves the same safe ViewModel boundary while reproducing the academic composition of the supplied Navier–Stokes source more closely.

| Source-form component | Semantic MVVM representation | Rendering rule |
|---|---|---|
| Title and subtitle | `Title`, `Subtitle` | Academic title page; author field remains empty unless a public attribution is explicitly included. |
| Bilingual abstract | `IReadOnlyList<RicisLatexAbstractViewModel>` | Ordered centered label and escaped body before the table of contents. |
| Contents | `IncludeTableOfContents` | Template renders `\tableofcontents`; production PDF compilation must use two passes to refresh `.toc`. |
| Front matter | `Unnumbered` section presentation | Section is included in TOC without a numeric counter. |
| Definitions and protocols | `Definition` and `AxiomGroup` section kinds | Rendered through `definition` and `axiom` theorem environments. |
| Theorem and proof | `Claim` plus ordered `ProofSteps` | Rendered through `theorem` and `proof` environments. Claim status and boundary remain visible. |
| Nested derivation | Recursive `Children` | Rendered as `section/subsection/subsubsection` according to depth. |
| Type verification | `ValidationRows` | Rendered as a bounded academic tabular block. |
| Glossary / appendix | `Appendix` section presentation | First appendix node emits `\appendix`; it is numbered alphabetically. |
| Closing | `Conclusion`, `Epilogue` | Unnumbered closing sections with TOC entries. |

The source composition is a **presentation and recursive-structure exemplar**. It must not be treated as a kernel proof of the external Navier–Stokes smoothness assertion. Equation strings remain escaped textual projections until a separately reviewed typed mathematics renderer is introduced; raw LaTeX from a runtime log, user request, callback, or expression tree is never inserted into the external template.

The external Navier–Stokes v2 exemplar is `Logging/Templates/navier-stokes-ricis.exemplar.json`. Its visual compiler evidence is generated under `artifacts/latex-source-form-validation/` and must remain ignored build evidence rather than a source of proof status.
