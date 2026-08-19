namespace Ricis.Finance.Domain;

/// <summary>Describes the review level produced by an effective-dated tax policy.</summary>
public enum TaxThresholdStatus
{
    Normal,
    Warning,
    ReviewRequired,
}

/// <summary>Provides the tax-relevant evidence associated with a confirmed settlement.</summary>
public sealed record TaxReceiptCandidate
{
    /// <summary>Creates a candidate receipt without submitting it to a tax authority.</summary>
    public TaxReceiptCandidate(
        Guid id,
        Guid settlementId,
        Money grossInOriginalCurrency,
        Money grossInByN,
        CounterpartyKind counterpartyKind,
        DateTimeOffset taxableEventAtUtc,
        string policyVersion)
    {
        if (id == Guid.Empty || settlementId == Guid.Empty)
        {
            throw new ArgumentException("Идентификаторы кандидата и settlement обязательны.");
        }

        if (!StringComparer.Ordinal.Equals(grossInByN.Currency, "BYN"))
        {
            throw new ArgumentException("Налоговая BYN-сумма должна иметь валюту BYN.", nameof(grossInByN));
        }

        if (string.IsNullOrWhiteSpace(policyVersion))
        {
            throw new ArgumentException("Версия применённой политики обязательна.", nameof(policyVersion));
        }

        Id = id;
        SettlementId = settlementId;
        GrossInOriginalCurrency = grossInOriginalCurrency;
        GrossInByN = grossInByN;
        CounterpartyKind = counterpartyKind;
        TaxableEventAtUtc = taxableEventAtUtc.ToUniversalTime();
        PolicyVersion = policyVersion.Trim();
    }

    /// <summary>Gets the candidate identifier.</summary>
    public Guid Id { get; }

    /// <summary>Gets the source settlement identifier.</summary>
    public Guid SettlementId { get; }

    /// <summary>Gets the gross payment amount before payment or bank fees.</summary>
    public Money GrossInOriginalCurrency { get; }

    /// <summary>Gets the auditable converted gross amount used by the policy.</summary>
    public Money GrossInByN { get; }

    /// <summary>Gets the payer classification received by the policy.</summary>
    public CounterpartyKind CounterpartyKind { get; }

    /// <summary>Gets the declared tax-event time, independent from payout timing.</summary>
    public DateTimeOffset TaxableEventAtUtc { get; }

    /// <summary>Gets the effective-dated policy version used to create this candidate.</summary>
    public string PolicyVersion { get; }
}

/// <summary>Summarises a policy evaluation for an annual taxable-income partition.</summary>
public sealed record AnnualTaxPosition
{
    /// <summary>Creates a policy-evaluation outcome with explicit currency and tax year.</summary>
    public AnnualTaxPosition(int taxYear, CounterpartyKind counterpartyKind, Money taxableIncomeByN, TaxThresholdStatus status, string policyVersion)
    {
        if (taxYear is < 2000 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(taxYear));
        }

        if (!StringComparer.Ordinal.Equals(taxableIncomeByN.Currency, "BYN"))
        {
            throw new ArgumentException("Годовой tax position должен быть выражен в BYN.", nameof(taxableIncomeByN));
        }

        if (string.IsNullOrWhiteSpace(policyVersion))
        {
            throw new ArgumentException("Версия политики обязательна.", nameof(policyVersion));
        }

        TaxYear = taxYear;
        CounterpartyKind = counterpartyKind;
        TaxableIncomeByN = taxableIncomeByN;
        Status = status;
        PolicyVersion = policyVersion.Trim();
    }

    /// <summary>Gets the calendar tax year evaluated by the policy.</summary>
    public int TaxYear { get; }

    /// <summary>Gets the counterparty category to which this position applies.</summary>
    public CounterpartyKind CounterpartyKind { get; }

    /// <summary>Gets policy-defined annual taxable income in BYN.</summary>
    public Money TaxableIncomeByN { get; }

    /// <summary>Gets the resulting visibility or review level.</summary>
    public TaxThresholdStatus Status { get; }

    /// <summary>Gets the effective policy version that produced the position.</summary>
    public string PolicyVersion { get; }
}
