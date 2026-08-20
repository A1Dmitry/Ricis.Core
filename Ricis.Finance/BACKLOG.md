# Backlog развития `Ricis.Finance`

**Статус:** единый источник незавершённых работ после релиза `v0.5.0`.
**Владелец продукта:** пользователь проекта.
**Правило приоритета:** сначала безопасность факта платежа и идемпотентность, затем автоматизация, затем расширение географии и UX.

> Этот backlog отделяет **готовый контракт** от **готовой production-интеграции**. Наличие `IPaymentLaunchPort` или bePaid adapter означает, что библиотека умеет сформировать provider session по документированному протоколу. Это не означает, что у конкретного merchant account уже есть договор, credentials, доступный метод оплаты или принятый webhook на production.

## Текущее состояние

**Count provenance:** The `328 + 12` values below are retained as a historical v0.5.0 snapshot. They are not the current gate. Current verified counts are recorded in `PUBLIC_API_CLI_AUDIT.md` and the sprint time-evidence files.

| Область | Что уже сделано | Что всё ещё отсутствует |
|---|---|---|
| Domain | `Money`, `FeeBreakdown`, `ProviderPayment`, `Settlement`, `PayoutRequest`, `Invoice`, tax evidence и инварианты денег. | Дальнейшая persistence реализация Invoice остаётся host-specific. |
| Application | Запись подтверждённого provider payment, idempotent payout request, policy ports, launch registry, Invoice issue/cancel/expire и invoice-owned launch workflow. | Реальные persistence adapters, reconciliation/status workflow. |
| Payment launch | `BY + ЕРИП/E-POS + BYN`: QR и provider-issued bank deep links. `RU + СБП + RUB`: provider-hosted QR/bank selector. | Конфигурация конкретного merchant account, host UI, реальный webhook verifier и production E2E. |
| Compliance | Tax, fee и payout rules вынесены за versioned/effective-dated ports. | Заполненные и утверждённые policy implementations, authorised tax route, действующие тарифы/условия конкретного банка и провайдера. |
| QA / DevOps | Historical v0.5.0 snapshot: `328` Core + `12` Finance regressions, GitHub CI, Swagger smoke-test, LeanDoc compilation. Current gate evidence is maintained separately: `386/386` Core, `18/18` Finance, `8/8` Lean artifacts. | Contract tests с sandbox merchant account, secret scanning, production observability, incident drill. |

## Неподвижные границы

Все следующие спринты обязаны сохранять следующие правила.

| Правило | Архитектурное следствие |
|---|---|
| Факт платежа создаётся только из проверенного provider event. | Browser return, app deep link и QR scan не могут создавать `ProviderPayment` или подтверждать settlement. |
| `Gross`, provider fee, bank fee и `Net` — разные факты. | Нельзя считать налоговую базу из net payout или вписывать неизвестную комиссию как константу. |
| Страна, rail и валюта выбираются явно. | `CIS` не является fallback route; новый рынок добавляется отдельной capability только после проверки API и callback contract. |
| Правила НПД, лимиты, сроки, комиссии и FX меняются во времени. | Используются versioned/effective-dated policy и snapshot ports; production constants запрещены. |
| Секреты принадлежат host-приложению. | Domain/Application не получают HTTP client, API key, Basic secret, SDK или URI банка. |
| Неподтверждённый внешний API не реализуется. | Не создавать прямой МНС, EasyStaff или банковский adapter до получения официального API, договора и test credentials. |

Подтверждённые ограничения НПД и отсутствие публичного server-to-server API МНС зафиксированы в [`../FINANCE_LIBRARY_COMPLIANCE_FINDINGS.md`](../FINANCE_LIBRARY_COMPLIANCE_FINDINGS.md). МНС публикует работу через официальную цифровую платформу, поэтому путь выдачи чека остаётся port/manual-authorised workflow, а не прямым неподтверждённым клиентом. [1] Публичный обзор QR-flow и provider contracts находится в [`PAYMENT_LAUNCH_INTEGRATION.md`](PAYMENT_LAUNCH_INTEGRATION.md).

## Последовательность спринтов

