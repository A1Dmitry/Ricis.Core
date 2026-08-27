# QR-платёж с выбором банка: интеграция `Ricis.Finance`

**Статус:** реализованная infrastructure-граница для документированных маршрутов `BY/ЕРИП-E-POS/BYN` и `RU/СБП/RUB`.

> **Важно.** Открытие банковского приложения и возврат браузера не подтверждают перевод денег. Единственным основанием для `ProviderPayment` остаётся проверенное серверное уведомление провайдера либо его аутентифицированный запрос статуса. Пользователь подтверждает платёж самостоятельно в выбранном банковском приложении.

## Что именно найдено

Описанный сценарий в Беларуси — это **ЕРИП QR / E-POS**. При открытии платёжной ссылки плательщик выбирает банк или платёжный сервис, а система открывает интернет-банкинг; QR из мобильного банка ведёт на предзаполненную услугу ЕРИП. [1] Документация bePaid показывает мобильный сценарий E-POS/ЕРИП со списком банков и переходом в приложение выбранного банка. [2]

В России соответствующий rail — **СБП**. СБП поддерживает оплату через QR, кнопку и ссылку; в bePaid для метода `sbp` ответ содержит provider-hosted URL, где плательщик видит QR либо выбор банковского приложения. [3] [4]

| Код страны плательщика | Rail | Валюта, разрешённая этим адаптером | Что получает host |
|---|---|---|---|
| `BY` | `PaymentRail.BelarusEripEpos` | `BYN` | QR image, плюс provider-returned список банков и HTTPS deep links по `iOS` / `Android` / `HuaweiApp`. |
| `RU` | `PaymentRail.RussiaSbp` | `RUB` | Provider-hosted `PaymentHandoff` на QR/селектор банков СБП. |
| Другой код страны СНГ | нет автоматического rail | — | Пустой список capabilities или явная ошибка `NotSupportedException`; перевод в BY/RU не выполняется. |

**СНГ не является единым платёжным rail.** География или валюта не подставляются эвристически. Для новой страны нужны официальный contract создания сессии, условия подключения, валюта и подтверждённый webhook/status contract, после чего добавляется самостоятельный adapter capability и regression-сценарий.

## Общий интерфейс

Контракт находится в `Ricis.Finance.Application/PaymentLaunch.cs`. Он не содержит HTTP, секретов, адресов провайдеров или хардкодинга банков.

| Тип | Назначение |
|---|---|
| `CreatePaymentLaunch` | Явный запрос: страна плательщика, `PaymentRail`, `Money`, заказ, разрешённые return/webhook URLs, key идемпотентности и test mode. |
| `IPaymentLaunchPort` | Injection boundary: адаптер создаёт provider payment session. |
| `PaymentRailRegistry` | Разрешает только зарегистрированный `country + rail + currency`; блокирует неявный CIS fallback и двусмысленную регистрацию. |
| `PaymentLaunchSession` | Не является payment fact. Содержит provider payment id, QR, selector form и/или provider-issued app deep links. |
| `PaymentHandoff` | Строго HTTPS GET/POST action плюс hidden fields; host может безопасно отрендерить форму или выполнить переход. |
| `BankApplicationOption` | Название/иконка и provider-returned platform deep links. Никакого локального справочника банков. |

## Подключение adapter

`Ricis.Finance.Bepaid` — отдельный infrastructure-проект. Он ссылается на `Application`, но обратной зависимости нет. Секрет передаётся host-приложением; он не должен попадать в журнал, исходный код или browser bundle.

```csharp
using Ricis.Finance.Application;
using Ricis.Finance.Bepaid;
using Ricis.Finance.Domain;

var adapter = new BepaidPaymentLaunchPort(
    httpClient,
    new BepaidOptions(
        shopId: configuration["Bepaid:ShopId"]!,
        secretKey: configuration["Bepaid:SecretKey"]!,
        eripServiceNumber: 12345678)); // Номер услуги подключённого merchant account.

var paymentLaunches = new CreatePaymentLaunchService(
    new PaymentRailRegistry([adapter]));
```

Перед вызовом host обязан проверять `returnUrl` и `notificationUrl` по собственному allow-list. Это не допускает open redirect и не позволяет клиентскому вводу заменить серверный endpoint подтверждений.

## Создание BY/ЕРИП-E-POS сессии

