# Отложенная задача: paid-user авторство для LaTeX-документа через client callback

**Task ID:** `LATEX-AUTH-PAID-01`  
**Status:** `Deferred`  
**Complexity:** `L`  
**Dependency risk:** `Blocked` until a client UI and an authenticated paid-user entitlement contract are selected.

## Delivered boundary

The current `RicisLatexAuthorAttributionResolver` accepts a requester selector and an `isPaidUser` decision **only for the current in-memory document request**. It never stores the requester email, paid status, payment data, customer ID or callback result. A trusted project identity selects the existing public `AuthorSeoProfile.RicisAuthor`. A paid-user request without a callback returns `CallbackRequired` and renders no author block.

The library does not validate payment entitlement, host a user interface, call an identity provider, persist a profile, or infer authorship from an email address other than the explicitly configured trusted local selector.

## Target UI and callback contract

| Concern | Required future behaviour | Forbidden behaviour |
|---|---|---|
| Entitlement | Client application establishes paid entitlement through its selected authenticated provider before requesting a document. | Library trusts a client-provided paid flag as proof of payment. |
| Author form | Client holds an opt-in public display name, alternate name, ORCID, description, keywords and public works only for the active document action. | Server/database persistence of the form, requester email or paid status. |
| Callback | UI passes `Func<RicisLatexPaidUserAuthorInput>` to the current document request. The callback is invoked at most once. | Background callback, network invocation by the library, retries that duplicate personal-data processing. |
| Rendering | Only the returned public fields are projected into `RicisLatexAuthorAttributionViewModel` and the external template. | Email, customer ID, payment receipt, JWT, raw form object or UI state appearing in a report, template, log or exception. |
| Deletion | Closing/cancelling the document action drops the client-side form state according to the client application's own lifecycle. | Cache, telemetry, analytics event or server-side record created by this library. |

## Proposed client-only flow

```text
paid entitlement verified by client
  → opt-in public authorship form in client memory
  → one callback for document request
  → RicisLatexPaidUserAuthorInput
  → document-only LaTeX ViewModel
  → rendered artifact returned to client
  → client discards form state after action
```

## Acceptance criteria

| ID | Criterion |
|---|---|
| `AUTH-P01` | UI requires explicit opt-in before providing any public author field to the callback. |
| `AUTH-P02` | A paid entitlement is verified by the chosen client/provider contract, not by the Core library. |
| `AUTH-P03` | Network inspection, source inspection and test fixtures show no persistence of email, entitlement, payment data or callback payload in Core. |
| `AUTH-P04` | Callback is invoked no more than once per explicit document action and never outside it. |
| `AUTH-P05` | Rendered LaTeX and all logs exclude email, customer IDs, payment tokens and private UI state. |
| `AUTH-P06` | Input validation accepts only public display metadata and absolute public ORCID/work URLs. |
| `AUTH-P07` | Cancel and callback-missing paths render no author block and produce a controlled `CallbackRequired` state. |
| `AUTH-P08` | New client API and every public Core contract receive direct regression tests in the same change. |

## Non-goals

This task does not create authentication, subscriptions, payment processing, CRM records, user accounts, a server profile store, or an API endpoint for author metadata. Those would require a separately approved host architecture, data-protection design and explicit user consent model.
