# Finance host-boundary prerequisites — 2026-08-20

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

| Prerequisite | Required evidence | Blocking if absent |
|---|---|---:|
| Official callback specification | Provider-owned fields, signature algorithm, status vocabulary, amount/currency/reference semantics | Yes |
| Test credentials | Non-production merchant/shop identity and secret delivery outside source control | Yes |
| Host webhook endpoint | HTTPS route, authentication boundary, body/header preservation and provider allow-list | Yes |
| Replay/idempotency contract | Event identifier, duplicate/out-of-order behavior and retention window | Yes |
| Route choice | Explicit country + rail + currency; no generic CIS fallback | Yes |
| Sandbox vectors | Valid, malformed, wrong-secret, wrong-amount/currency, duplicate and failed callbacks | Yes |

## FIN-03/04/05/10 prerequisite sequence

`FIN-03` requires a host database and transaction/outbox decision before concrete persistence code. `FIN-04` requires a host Web API/UI decision before treating `PaymentHandoff` as a complete checkout. `FIN-05` requires provider sandbox access and CI-safe secret injection. `FIN-10` requires the deployment’s secret provider, logging/metrics stack and incident ownership. These are host integration choices, not missing Domain abstractions.

## Security and non-goals

No direct MNS, EasyStaff, universal CIS selector or universal bank-fee adapter is authorized by this checklist. Each needs its own official API/webhook contract, credentials, route evidence and regression suite. Browser return, QR scan and bank deep-link completion never create `ProviderPayment`; only a verified provider event may do that.

## Acceptance

This preparation increment is complete when the host owner supplies or explicitly marks each prerequisite as `Provided`, `Blocked` or `Not applicable`, with source evidence. `Blocked` is an acceptable result and must not be replaced with a placeholder implementation. Production FIN-02 remains blocked until all mandatory rows are provided.
