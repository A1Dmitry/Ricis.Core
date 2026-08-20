# Finance host-boundary prerequisites — 2026-08-20

**Status:** Blocked at host boundary; prerequisite map implemented, production integration deferred.
**Evidence:** [`BACKLOG.md`](BACKLOG.md), [`PAYMENT_LAUNCH_INTEGRATION.md`](PAYMENT_LAUNCH_INTEGRATION.md), and the current `Ricis.Finance` Domain/Application ports.
**Current gate:** 386/386 Core regression, 18/18 Finance regression, 8/8 Lean artifacts; no provider production claim is made by this checklist.

**Purpose:** дешёвый подготовительный increment перед дорогой FIN-02+ реализацией.
**Rule:** не создавать fake provider adapter, не принимать test stub за production verifier и не передавать secrets в Domain/Application.

## Current boundary

| Area | Present | Missing before production implementation |
|---|---|---|
| Payment launch | `IPaymentLaunchPort`, `PaymentHandoff`, BY ERIP/E-POS and RU SBP bePaid mapping | Host endpoint/UI, merchant configuration and route-specific production acceptance |
| Webhook application boundary | `IPaymentProviderWebhookVerifier`, `ProviderWebhookPayload`, `RecordProviderPaymentService` | Concrete provider verifier, signature/status/amount/currency/reference mapping and replay/idempotency policy |
| Persistence boundary | Repository ports for settlements, payouts and invoices | Host database adapters, unique keys, transaction boundary and outbox |
| Compliance boundary | Versioned/effective-dated policy ports | Approved policy implementation, business process and current route terms |
| Operations | Domain/application contracts and regression fixtures | Secret provider, redacted audit log, metrics, alerts, replay window and incident runbook |

## FIN-02 unblock package

Before implementation of a production bePaid verifier, the host must provide all of the following:

| Prerequisite | Required evidence | Blocking if absent | Current status | Evidence boundary |
|---|---|---:|---|---|
| Official callback specification | Provider-owned fields, signature algorithm, status vocabulary, amount/currency/reference semantics | Yes | `Blocked` | No official provider specification is present in this repository |
| Test credentials | Non-production merchant/shop identity and secret delivery outside source control | Yes | `Blocked` | No credentials are accepted or stored by this preparation increment |
| Host webhook endpoint | HTTPS route, authentication boundary, body/header preservation and provider allow-list | Yes | `Blocked` | No host endpoint or deployment boundary is supplied |
| Replay/idempotency contract | Event identifier, duplicate/out-of-order behavior and retention window | Yes | `Blocked` | No provider event contract or retention decision is supplied |
| Route choice | Explicit country + rail + currency; no generic CIS fallback | Yes | `Blocked` | Domain supports explicit rails, but no production merchant route is selected |
| Sandbox vectors | Valid, malformed, wrong-secret, wrong-amount/currency, duplicate and failed callbacks | Yes | `Blocked` | No provider sandbox vector package is supplied |

## FIN-03/04/05/10 prerequisite sequence

`FIN-03` requires a host database and transaction/outbox decision before concrete persistence code. `FIN-04` requires a host Web API/UI decision before treating `PaymentHandoff` as a complete checkout. `FIN-05` requires provider sandbox access and CI-safe secret injection. `FIN-10` requires the deployment’s secret provider, logging/metrics stack and incident ownership. These are host integration choices, not missing Domain abstractions.

| Backlog item | Preparation result | Production status |
|---|---|---|
| `FIN-02` webhook verifier | Payload/port boundary mapped; six mandatory external prerequisites listed | `Blocked` |
| `FIN-03` persistence/outbox | Repository and event boundary identified | `Blocked` on host database/transaction decision |
| `FIN-04` secure checkout | `PaymentHandoff` boundary identified | `Blocked` on host endpoint/UI and route acceptance |
| `FIN-05` sandbox contract | Required vector categories listed | `Blocked` on provider sandbox and CI-safe credentials |
| `FIN-10` observability | Required secret/log/metric/incident boundary listed | `Blocked` on deployment operations stack |

## Security and non-goals

No direct MNS, EasyStaff, universal CIS selector or universal bank-fee adapter is authorized by this checklist. Each needs its own official API/webhook contract, credentials, route evidence and regression suite. Browser return, QR scan and bank deep-link completion never create `ProviderPayment`; only a verified provider event may do that.

## Acceptance

This preparation increment is complete when the host owner supplies or explicitly marks each prerequisite as `Provided`, `Blocked` or `Not applicable`, with source evidence. The current repository evidence supports only `Blocked` for all six mandatory FIN-02 rows; this is a truthful preparation result, not a production implementation. `Blocked` is an acceptable result and must not be replaced with a placeholder implementation. Production FIN-02 remains blocked until all mandatory rows are provided.

### Task 2 evidence boundary

No provider verifier, webhook endpoint, credential, sandbox adapter or production persistence code was added. The status table records the absence of external prerequisites and prevents an unsupported transition to `Provided`. The existing domain/application ports remain available for later host integration.
