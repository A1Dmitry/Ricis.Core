using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Ricis.Finance.Application;
using Ricis.Finance.Bepaid;
using Ricis.Finance.Domain;

var tests = new (string Name, Func<Task> Body)[]
{
    ("FIN01: Money запрещает смешение валют", MoneyRejectsCurrencyMixing),
    ("FIN02: FeeBreakdown сохраняет gross, комиссии и net раздельно", FeeBreakdownSeparatesFacts),
    ("FIN03: Provider webhook идемпотентен и создаёт receipt candidate по payment event", ProviderPaymentIsIdempotentAndTaxable),
    ("FIN04: Payout резервирует только доступный остаток и отправляется один раз", PayoutIsAuthorisedAndIdempotent),
    ("FIN05: Payout policy блокирует неразрешённый release без вызова provider", PayoutPolicyBlocksProviderCall),
    ("FIN06: Threshold status принадлежит policy и не является глобальным лимитом", TaxPositionIsPolicyOwned),
    ("FIN07: Payment launch registry не допускает неявный CIS fallback", PaymentLaunchRegistryRequiresExplicitCapability),
    ("FIN08: bePaid ЕРИП launch передаёт RequestID и возвращает provider-issued bank deep links", BepaidEripLaunchMapsProviderResponse),
    ("FIN09: bePaid СБП launch возвращает provider-hosted selector и не делает возврат подтверждением", BepaidSbpLaunchMapsProviderHandoff),
};

var failures = 0;
foreach (var (name, body) in tests)
{
    try
    {
        await body();
        Console.WriteLine($"PASS: {name}");
    }
    catch (Exception error)
    {
        failures++;
        Console.WriteLine($"FAIL: {name}\n  {error}");
    }
}

if (failures > 0)
{
    Console.Error.WriteLine($"{failures} finance regression test(s) failed.");
    return 1;
}

Console.WriteLine($"All {tests.Length} finance regression tests passed.");
return 0;

static Task MoneyRejectsCurrencyMixing()
{
    RequireThrows<InvalidOperationException>(() => _ = new Money(1m, "USD").Add(new Money(1m, "EUR")));
    RequireThrows<ArgumentOutOfRangeException>(() => _ = new Money(-0.01m, "USD"));
    return Task.CompletedTask;
}

static Task FeeBreakdownSeparatesFacts()
{
    var fees = new FeeBreakdown(new Money(100m, "USD"), new Money(6m, "USD"), new Money(3m, "USD"));
    Require(fees.Gross.Amount == 100m && fees.ProviderFee.Amount == 6m && fees.BankFee.Amount == 3m && fees.Net.Amount == 91m,
        "Fee breakdown обязан хранить gross, обе комиссии и net без смешения.");
    return Task.CompletedTask;
}

static async Task ProviderPaymentIsIdempotentAndTaxable()
{
    var timestamp = new DateTimeOffset(2026, 8, 19, 8, 0, 0, TimeSpan.Zero);
    var repositories = new InMemoryRepositories();
    var service = new RecordProviderPaymentService(
        new StubWebhookVerifier(new VerifiedProviderPayment(
            "evt-001",
            "invoice-001",
            new Money(100m, "USD"),
            CounterpartyKind.ForeignBusiness,
            timestamp,
            new FeeBreakdown(new Money(100m, "USD"), new Money(6m, "USD"), Money.Zero("USD")))),
        repositories,
        new StubFxRateSource(new FxSnapshot("NBRB-test", new DateOnly(2026, 8, 19), "USD", "BYN", 3.20m)),
        new StubTaxPolicy("tax-policy/2026-08", createCandidate: true));

    var payload = new ProviderWebhookPayload("signature", "body", new Dictionary<string, string>());
    var first = await service.HandleAsync(payload, CancellationToken.None);
    var duplicate = await service.HandleAsync(payload, CancellationToken.None);

    Require(!first.WasDuplicate && first.TaxReceiptCandidate is not null && first.TaxReceiptCandidate.GrossInByN.Amount == 320m,
        "Первый verified provider payment обязан создать candidate из gross payment и FX snapshot.");
    Require(duplicate.WasDuplicate && repositories.StoredSettlementCount == 1,
        "Повторный webhook с тем же provider event id не должен создавать второй settlement.");
}

