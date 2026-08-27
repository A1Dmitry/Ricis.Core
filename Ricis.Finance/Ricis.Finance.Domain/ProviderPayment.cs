using Ricis.Core.Resources;
namespace Ricis.Finance.Domain;

/// <summary>Classifies the payer for tax-policy routing without embedding a tax rate in the payment aggregate.</summary>
public enum CounterpartyKind
{
    Individual,
    ForeignBusiness,
    BelarusRegisteredBusiness,
}

/// <summary>Represents the provider-confirmed incoming payment event.</summary>
public sealed class ProviderPayment
{
    /// <summary>Creates an immutable confirmed payment fact from a verified provider event.</summary>
    public ProviderPayment(
        Guid id,
        string providerEventId,
        string providerInvoiceId,
        Money gross,
        CounterpartyKind counterpartyKind,
        DateTimeOffset confirmedAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.03288e992004"), nameof(id));
        }

        Id = id;
        ProviderEventId = RequireIdentifier(providerEventId, nameof(providerEventId));
        ProviderInvoiceId = RequireIdentifier(providerInvoiceId, nameof(providerInvoiceId));
        Gross = gross;
        CounterpartyKind = counterpartyKind;
        ConfirmedAtUtc = confirmedAtUtc.ToUniversalTime();
    }

    /// <summary>Gets the internal immutable aggregate identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the provider event identifier used as the idempotency key.</summary>
    public string ProviderEventId { get; }

    /// <summary>Gets the provider invoice identifier used for reconciliation.</summary>
    public string ProviderInvoiceId { get; }

    /// <summary>Gets the gross incoming payment before provider or bank fees.</summary>
    public Money Gross { get; }

    /// <summary>Gets the classification required by a tax policy.</summary>
    public CounterpartyKind CounterpartyKind { get; }

    /// <summary>Gets the provider-confirmed instant, normalized to UTC.</summary>
    public DateTimeOffset ConfirmedAtUtc { get; }

    private static string RequireIdentifier(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.63793260a01a"), parameterName);
        }

        return value.Trim();
    }
}

/// <summary>Captures an auditable FX rate used for a single conversion, never a live recalculation.</summary>
public sealed record FxSnapshot
{
    /// <summary>Creates a single-source conversion snapshot.</summary>
    public FxSnapshot(
        string source,
        DateOnly effectiveDate,
        string sourceCurrency,
        string targetCurrency,
        decimal targetPerSource)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.f34afca7412f"), nameof(source));
        }

        if (targetPerSource <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(targetPerSource), targetPerSource, RicisLegacyTextResources.Get("runtime.legacy.06ebf24b94d9"));
        }

        Source = source.Trim();
        EffectiveDate = effectiveDate;
        SourceCurrency = NormalizeCurrency(sourceCurrency);
        TargetCurrency = NormalizeCurrency(targetCurrency);
        TargetPerSource = targetPerSource;
    }

    /// <summary>Gets the published rate source identifier.</summary>
    public string Source { get; }

    /// <summary>Gets the date to which this rate applies.</summary>
    public DateOnly EffectiveDate { get; }

    /// <summary>Gets the original currency.</summary>
    public string SourceCurrency { get; }

    /// <summary>Gets the converted currency.</summary>
    public string TargetCurrency { get; }

    /// <summary>Gets target-currency units per one source-currency unit.</summary>
    public decimal TargetPerSource { get; }

    /// <summary>Converts an amount only when its source currency matches this snapshot.</summary>
    public Money Convert(Money source) => StringComparer.Ordinal.Equals(source.Currency, SourceCurrency)
        ? new Money(source.Amount * TargetPerSource, TargetCurrency)
        : throw new InvalidOperationException(RicisLegacyTextResources.Get("runtime.legacy.5fdd1f9475be"));

    private static string NormalizeCurrency(string value) => new Money(0m, value).Currency;
}
