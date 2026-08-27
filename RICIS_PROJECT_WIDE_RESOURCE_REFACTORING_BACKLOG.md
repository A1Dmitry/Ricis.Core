# RICIS Project-Wide Resource Refactoring Backlog

**Status:** Active, mandatory for new public text.  
**Created:** 2026-08-21.  
**Scope:** All C# source files under `Ricis.Core`, excluding generated/build output.  
**Rule:** No string literal may be silently deleted to satisfy localization. Every literal is classified as **localized public text**, **localized controlled diagnostic**, **invariant protocol/token**, **format/syntax**, or **test fixture** before migration.

## Baseline audit

The initial source-only scan identified **5,196** string-literal occurrences. This number is a triage baseline, not a count of localization candidates: test fixtures, regexes, JSON property names, C# expressions, LaTeX syntax, stable theorem IDs, and transport protocol tokens must not be translated.

| Area | Literal occurrences | Initial disposition | Priority |
|---|---:|---|---|
| `Logging` | 315 | Public report labels, renderer diagnostics, LaTeX syntax, template/asset names | P0 |
| `Proofs` | 280 | Academic/proof wording versus stable rule IDs | P1 |
| `Ricis.Console` | 381 | User-visible console content and usage diagnostics | P1 |
| `Ricis.WebApi` | 67 | API-visible problem details and validation messages | P1 |
| `Ricis.WebAssembly` | 20 | Client-visible UI labels | P1 |
| `Ricis.Finance` | 348 | Regulated user-facing messages, provider diagnostics and invariant codes | P2 |
| `Ricis.Numerics` | 274 | Public numerical errors and invariant algorithm tokens | P2 |
| `Extensions` | 401 | Domain messages, rule IDs and expression fragments | P2 |
| `Expressions` | 147 | Public explanation text versus expression syntax | P2 |
| `Phases` | 57 | Lifecycle/user messaging versus state keys | P2 |
| `Solvers` | 18 | Solver report messages | P2 |
| `RegressionTests` | 1,930 | Test descriptions and expected values; resource tests only where public text is asserted | P3 |
| `UnitTests` | 825 | Test fixtures and expected values; do not localize blindly | P3 |
| Other root/domain files | 133 | Case-by-case classification | P3 |

## Resource topology

```text
Resources/
  RicisSemanticReportStrings.resx          # neutral English fallback
  RicisSemanticReportStrings.en-US.resx    # optional English override only when needed
  RicisSemanticReportStrings.ru-RU.resx
  RicisSemanticReportStrings.fr-CA.resx
  RicisSemanticReportStrings.de-DE.resx
  RicisSemanticReportStrings.hi-IN.resx
  RicisSemanticReportStrings.ms-MY.resx
  RicisSemanticReportResources.cs          # strongly typed facade and BCP-47 fallback
```

The initial migration covers **semantic reporting and LaTeX PDF labels**. The facade performs the fixed fallback `requested supported culture → neutral English`. It never receives or persists requester email, payment entitlement, country analytics, browser state, or technical Trace payload.

## Classification contract

| Class | Examples | Resource rule |
|---|---|---|
| Localized public text | Academic headings, UI labels, client-facing validation, report table headers | Required `.resx` key for every supported culture or documented English fallback. |
| Localized controlled diagnostic | Safe compiler/format errors that can reach a client | Resource key; technical evidence remains separate from Academic model. |
| Invariant protocol/token | `KernelChecked`, `Deferred`, theorem IDs, JSON field names, ISO country codes, format enum names | Never translate; document semantic meaning instead. |
| Format/syntax | Regexes, LaTeX commands, file extensions, shell compiler arguments, XML/JSON syntax | Never translate. |
| Test fixture | Expected source text, adversarial inputs, golden data | Keep literal unless it represents a public localized contract; resource test must assert the correct culture result. |

## Migration order

| Sprint | Scope | Definition of done |
|---|---|---|
| P0 | `Logging`, `Resources`, external templates and semantic PDFs | No localized C# report label remains outside the strongly typed resource facade; all locale templates consume public resource placeholders. |
| P1 | Console, Web API and WebAssembly public UI/problem text | Locale is request/client scoped; endpoint or UI culture tests prove explicit choice beats country default. |
| P2 | Proofs, Solvers, Extensions, Finance and Numerics public messages | Domain theorem IDs and invariant statuses remain stable; public explanatory text gains resources. |
| P3 | Test cleanup and remaining root literals | Tests classify fixtures; a public wording change requires a culture-specific expected result. |

## Mandatory implementation sequence

1. **Classify** the literal; never assume a string is translatable.
2. Add a neutral resource key and all currently supported culture values.
3. Introduce or extend a strongly typed facade. Do not call `ResourceManager` ad hoc throughout domain code.
4. Replace only the matching localized source use. Preserve IDs, JSON/Lean/LaTeX syntax and diagnostic boundaries.
5. Add tests for every selected culture and fallback.
6. Run `git diff --check`, no-deletion verification, the relevant public API tests and full quality gate.
7. Record remaining literals with category, owner and dependency; do not close a sprint by deleting code.

## Country and client locale policy

The authoritative request-scoped policy is `Logging/Templates/ricis-country-locale-coverage.exemplar.json`.

| Country | Default locale | Explicit supported locale |
|---|---|---|
| United States | `en-US` | `en-US` |
| Canada | `en-US` | `en-US`, `fr-CA` |
| Germany | `de-DE` | `de-DE`, `en-US` |
| India | `hi-IN` | `hi-IN`, `en-US` |
| Malaysia | `ms-MY` | `ms-MY`, `en-US` |

A future UI must pass `countryCode` and/or explicit `locale` only for the active report request. `RicisReportLocaleResolver` applies explicit supported locale, then country default, then `en-US`; it exposes no persistence method.

## Acceptance tests

- Every listed culture resolves required resource keys.
- The requested supported locale takes precedence over country default.
- Unsupported country/locale falls back to English without exception or storage.
- Template markers select the same locale as the resource facade.
- Default Academic output excludes `Trace`, raw runtime entries, requester email and payment state.
- The LaTeX-to-PDF adapter records bounded compiler evidence separately from academic content.
- Every supported template compiles in two passes with its declared engine.