static async Task PayoutIsAuthorisedAndIdempotent()
{
    var repositories = new InMemoryRepositories();
    var settlement = CreateSettlement();
    await repositories.StoreAsync(settlement, CancellationToken.None);
    var provider = new StubProviderPort("payout-001");
    var service = new RequestPayoutService(
        repositories,
        repositories,
        new StubPayoutReleasePolicy(isAllowed: true),
        provider,
        new StubClock(new DateTimeOffset(2026, 8, 19, 9, 0, 0, TimeSpan.Zero)));
    var command = new RequestPayout(settlement.Id, "request-001", new Money(50m, "USD"));

    var first = await service.HandleAsync(command, CancellationToken.None);
    var duplicate = await service.HandleAsync(command, CancellationToken.None);

    Require(first.Status == PayoutStatus.Submitted && first.ProviderPayoutId == "payout-001" && provider.CallCount == 1,
        "Разрешённый payout должен быть отправлен provider один раз.");
    Require(ReferenceEquals(first, duplicate) && settlement.AvailableToAllocate.Amount == 44m,
        "Идемпотентный payout должен вернуть сохранённый aggregate и не резервировать сумму повторно.");
}

static async Task PayoutPolicyBlocksProviderCall()
{
    var repositories = new InMemoryRepositories();
    var settlement = CreateSettlement();
    await repositories.StoreAsync(settlement, CancellationToken.None);
    var provider = new StubProviderPort("must-not-submit");
    var service = new RequestPayoutService(
        repositories,
        repositories,
        new StubPayoutReleasePolicy(isAllowed: false),
        provider,
        new StubClock(DateTimeOffset.UtcNow));

    await RequireThrowsAsync<InvalidOperationException>(() => service.HandleAsync(
        new RequestPayout(settlement.Id, "blocked-001", new Money(1m, "USD")),
        CancellationToken.None).AsTask());
    Require(provider.CallCount == 0 && settlement.AvailableToAllocate.Amount == 94m,
        "Отклонённый policy payout не должен вызывать provider или менять баланс settlement.");
}

static Task TaxPositionIsPolicyOwned()
{
    var policy = new StubTaxPolicy("tax-policy/2026-08", createCandidate: true);
    var position = policy.EvaluateAnnualPosition(2026, CounterpartyKind.BelarusRegisteredBusiness, new Money(48_000m, "BYN"));
    Require(position.Status == TaxThresholdStatus.Warning && position.CounterpartyKind == CounterpartyKind.BelarusRegisteredBusiness,
        "Пороговая оценка должна возвращаться политикой для конкретной категории контрагента.");
    return Task.CompletedTask;
}

static async Task PaymentLaunchRegistryRequiresExplicitCapability()
{
    var provider = new StubPaymentLaunchPort(
        [new PaymentRailCapability("BY", PaymentRail.BelarusEripEpos, ["BYN"])],
        new PaymentLaunchSession(
            "test-provider",
            "session-001",
            PaymentRail.BelarusEripEpos,
            new Money(10m, "BYN"),
            new PaymentHandoff(new Uri("https://provider.example/select"), PaymentHandoffMethod.Get),
            null,
            null,
            null));
    var registry = new PaymentRailRegistry([provider]);
    var service = new CreatePaymentLaunchService(registry);

    var result = await service.HandleAsync(CreateLaunch("BY", PaymentRail.BelarusEripEpos, new Money(10m, "BYN")), CancellationToken.None);
    Require(result.ProviderPaymentId == "session-001" && provider.CallCount == 1,
        "Явно поддержанный BY/ЕРИП/BYN launch обязан быть направлен в зарегистрированный adapter.");
    RequireThrows<NotSupportedException>(() => _ = registry.Resolve(
        CreateLaunch("KZ", PaymentRail.BelarusEripEpos, new Money(10m, "BYN"))));
    RequireThrows<NotSupportedException>(() => _ = registry.Resolve(
        CreateLaunch("BY", PaymentRail.BelarusEripEpos, new Money(10m, "RUB"))));
}

