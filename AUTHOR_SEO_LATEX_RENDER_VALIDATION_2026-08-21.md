# Author SEO LaTeX render validation

**Status:** `Validated`  
**Date:** 2026-08-21  
**Scope:** trusted public author attribution in the semantic LaTeX report.

## Rendered artifact

The validation source `trusted-author-seo-validation.tex` was produced by the real external `latex.en-US.template` using `RicisLatexAuthorAttributionResolver.Resolve("dima.aley@gmail.com", false)`. It compiled with `pdflatex -interaction=nonstopmode -halt-on-error` into a two-page PDF.

## Observed result

| Check | Result |
|---|---|
| Author SEO section | Visible with the expected heading. |
| Public profile | Renders Дмитрий Алейников, alternate name, ORCID, public description, keywords and public work list from `AuthorSeoProfile.RicisAuthor`. |
| Cyrillic | Rendered legibly with the template's UTF-8/T2A/Babel configuration. |
| Selector email | Absent from generated `.tex`, extracted PDF text and the visual document. |
| Paid-user status | Absent from the rendered document. |
| Technical Trace | Not included; author block is independent of the explicit technical appendix option. |
| Compile result | PASS; no fatal LaTeX errors. |

## Privacy boundary

The selector email is used only to choose the trusted public profile for the current process. It is not a `RicisLatexAuthorAttributionViewModel` field, is not supplied to the external template and is not written to the report. Paid-user author information remains callback-only and is recorded as a separate deferred UI/entitlement task in [`RICIS_PAID_USER_AUTHOR_CALLBACK_DEFERRED_TASK_2026-08-21.md`](RICIS_PAID_USER_AUTHOR_CALLBACK_DEFERRED_TASK_2026-08-21.md).

## Evidence

[1]: `Logging/RicisLatexAuthorAttribution.cs` — transient resolver and callback-only contract.  
[2]: `Logging/Templates/latex.en-US.template` — external author block.  
[3]: `RegressionTests/RicisSemanticReportSuite.cs` — `AUTH01`–`AUTH03`.  
[4]: `artifacts/latex-author-validation/trusted-author-seo-validation.tex` and `.pdf` — generated validation artifacts.
