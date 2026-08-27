using Ricis.Core.Resources;
namespace Ricis.Finance.Domain;

/// <summary>Tracks the lifecycle of a provider-side settlement independently from a bank payout.</summary>
public enum SettlementStatus
{
    Confirmed,
    Reconciled,
    FullyAllocated,
}

/// <summary>Tracks the lifecycle of one payout request to an external provider.</summary>
public enum PayoutStatus
{
    Pending,
    Submitted,
    Confirmed,
    Rejected,
}

/// <summary>
/// A provider balance settlement created from one confirmed provider payment.
/// It owns allocation state and cannot mutate the source payment fact.
/// </summary>
public sealed class Settlement
{
    private Money _allocated;

    /// <summary>Creates a confirmed settlement with its own fee and FX evidence.</summary>
    public Settlement(Guid id, ProviderPayment payment, FeeBreakdown fees, FxSnapshot fxSnapshot)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.e61c9c338f53"), nameof(id));
        }

        ArgumentNullException.ThrowIfNull(payment);
        ArgumentNullException.ThrowIfNull(fees);
        ArgumentNullException.ThrowIfNull(fxSnapshot);
        if (!StringComparer.Ordinal.Equals(payment.Gross.Currency, fees.Gross.Currency) || payment.Gross.Amount != fees.Gross.Amount)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.fcac71ec74a3"), nameof(fees));
        }

        Id = id;
        Payment = payment;
        Fees = fees;
        FxSnapshot = fxSnapshot;
        _allocated = Money.Zero(fees.Net.Currency);
        Status = SettlementStatus.Confirmed;
    }

    /// <summary>Gets the settlement identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the immutable underlying provider payment.</summary>
    public ProviderPayment Payment { get; }

    /// <summary>Gets separately recorded gross and fees.</summary>
    public FeeBreakdown Fees { get; }

    /// <summary>Gets the FX snapshot applicable to this settlement.</summary>
    public FxSnapshot FxSnapshot { get; }

    /// <summary>Gets the remaining provider-balance amount eligible for payout allocation.</summary>
    public Money AvailableToAllocate => Fees.Net.Subtract(_allocated);

    /// <summary>Gets the cumulative amount reserved by payout requests.</summary>
    public Money Allocated => _allocated;

    /// <summary>Gets the settlement lifecycle state.</summary>
    public SettlementStatus Status { get; private set; }

    /// <summary>Reserves a payout amount without executing an external transfer.</summary>
    public PayoutRequest ReservePayout(Guid payoutId, string idempotencyKey, Money amount, DateTimeOffset requestedAtUtc)
    {
        if (Status == SettlementStatus.FullyAllocated)
        {
            throw new InvalidOperationException(RicisLegacyTextResources.Get("runtime.legacy.df1220f79005"));
        }

        if (!StringComparer.Ordinal.Equals(amount.Currency, AvailableToAllocate.Currency))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.71c4982a761a"), nameof(amount));
        }

        if (amount.Amount == 0m || amount.Amount > AvailableToAllocate.Amount)
        {
            throw new InvalidOperationException(RicisLegacyTextResources.Get("runtime.legacy.7d1796d35ea3"));
        }

        _allocated = _allocated.Add(amount);
        if (AvailableToAllocate.Amount == 0m)
        {
            Status = SettlementStatus.FullyAllocated;
        }

        return new PayoutRequest(payoutId, Id, idempotencyKey, amount, requestedAtUtc);
    }
}

/// <summary>Represents a requested external payout; confirmation is driven by a verified provider event.</summary>
public sealed class PayoutRequest
{
    /// <summary>Creates a pending payout request from an allocated settlement amount.</summary>
    public PayoutRequest(Guid id, Guid settlementId, string idempotencyKey, Money requestedAmount, DateTimeOffset requestedAtUtc)
    {
        if (id == Guid.Empty || settlementId == Guid.Empty)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.9a7899f07ab4"));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.89d28f4bd8c4"), nameof(idempotencyKey));
        }

        if (requestedAmount.Amount == 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(requestedAmount), RicisLegacyTextResources.Get("runtime.legacy.a5bb27f674c9"));
        }

        Id = id;
        SettlementId = settlementId;
        IdempotencyKey = idempotencyKey.Trim();
        RequestedAmount = requestedAmount;
        RequestedAtUtc = requestedAtUtc.ToUniversalTime();
        Status = PayoutStatus.Pending;
    }

    /// <summary>Gets the payout aggregate identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the source settlement identifier.</summary>
    public Guid SettlementId { get; }

    /// <summary>Gets the caller supplied idempotency key.</summary>
    public string IdempotencyKey { get; }

    /// <summary>Gets the amount reserved at request time.</summary>
    public Money RequestedAmount { get; }

    /// <summary>Gets the UTC request instant.</summary>
    public DateTimeOffset RequestedAtUtc { get; }

    /// <summary>Gets the current payout status.</summary>
    public PayoutStatus Status { get; private set; }

    /// <summary>Gets the verified provider payout identifier after submission or confirmation.</summary>
    public string? ProviderPayoutId { get; private set; }

    /// <summary>Gets the actual bank fee when confirmed.</summary>
    public Money? ActualBankFee { get; private set; }

    /// <summary>Marks the request submitted with a provider identifier.</summary>
    public void MarkSubmitted(string providerPayoutId)
    {
        EnsureStatus(PayoutStatus.Pending);
        ProviderPayoutId = RequireProviderPayoutId(providerPayoutId);
        Status = PayoutStatus.Submitted;
    }

    /// <summary>Marks a submitted payout confirmed with its actual bank fee.</summary>
    public void Confirm(Money actualBankFee)
    {
        EnsureStatus(PayoutStatus.Submitted);
        if (!StringComparer.Ordinal.Equals(actualBankFee.Currency, RequestedAmount.Currency) || actualBankFee.Amount > RequestedAmount.Amount)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.4b5fefbc699e"), nameof(actualBankFee));
        }

        ActualBankFee = actualBankFee;
        Status = PayoutStatus.Confirmed;
    }

    /// <summary>Marks a submitted request rejected by the external provider.</summary>
    public void Reject()
    {
        EnsureStatus(PayoutStatus.Submitted);
        Status = PayoutStatus.Rejected;
    }

    private static string RequireProviderPayoutId(string value) => string.IsNullOrWhiteSpace(value)
        ? throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.c9b4319a1bac"), nameof(value))
        : value.Trim();

    private void EnsureStatus(PayoutStatus expected)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException(RicisLegacyTextResources.Format("runtime.legacy.6b6a7f32c55d", ("expected", expected), ("Status", Status)));
        }
    }
}