static async Task BepaidEripLaunchMapsProviderResponse()
{
    const string qrPayload = "0002010123";
    const string qrPayloadBase64 = "MDAwMjAxMDEyMw==";
    const string responseJson = """
        {
          "transaction": {
            "uid": "erip-session-001",
            "expired_at": "2026-08-20T08:00:00Z",
            "erip": {
              "qr_code_raw": "MDAwMjAxMDEyMw==",
              "qr_code": "data:image/png;base64,aW1hZ2U=",
              "banks": [
                {
                  "name": "Тестовый Банк",
                  "icon": "PHN2Zz48L3N2Zz4=",
                  "platform_urls": {
                    "ios": "https://ios.bank.example/pay?payload=",
                    "android": "https://android.bank.example/pay?payload=",
                    "huaweiapp": "https://huawei.bank.example/pay?payload="
                  }
                }
              ]
            }
          }
        }
        """;
    var handler = new RecordingHttpHandler(HttpStatusCode.OK, responseJson);
    using var httpClient = new HttpClient(handler);
    var provider = new BepaidPaymentLaunchPort(
        httpClient,
        new BepaidOptions("shop-001", "secret-001", eripServiceNumber: 77, apiBaseUri: new Uri("https://api.test.example/")));

    var session = await provider.CreateAsync(
        CreateLaunch("BY", PaymentRail.BelarusEripEpos, new Money(123.45m, "BYN"), idempotencyKey: "launch-001"),
        CancellationToken.None);

    Require(handler.RequestUri == "https://api.test.example/beyag/payments" && handler.Method == HttpMethod.Post,
        "ЕРИП adapter обязан вызывать документированный bePaid endpoint создания счёта.");
    Require(handler.RequestId == "launch-001" && handler.Authorization?.Scheme == "Basic" &&
            Encoding.UTF8.GetString(Convert.FromBase64String(handler.Authorization.Parameter!)) == "shop-001:secret-001",
        "ЕРИП adapter обязан передать provider RequestID и Basic credentials из host configuration.");
    Require(handler.Body.Contains("\"amount\":12345", StringComparison.Ordinal) &&
            handler.Body.Contains("\"currency\":\"BYN\"", StringComparison.Ordinal) &&
            handler.Body.Contains("\"account_number\":\"order-001\"", StringComparison.Ordinal) &&
            handler.Body.Contains("\"service_no\":77", StringComparison.Ordinal),
        "ЕРИП adapter обязан сериализовать сумму в копейках, BYN, account number и configured service number.");
    Require(session.ProviderPaymentId == "erip-session-001" && session.Handoff is null && session.QrCodeDataUri == "data:image/png;base64,aW1hZ2U=",
        "ЕРИП response обязан сохранить provider session id, QR image и не выдумывать невыданный selector URI.");
    Require(session.BankApplications.Count == 1 &&
            session.BankApplications[0].DeepLinks[MobilePlatform.Android].AbsoluteUri == $"https://android.bank.example/pay?payload={qrPayload}" &&
            session.BankApplications[0].IconDataUri == "data:image/svg+xml;base64,PHN2Zz48L3N2Zz4=",
        "ЕРИП adapter обязан строить bank deep link из provider prefix и Base64-декодированного QR payload.");
    Require(qrPayloadBase64 == Convert.ToBase64String(Encoding.UTF8.GetBytes(qrPayload)), "Тестовый QR payload должен оставаться корректным Base64 evidence.");
}

