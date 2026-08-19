using Ricis.Finance.Application;
using Ricis.Finance.Domain;

var tests = new (string Name, Func<Task> Body)[]
{
    ("FIN01: Money запрещает смешение валют", MoneyRejectsCurrencyMixing),
    ("FIN02: FeeBreakdown сохраняет gross, комиссии и net раздельно", FeeBreakdownSeparatesFacts),
    ("FIN03: Provider webhook идемпотентен и создаёт receipt candidate по payment event", ProviderPaymentIsIdempotentAndTaxable),
    ("FIN04: Payout резервирует только доступный остаток и отправляется один раз", PayoutIsAuthorisedAndIdempotent),
    ("FIN05: Payout policy блокирует неразрешённый release без вызова provider", PayoutPolicyBlocksProviderCall),
    ("FIN06: Threshold status принадлежит policy и не является глобальным лимитом", TaxPositionIsPolicyOwned),
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
