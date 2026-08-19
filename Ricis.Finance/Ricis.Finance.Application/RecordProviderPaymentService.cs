using Ricis.Finance.Domain;

namespace Ricis.Finance.Application;

/// <summary>Records one verified provider callback and derives compliant application artefacts exactly once.</summary>
public sealed class RecordProviderPaymentService
{
    private readonly IPaymentProviderWebhookVerifier _webhookVerifier;
    private readonly ISettlementRepository _settlements;
    private readonly IFxRateSource _fxRateSource;
    private readonly ITaxPolicy _taxPolicy;

    /// <summary>Creates the service from explicit external ports.</summary>
    public RecordProviderPaymentService(
        IPaymentProviderWebhookVerifier webhookVerifier,
        ISettlementRepository settlements,
        IFxRateSource fxRateSource,
        ITaxPolicy taxPolicy)
    {
        _webhookVerifier = webhookVerifier ?? throw new ArgumentNullException(nameof(webhookVerifier));
        _settlements = settlements ?? throw new ArgumentNullException(nameof(settlements));
        _fxRateSource = fxRateSource ?? throw new ArgumentNullException(nameof(fxRateSource));
        _taxPolicy = taxPolicy ?? throw new ArgumentNullException(nameof(taxPolicy));
    }

    /// <summary>
    /// Verifies, de-duplicates and records one provider payment event. The method
    /// creates a tax receipt candidate when policy requires it, but never submits
    /// that candidate to an external authority.
    /// </summary>
    public async ValueTask<RecordedProviderPayment> HandleAsync(ProviderWebhookPayload payload, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var verified = await _webhookVerifier.VerifyAsync(payload, cancellationToken).ConfigureAwait(false);
        var existing = await _settlements.FindByProviderEventIdAsync(verified.ProviderEventId, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return RecordedProviderPayment.FromExisting(existing, _taxPolicy.Version);
        }

        var payment = new ProviderPayment(
            Guid.NewGuid(),
            verified.ProviderEventId,
            verified.ProviderInvoiceId,
            verified.Gross,
            verified.CounterpartyKind,
            verified.ConfirmedAtUtc);
        var fxSnapshot = await _fxRateSource.GetSnapshotAsync(
            DateOnly.FromDateTime(payment.ConfirmedAtUtc.UtcDateTime),
            payment.Gross.Currency,
            "BYN",
            cancellationToken).ConfigureAwait(false);
        var settlement = new Settlement(Guid.NewGuid(), payment, verified.FeeBreakdown, fxSnapshot);
        await _settlements.StoreAsync(settlement, cancellationToken).ConfigureAwait(false);

        var decision = _taxPolicy.DecideReceipt(settlement);
        var candidate = decision.CreateCandidate
            ? new TaxReceiptCandidate(
                Guid.NewGuid(),
                settlement.Id,
                payment.Gross,
                fxSnapshot.Convert(payment.Gross),
                payment.CounterpartyKind,
                decision.TaxableEventAtUtc,
                _taxPolicy.Version)
            : null;
        return new RecordedProviderPayment(
            settlement,
            candidate,
            WasDuplicate: false,
            TaxPolicyReason: decision.Reason);
    }
}

/// <summary>Result of processing one inbound provider payment event.</summary>
public sealed record RecordedProviderPayment(
    Settlement Settlement,
    TaxReceiptCandidate? TaxReceiptCandidate,
    bool WasDuplicate,
    string TaxPolicyReason)
{
    internal static RecordedProviderPayment FromExisting(Settlement settlement, string policyVersion) => new(
        settlement,
        null,
            WasDuplicate: true,
            TaxPolicyReason: $"Повторное provider event проигнорировано; сохранённый settlement использует ранее применённую policy. Current policy: {policyVersion}.");
}