static async Task BepaidSbpLaunchMapsProviderHandoff()
{
    const string responseJson = """
        {
          "transaction": {
            "uid": "sbp-session-001",
            "expired_at": "2026-08-20T08:00:00Z",
            "form": {
              "action": "https://qr.nspk.ru/AD100000TEST",
              "method": "GET",
              "fields": []
            }
          }
        }
        """;
    var handler = new RecordingHttpHandler(HttpStatusCode.OK, responseJson);
    using var httpClient = new HttpClient(handler);
    var provider = new BepaidPaymentLaunchPort(
        httpClient,
        new BepaidOptions("shop-001", "secret-001", apiBaseUri: new Uri("https://api.test.example/")));

    var session = await provider.CreateAsync(
        CreateLaunch("RU", PaymentRail.RussiaSbp, new Money(500m, "RUB"), idempotencyKey: "sbp-launch-001"),
        CancellationToken.None);

    Require(handler.RequestUri == "https://api.test.example/beyag/transactions/payments" && handler.RequestId == "sbp-launch-001",
        "СБП adapter обязан использовать документированный alternative-payment endpoint и RequestID.");
    Require(handler.Body.Contains("\"amount\":50000", StringComparison.Ordinal) &&
            handler.Body.Contains("\"currency\":\"RUB\"", StringComparison.Ordinal) &&
            handler.Body.Contains("\"method\":{\"type\":\"sbp\"}", StringComparison.Ordinal) &&
            handler.Body.Contains("\"return_url\":\"https://merchant.example/return\"", StringComparison.Ordinal) &&
            handler.Body.Contains("\"notification_url\":\"https://merchant.example/webhook\"", StringComparison.Ordinal),
        "СБП adapter обязан передать RUB сумму в копейках, method=sbp, return и notification URL.");
    Require(session.ProviderPaymentId == "sbp-session-001" && session.Handoff is not null &&
            session.Handoff.Action.AbsoluteUri == "https://qr.nspk.ru/AD100000TEST" &&
            session.Handoff.Method == PaymentHandoffMethod.Get && session.BankApplications.Count == 0 && session.QrCodeDataUri is null,
        "СБП launch обязан вернуть provider-owned selector URL, а не самостоятельно сгенерированный bank deep link или факт оплаты.");
    Require(provider.Capabilities.Any(capability => capability.Supports("RU", PaymentRail.RussiaSbp, new Money(1m, "RUB"))) &&
            !provider.Capabilities.Any(capability => capability.Supports("KZ", PaymentRail.RussiaSbp, new Money(1m, "RUB"))),
        "СБП capability должна быть явной только для RU/RUB; прочий СНГ не должен быть fallback-маршрутом.");
}

static CreatePaymentLaunch CreateLaunch(string country, PaymentRail rail, Money amount, string idempotencyKey = "launch-001") =>
    new(
        country,
        rail,
        amount,
        "order-001",
        "Оплата заказа #1",
        "127.0.0.1",
        new Uri("https://merchant.example/return"),
        new Uri("https://merchant.example/webhook"),
        idempotencyKey,
        new DateTimeOffset(2026, 8, 20, 8, 0, 0, TimeSpan.Zero),
        test: true);

static Settlement CreateSettlement()
{
    var payment = new ProviderPayment(
        Guid.NewGuid(),
        $"evt-{Guid.NewGuid():N}",
        "invoice-payout",
        new Money(100m, "USD"),
        CounterpartyKind.ForeignBusiness,
        new DateTimeOffset(2026, 8, 19, 8, 0, 0, TimeSpan.Zero));
    return new Settlement(
        Guid.NewGuid(),
        payment,
        new FeeBreakdown(payment.Gross, new Money(6m, "USD"), Money.Zero("USD")),
        new FxSnapshot("NBRB-test", new DateOnly(2026, 8, 19), "USD", "BYN", 3.20m));
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void RequireThrows<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
        throw new InvalidOperationException($"Ожидалось исключение {typeof(TException).Name}.");
    }
    catch (TException)
    {
    }
}

static async Task RequireThrowsAsync<TException>(Func<Task> action)
    where TException : Exception
{
    try
    {
        await action();
        throw new InvalidOperationException($"Ожидалось исключение {typeof(TException).Name}.");
    }
    catch (TException)
    {
    }
}

sealed class InMemoryRepositories : ISettlementRepository, IPayoutRepository
{
    private readonly Dictionary<Guid, Settlement> _settlementsById = [];
    private readonly Dictionary<string, Settlement> _settlementsByEventId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, PayoutRequest> _payouts = new(StringComparer.Ordinal);

    public int StoredSettlementCount => _settlementsById.Count;