| ID | Следующий результат | Приоритет | Зависит от | Definition of Done |
|---|---|---:|---|---|
| `FIN-01` | **Invoice ownership и launch lifecycle.** Реализовать `Invoice` aggregate, `InvoiceStatus`, invoice repository и commands выпуска, отмены, истечения и payment-launch из счёта. | DONE | Нет | Выполнено: launch запрещён для отменённого/истёкшего invoice; один invoice имеет auditable order reference; state transitions и duplicate command покрыты FIN10–FIN12. |
| `FIN-02` | **bePaid inbound confirmation.** Реализовать `IPaymentProviderWebhookVerifier` для документированного bePaid callback, mapping status/amount/currency/reference и safe rejection невалидного callback. | NEXT P0 / `Blocked` | Merchant onboarding, test credentials, host endpoint, official callback spec, replay contract and sandbox vectors | Production implementation remains blocked; prerequisite record: [`FINANCE_HOST_BOUNDARY_PREREQUISITES_2026-08-20.md`](FINANCE_HOST_BOUNDARY_PREREQUISITES_2026-08-20.md). No fake webhook adapter or payment fact is created; only a verified `successful` event may later create the fact. [2] |
| `FIN-03` | **Надёжное хранилище и outbox.** Реализовать persistence для settlements, payouts, invoices и launch telemetry; добавить уникальные ключи provider event / idempotency / invoice reference и transactional outbox. | P0 | Выбор host database | Перезапуск процесса, повтор webhook и конкурентная доставка не создают второй settlement/payout; публикация внутренних событий не теряется между DB commit и delivery. |
| `FIN-04` | **Безопасный host checkout.** Добавить server-side endpoint создания launch, allow-list return/notification URLs, защищённый рендер GET/POST `PaymentHandoff`, mobile bank-selection UI и explicit user action перед открытием внешнего банка. | P0 | FIN-01, FIN-02 | Client не получает secret; HTML form не допускает подмену action/fields; bank deep link не открывается автоматически; return page показывает только server-confirmed status. |
| `FIN-05` | **Sandbox contract suite.** Подключить bePaid test mode и создать contract tests для ЕРИП/E-POS BYN и СБП RUB: session, expiry, handoff, webhook, duplicate delivery, failed/expired flow. | P0 | FIN-02, FIN-04, provider sandbox access | Тесты исполняются с выделенными test credentials в CI-safe environment; live money не создаётся; documented `RequestID` retry подтверждён. [3] |
| `FIN-06` | **Официальный FX adapter.** Реализовать `NbrbFxRateSource` через публичный НБРБ JSON API, с календарной датой, scale, source evidence, cache boundary и regression cases. | P1 | FIN-03 | Один settlement хранит immutable official-rate snapshot; запрос никогда не изменяет исторический rate; отсутствие rate возвращает явную domain/application error. |
| `FIN-07` | **Versioned tax policy и receipt work queue.** Реализовать effective-dated NPD policy store, `AnnualTaxPosition` rules, receipt-candidate review queue и manual/authorised `ITaxReceiptGateway` adapter. | P1 | FIN-03, подтверждённый business process | Ставки, пороги, minimum tax и сроки не захардкожены; каждое решение хранит policy version; нет прямого вызова неподтверждённого API МНС. [1] |
| `FIN-08` | **Payout и bank fee routes.** Выбрать один официальный provider/bank route, реализовать `IPaymentProviderPort` и `IBankFeeSchedule` с effective date, provider payout event и reconciliation. | P1 | Письменный договор, API docs, credentials, FIN-03 | Release policy срабатывает до provider call; сумма не превышает available settlement; actual bank fee отделена от provider fee; submit/retry/refusal идемпотентны. |
| `FIN-09` | **Refund, cancellation и post-payment lifecycle.** Добавить refund request aggregate, provider-specific refund adapter и связь с первоначальным confirmed payment без изменения исторического gross fact. | P1 | FIN-02, выбранный provider route | Частичный/полный refund не превышает допустимый остаток; webhook/refund status auditable; налоговое/receipt следствие оставлено policy, не выводится из предположения. |
| `FIN-10` | **Operational security and observability.** Добавить secret-provider integration, secret scanning, structured audit log с redaction, metrics, alerting, correlation id, replay window и incident runbook. | P0 | FIN-02, FIN-03 | Secret не попадает в code/logs/traces; webhook failure и reconciliation lag наблюдаемы; recovery drill воспроизводим без изменения money facts. |
| `FIN-11` | **Production readiness review.** Провести совместный QA/compliance/DevOps review: договоры, KYC/AML, тарифы, retention, data-access, backup/restore, rate limits и rollback. | P0 перед production | FIN-02–FIN-10 по нужному маршруту | Подписан route-specific checklist; sandbox evidence приложен; production switch требует отдельного явного подтверждения владельца. |
| `FIN-12` | **Расширение СНГ по одному rail.** Выбрать одну страну и одного provider; собрать официальные API/webhook docs и добавить отдельный adapter capability. | P2 | FIN-05, FIN-11 | Ни один существующий BY/RU route не меняется; новый `country + rail + currency` покрыт sandbox tests, webhook verification и documentation. |

