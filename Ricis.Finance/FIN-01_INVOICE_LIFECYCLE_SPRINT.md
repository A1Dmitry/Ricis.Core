# FIN-01 — Invoice ownership и launch lifecycle

**Статус:** реализовано и локально проверено.

## Цель

Счёт должен владеть auditable order reference и явно связывать сумму, валюту, страну плательщика и payment rail. Создание provider launch допускается только для активного счёта и не должно превращать browser return, QR scan или provider handoff в подтверждённый факт платежа.

## Реализовано

| Слой | Артефакт | Назначение |
|---|---|---|
| Domain | `Ricis.Finance.Domain.Invoice` | Aggregate с `Issued`, `Cancelled`, `Expired`, immutable amount/route/order reference и строгими transition guards |
| Domain | `InvoicePaymentRoute` | Явная страна и rail без CIS fallback |
| Application | `IssueInvoiceService` | Issue command, duplicate idempotency key и duplicate order reference rejection |
| Application | `CancelInvoiceService` | Явный переход `Issued → Cancelled` |
| Application | `ExpireInvoiceService` | Переход в `Expired` только после `ExpiresAtUtc` |
| Application | `CreateInvoiceLaunchService` | Repository-first launch idempotency, active-invoice guard и provider evidence |
| Application | `IInvoiceRepository` | Persistence boundary для aggregate, order reference и issue key |
| Application | `IInvoiceLaunchRepository` | Persistence boundary для launch evidence |
| QA | `FIN10`–`FIN12` | Issue idempotency, transition safety, launch idempotency и expired rejection |

## Trust boundary

`Invoice` не знает о HTTP, SDK, credentials или provider secrets. Application service преобразует сохранённый `InvoicePaymentRoute` в существующий `CreatePaymentLaunch`, вызывает injected `IPaymentLaunchPort`, а затем сохраняет только provider-issued launch evidence. Подтверждённый payment fact по-прежнему создаётся исключительно через проверенный webhook workflow.

## Definition of Done

| Проверка | Результат |
|---|---|
| Один invoice имеет auditable order reference | PASS |
| Duplicate issue command не создаёт второй aggregate | PASS |
| Другой order reference с тем же или новым конфликтующим ключом отклоняется | PASS |
| Cancelled invoice нельзя отменить повторно | PASS |
| Expired transition запрещена до deadline | PASS |
| Expired invoice автоматически фиксируется перед отказом launch | PASS |
| Duplicate launch возвращает сохранённое provider evidence | PASS |
| Provider не вызывается для expired invoice | PASS |
| NuGet publication | Не выполнялась по решению владельца проекта |

## Regression evidence

После FIN-01 Finance regression suite содержит **12 PASS тестов**. Следующий незавершённый P0 — `FIN-02` inbound bePaid confirmation; его production implementation заблокирована до получения официальной callback specification, test credentials и webhook endpoint.
