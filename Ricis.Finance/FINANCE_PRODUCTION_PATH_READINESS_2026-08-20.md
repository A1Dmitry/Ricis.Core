# Finance production-path readiness boundary — 2026-08-20

**Status:** Blocked — readiness matrix prepared; production path is not authorized.
**Evidence:** [`BACKLOG.md`](BACKLOG.md), [`FINANCE_HOST_BOUNDARY_PREREQUISITES_2026-08-20.md`](FINANCE_HOST_BOUNDARY_PREREQUISITES_2026-08-20.md), [`PAYMENT_LAUNCH_INTEGRATION.md`](PAYMENT_LAUNCH_INTEGRATION.md).
**Rule:** Domain/Application contracts are not production readiness. No provider call, persistence adapter, checkout endpoint or secret integration is created by this document.

| Backlog | Present contract boundary | Required external/host decision | Status | Release gate |
|---|---|---|---|---|
| `FIN-03` persistence/outbox | Repository ports for invoices, settlements and payouts; application idempotency contracts | Host DB, unique keys, transaction boundary, outbox delivery and recovery policy | `Blocked` | No concrete persistence until host architecture is approved |
| `FIN-04` secure checkout | `IPaymentLaunchPort`, `PaymentHandoff`, explicit country/rail/currency route | Host Web API/UI, allow-listed return/notification URLs, explicit user action and server-confirmed return state | `Blocked` | No client secret and no automatic bank/deep-link opening |
| `FIN-05` sandbox contract | Provider session and launch domain boundary | Official provider sandbox, CI-safe credential injection, session/expiry/webhook vectors and RequestID retry contract | `Blocked` | No live money and no undocumented test adapter |
| `FIN-10` observability/security | Domain/application audit and regression fixtures | Deployment secret provider, redacted structured logs, metrics, alerts, replay window and incident owner | `Blocked` | No secret in source/logs/traces; recovery drill required |
| `FIN-11` readiness review | Backlog and route-specific prerequisite checklist | QA/compliance/DevOps approval, KYC/AML, tariffs, retention, backup/restore, rate limits and rollback decision | `Blocked` | Explicit owner confirmation required before production switch |

## Route boundary

The current supported contract routes are BY / ЕРИП-E-POS / BYN and RU / СБП / RUB as documented in [`PAYMENT_LAUNCH_INTEGRATION.md`](PAYMENT_LAUNCH_INTEGRATION.md). This matrix does not infer a universal CIS rail or bank selector. A new route requires its own official API, authentication, callback, sandbox and regression evidence.

## Truthful completion boundary

This preparation increment is `Blocked`, not `Done`: all five production-path packages have a mapped contract boundary, but the external/host decisions required for implementation are absent. No placeholder adapter is accepted as evidence.