## Заблокированные направления

| ID | Направление | Причина блокировки | Условие разблокировки |
|---|---|---|---|
| `BLOCK-01` | Прямой adapter МНС НПД | В проверенных публичных материалах не найден public server-to-server API для сторонней библиотеки. [1] | Официальная спецификация, допустимая модель авторизации, sandbox/credential contract и подтверждение владельца процесса. |
| `BLOCK-02` | EasyStaff Invoice adapter | Публичная продуктовая страница описывает пользовательский flow, но не даёт developer API/webhook contract. [4] | Официальные API docs, webhook verification, credentials и договорное разрешение на интеграцию. |
| `BLOCK-03` | Универсальный «CIS bank selector» | Нет общего подтверждённого межстранового payment rail, bank directory или callback contract. | Отдельная проверка provider/rail для конкретной страны; добавление capability без изменения существующих. |
| `BLOCK-04` | Универсальная комиссия МТБанка или любого банка | Тарифы route- и date-dependent; публичные документы не дают константу для всех payout routes. [5] | Действующий договор/тариф конкретного route и effective-date fee schedule. |

## Дополнительные улучшения после production-readiness

Эти пункты не блокируют первый подтверждённый маршрут, однако уменьшают будущую стоимость поддержки.

| ID | Улучшение | Приоритет | Критерий завершения |
|---|---|---:|---|
| `FIN-13` | Независимый adapter НБРБ и contract-test fixture для официальных FX payload. | P2 | Обновление внешнего JSON не ломает mapping silently; schema drift имеет alert. |
| `FIN-14` | Provider conformance matrix: currencies, expiry, refund, webhook signing, idempotency duration и supported environments. | P2 | Каждый adapter публикует versioned capability matrix и error mapping. |
| `FIN-15` | User-facing receipt/reconciliation history, без отображения секретов и с корректным timezone. | P2 | Пользователь видит payment session, подтверждённый event и payout как разные состояния. |
| `FIN-16` | Load, retry and failure-injection tests для webhook/outbox/reconciliation. | P2 | Доказана корректность при network timeout, duplicate callback, out-of-order event и DB retry. |
| `FIN-17` | Data retention, export and deletion policy для PII, payment metadata и audit evidence. | P2 | Сроки хранения и удаления конфигурируемы, а audit integrity сохраняется. |

## Рекомендуемый следующий спринт

`FIN-01` завершён. Следующим следует взять **`FIN-02`**, а параллельно подготовить только безопасные host-boundary контракты из `FIN-04`. Для FIN-02 потребуются официальная callback specification, test credentials и URL тестового webhook endpoint; до их получения production adapter не создаётся.

После завершения `FIN-01` пользователь выбирает первый production rail по договору: **BY/ЕРИП-E-POS/BYN** или **RU/СБП/RUB**. Для запуска `FIN-02` потребуются test credentials, URL тестового webhook endpoint и подтверждение доступного merchant payment method. Ключи нельзя передавать в source control или клиентское приложение.

## Правило обновления backlog

Каждая новая внешняя гипотеза добавляется сначала как запись исследования со ссылкой на первоисточник. Она может быть переведена из `BLOCK-*` в активный `FIN-*` только после фиксации API contract, authentication model, sandbox/production access, status confirmation model и test plan. После каждого завершённого спринта нужно обновить статус, regression IDs, release version и link на CI run в этом же документе.

## References

[1]: https://www.nalog.gov.by/professional_income_tax/ "МНС Республики Беларусь — Налог на профессиональный доход"
[2]: https://docs.bepaid.by/ru/integration/apm_api/webhooks/ "bePaid API — Автоматические уведомления альтернативных способов оплаты"
[3]: https://docs.bepaid.by/ru/using_api/idempotent_requests/ "bePaid API — Идемпотентные запросы"
[4]: https://easystaff.io/ru/invoice "EasyStaff Invoice — официальный продуктовый материал"
[5]: https://www.mtbank.by/about/rates/ "МТБанк — действующие тарифы"