```csharp
var session = await paymentLaunches.HandleAsync(
    new CreatePaymentLaunch(
        payerCountryCode: "BY",
        rail: PaymentRail.BelarusEripEpos,
        amount: new Money(125.50m, "BYN"),
        orderReference: "order-1842",
        description: "Оплата счёта №1842",
        payerIpAddress: clientIpAddress,
        returnUrl: new Uri("https://merchant.example/payments/return"),
        notificationUrl: new Uri("https://merchant.example/payments/bepaid-webhook"),
        idempotencyKey: "payment-launch:order-1842",
        expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(15),
        test: true),
    cancellationToken);
```

Для desktop host отображает `session.QrCodeDataUri`. Для mobile host отображает `session.BankApplications`: пользователь выбирает банк, а UI открывает только link для текущей платформы. Для этого E-POS API response может не содержать единого selector URL, поэтому `session.Handoff` допускается `null`; host не подменяет это выдуманной ссылкой.

```csharp
var androidLink = session.BankApplications
    .Select(option => option.DeepLinks.GetValueOrDefault(MobilePlatform.Android))
    .FirstOrDefault(link => link is not null);

// Открывать URL только после явного выбора плательщика в UI.
```

## Создание RU/СБП сессии

```csharp
var session = await paymentLaunches.HandleAsync(
    new CreatePaymentLaunch(
        payerCountryCode: "RU",
        rail: PaymentRail.RussiaSbp,
        amount: new Money(500m, "RUB"),
        orderReference: "order-1843",
        description: "Оплата счёта №1843",
        payerIpAddress: clientIpAddress,
        returnUrl: new Uri("https://merchant.example/payments/return"),
        notificationUrl: new Uri("https://merchant.example/payments/bepaid-webhook"),
        idempotencyKey: "payment-launch:order-1843",
        expiresAtUtc: DateTimeOffset.UtcNow.AddMinutes(15),
        test: true),
    cancellationToken);

var handoff = session.Handoff
    ?? throw new InvalidOperationException("СБП provider обязан вернуть selector handoff.");
```

Host следует на `handoff.Action` методом `handoff.Method`; для `POST` добавляются только поля из `handoff.Fields`. bePaid возвращает `form.action`, а provider/НСПК владеет страницей QR и выбора банка. Следовательно, приложение не создаёт и не поддерживает собственный перечень российских банков.

## Подтверждение, а не возврат браузера

При запуске сессии adapter устанавливает provider header `RequestID` из `CreatePaymentLaunch.IdempotencyKey`. Для документированного bePaid alternative-payment API одинаковый `RequestID` повторяет один host-to-host запрос, а provider хранит ключ в течение 24 часов. [5]

После user handoff host принимает bePaid `POST` на `notificationUrl`, проверяет provider authentication и передаёт только валидное событие в существующий `IPaymentProviderWebhookVerifier` → `RecordProviderPaymentService`. bePaid документирует уведомления по смене статуса, включая `pending`, `expired`, `failed` и `successful`, и прямо требует проверять авторизационные данные webhook. [6] `returnUrl` используется только для UX — показать «ожидаем подтверждение», «успешно» или «не завершено» после server-side проверки.

## Проверки в репозитории

```bash
dotnet build Ricis.Core.sln --configuration Release
dotnet run --project Ricis.Finance/Ricis.Finance.RegressionTests/Ricis.Finance.RegressionTests.csproj --configuration Release
```

Regression suite включает следующие новые контракты: `FIN07` запрещает неявный CIS fallback; `FIN08` проверяет BYN E-POS request, `RequestID`, QR decode и bank deep links; `FIN09` проверяет RUB СБП request и provider-hosted НСПК handoff. Они используют локальный `HttpMessageHandler` и не создают реальный платёж.

## References

[1]: https://raschet.by/chastnym-litsam/oplata-po-qr-kodu/ "ЕРИП — Оплата по QR-коду"
[2]: https://bepaid.by/oplata-cherez-erip-i-e-pos-teper-bystree-i-prosche "bePaid — Оплата через ЕРИП и E-POS"
[3]: https://sbp.nspk.ru/ "Система быстрых платежей — официальный сайт"
[4]: https://docs.bepaid.by/ru/payment_methods/apms/sbp/ "bePaid API — Система быстрых платежей"
[5]: https://docs.bepaid.by/ru/using_api/idempotent_requests/ "bePaid API — Идемпотентные запросы"
[6]: https://docs.bepaid.by/ru/integration/apm_api/webhooks/ "bePaid API — Автоматические уведомления альтернативных способов оплаты"