    public ValueTask<Settlement?> FindByIdAsync(Guid settlementId, CancellationToken cancellationToken) =>
        ValueTask.FromResult(_settlementsById.GetValueOrDefault(settlementId));

    public ValueTask<Settlement?> FindByProviderEventIdAsync(string providerEventId, CancellationToken cancellationToken) =>
        ValueTask.FromResult(_settlementsByEventId.GetValueOrDefault(providerEventId));

    public ValueTask StoreAsync(Settlement settlement, CancellationToken cancellationToken)
    {
        _settlementsById[settlement.Id] = settlement;
        _settlementsByEventId[settlement.Payment.ProviderEventId] = settlement;
        return ValueTask.CompletedTask;
    }

    public ValueTask<PayoutRequest?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        ValueTask.FromResult(_payouts.GetValueOrDefault(idempotencyKey));

    public ValueTask StoreAsync(PayoutRequest payout, CancellationToken cancellationToken)
    {
        _payouts[payout.IdempotencyKey] = payout;
        return ValueTask.CompletedTask;
    }
}

sealed class StubWebhookVerifier(VerifiedProviderPayment verified) : IPaymentProviderWebhookVerifier
{
    public ValueTask<VerifiedProviderPayment> VerifyAsync(ProviderWebhookPayload payload, CancellationToken cancellationToken) => ValueTask.FromResult(verified);
}

sealed class StubFxRateSource(FxSnapshot snapshot) : IFxRateSource
{
    public ValueTask<FxSnapshot> GetSnapshotAsync(DateOnly effectiveDate, string sourceCurrency, string targetCurrency, CancellationToken cancellationToken) => ValueTask.FromResult(snapshot);
}

sealed class StubTaxPolicy(string version, bool createCandidate) : ITaxPolicy
{
    public string Version => version;

    public TaxReceiptDecision DecideReceipt(Settlement settlement) => new(createCandidate, settlement.Payment.ConfirmedAtUtc, "Confirmed settlement is policy-taxable in this test.");

    public AnnualTaxPosition EvaluateAnnualPosition(int taxYear, CounterpartyKind counterpartyKind, Money taxableIncomeByN) =>
        new(taxYear, counterpartyKind, taxableIncomeByN, TaxThresholdStatus.Warning, version);
}

sealed class StubPayoutReleasePolicy(bool isAllowed) : IPayoutReleasePolicy
{
    public PayoutReleaseDecision Decide(Settlement settlement, Money requestedAmount) => new(isAllowed, isAllowed ? "Allowed by test policy." : "Manual compliance review required.");
}

sealed class StubProviderPort(string providerPayoutId) : IPaymentProviderPort
{
    public int CallCount { get; private set; }

    public ValueTask<ProviderPayoutSubmission> SubmitPayoutAsync(PayoutRequest request, CancellationToken cancellationToken)
    {
        CallCount++;
        return ValueTask.FromResult(new ProviderPayoutSubmission(providerPayoutId));
    }
}

sealed class StubClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow => now;
}

sealed class StubPaymentLaunchPort(IReadOnlyCollection<PaymentRailCapability> capabilities, PaymentLaunchSession session) : IPaymentLaunchPort
{
    public int CallCount { get; private set; }

    public IReadOnlyCollection<PaymentRailCapability> Capabilities => capabilities;

    public ValueTask<PaymentLaunchSession> CreateAsync(CreatePaymentLaunch request, CancellationToken cancellationToken)
    {
        CallCount++;
        return ValueTask.FromResult(session);
    }
}

sealed class RecordingHttpHandler(HttpStatusCode statusCode, string responseJson) : HttpMessageHandler
{
    public HttpMethod? Method { get; private set; }

    public string? RequestUri { get; private set; }

    public string Body { get; private set; } = string.Empty;

    public string? RequestId { get; private set; }

    public AuthenticationHeaderValue? Authorization { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Method = request.Method;
        RequestUri = request.RequestUri?.AbsoluteUri;
        Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
        RequestId = request.Headers.TryGetValues("RequestID", out var values) ? values.SingleOrDefault() : null;
        Authorization = request.Headers.Authorization;
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
        };
    }
}
