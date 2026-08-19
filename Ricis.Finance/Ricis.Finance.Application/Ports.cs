using Ricis.Finance.Domain;

namespace Ricis.Finance.Application;

/// <summary>Verifies and parses a provider callback before it reaches the application workflow.</summary>
public interface IPaymentProviderWebhookVerifier
{
    /// <summary>Returns a verified provider payment fact or rejects an invalid callback.</summary>
    ValueTask<VerifiedProviderPayment> VerifyAsync(ProviderWebhookPayload payload, CancellationToken cancellationToken);
}

/// <summary>Executes or queries provider payout operations outside the domain model.</summary>
public interface IPaymentProviderPort
{
    /// <summary>Submits an authorised payout request with the caller's idempotency key.</summary>
    ValueTask<ProviderPayoutSubmission> SubmitPayoutAsync(PayoutRequest request, CancellationToken cancellationToken);
}

/// <summary>Provides an auditable FX snapshot for a requested date and currency pair.</summary>
public interface IFxRateSource
{
    /// <summary>Returns the effective-date FX evidence required by a settlement.</summary>
    ValueTask<FxSnapshot> GetSnapshotAsync(DateOnly effectiveDate, string sourceCurrency, string targetCurrency, CancellationToken cancellationToken);
}

/// <summary>Calculates tax treatment, threshold state and receipt timing from versioned rules.</summary>
public interface ITaxPolicy
{
    /// <summary>Gets the immutable policy version used by the current decision.</summary>
    string Version { get; }

    /// <summary>Determines whether and when a settlement creates a receipt candidate.</summary>
    TaxReceiptDecision DecideReceipt(Settlement settlement);

    /// <summary>Evaluates one counterparty partition for a calendar year.</summary>
    AnnualTaxPosition EvaluateAnnualPosition(int taxYear, CounterpartyKind counterpartyKind, Money taxableIncomeByN);
}

/// <summary>Authorises a payout release independently from the tax receipt event.</summary>
public interface IPayoutReleasePolicy
{
    /// <summary>Returns an explicit decision for the requested provider-balance amount.</summary>
    PayoutReleaseDecision Decide(Settlement settlement, Money requestedAmount);
}

/// <summary>Supplies effective-dated bank fees for a declared payout route.</summary>
public interface IBankFeeSchedule
{
    /// <summary>Returns the applicable bank fee for an already-authorised payout.</summary>
    ValueTask<Money> QuoteAsync(PayoutRequest request, DateOnly effectiveDate, CancellationToken cancellationToken);
}

/// <summary>Submits an already-reviewed tax receipt candidate through an authorised external route.</summary>
public interface ITaxReceiptGateway
{
    /// <summary>Submits a candidate exactly once through the adapter's supported channel.</summary>
    ValueTask<TaxReceiptSubmission> SubmitAsync(TaxReceiptCandidate candidate, CancellationToken cancellationToken);
}

/// <summary>Stores and retrieves settlements with provider-event idempotency.</summary>
public interface ISettlementRepository
{
    /// <summary>Finds a settlement by its internal aggregate identifier.</summary>
    ValueTask<Settlement?> FindByIdAsync(Guid settlementId, CancellationToken cancellationToken);

    /// <summary>Finds a settlement previously stored for a provider event identifier.</summary>
    ValueTask<Settlement?> FindByProviderEventIdAsync(string providerEventId, CancellationToken cancellationToken);

    /// <summary>Persists a new settlement atomically with its provider-event key.</summary>
    ValueTask StoreAsync(Settlement settlement, CancellationToken cancellationToken);
}

/// <summary>Stores payout lifecycle transitions keyed by application and provider identifiers.</summary>
public interface IPayoutRepository
{
    /// <summary>Finds a payout by caller-supplied idempotency key.</summary>
    ValueTask<PayoutRequest?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>Persists the current payout state.</summary>
    ValueTask StoreAsync(PayoutRequest payout, CancellationToken cancellationToken);
}

/// <summary>Stores invoice aggregates and their caller-side lifecycle idempotency keys.</summary>
public interface IInvoiceRepository
{
    /// <summary>Finds an invoice by its aggregate identifier.</summary>
    ValueTask<Invoice?> FindByIdAsync(Guid invoiceId, CancellationToken cancellationToken);

    /// <summary>Finds an invoice by the merchant-owned auditable order reference.</summary>
    ValueTask<Invoice?> FindByOrderReferenceAsync(string orderReference, CancellationToken cancellationToken);

    /// <summary>Finds an invoice previously issued with the supplied idempotency key.</summary>
    ValueTask<Invoice?> FindByIssueIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>Persists a new or changed invoice aggregate.</summary>
    ValueTask StoreAsync(Invoice invoice, CancellationToken cancellationToken);
}

/// <summary>Stores provider launch evidence separately from the invoice money fact.</summary>
public interface IInvoiceLaunchRepository
{
    /// <summary>Finds a launch by its caller-supplied idempotency key.</summary>
    ValueTask<InvoiceLaunchRecord?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);

    /// <summary>Persists the provider-created launch evidence.</summary>
    ValueTask StoreAsync(InvoiceLaunchRecord launch, CancellationToken cancellationToken);
}

/// <summary>Provider launch evidence linked to one invoice; it is not payment confirmation.</summary>
public sealed record InvoiceLaunchRecord(
    Guid InvoiceId,
    string IdempotencyKey,
    PaymentLaunchSession Session);

/// <summary>Supplies deterministic UTC time to the application layer.</summary>
public interface IClock
{
    /// <summary>Gets the current instant in UTC.</summary>
    DateTimeOffset UtcNow { get; }
}

/// <summary>Opaque raw provider callback received by the host application.</summary>
public sealed record ProviderWebhookPayload(string Signature, string Body, IReadOnlyDictionary<string, string> Headers);

/// <summary>Verified provider payment data supplied by a provider-specific adapter.</summary>
public sealed record VerifiedProviderPayment(
    string ProviderEventId,
    string ProviderInvoiceId,
    Money Gross,
    CounterpartyKind CounterpartyKind,
    DateTimeOffset ConfirmedAtUtc,
    FeeBreakdown FeeBreakdown);

/// <summary>Provider response to a payout submission; it is not a bank-confirmation event.</summary>
public sealed record ProviderPayoutSubmission(string ProviderPayoutId);

/// <summary>Result of applying policy to a settlement before any receipt submission.</summary>
public sealed record TaxReceiptDecision(bool CreateCandidate, DateTimeOffset TaxableEventAtUtc, string Reason);

/// <summary>Result of a provider-balance payout release policy evaluation.</summary>
public sealed record PayoutReleaseDecision(bool IsAllowed, string Reason);

/// <summary>External receipt submission response captured by an adapter.</summary>
public sealed record TaxReceiptSubmission(string ExternalReceiptId, DateTimeOffset SubmittedAtUtc);
