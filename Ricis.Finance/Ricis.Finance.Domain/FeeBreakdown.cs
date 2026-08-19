namespace Ricis.Finance.Domain;

/// <summary>
/// Separates provider and recipient-bank fees from an auditable gross settlement amount.
/// </summary>
public sealed record FeeBreakdown
{
    /// <summary>Creates a fee breakdown and proves the net amount is non-negative.</summary>
    public FeeBreakdown(Money gross, Money providerFee, Money bankFee)
    {
        RequireCurrency(gross, providerFee, nameof(providerFee));
        RequireCurrency(gross, bankFee, nameof(bankFee));

        Gross = gross;
        ProviderFee = providerFee;
        BankFee = bankFee;
        Net = gross.Subtract(providerFee).Subtract(bankFee);
    }

    /// <summary>Gets the contractual or reported incoming amount before fees.</summary>
    public Money Gross { get; }

    /// <summary>Gets the payment-provider fee, if known for this settlement.</summary>
    public Money ProviderFee { get; }

    /// <summary>Gets the recipient-bank fee, if known for this payout route.</summary>
    public Money BankFee { get; }

    /// <summary>Gets gross less all recorded fees.</summary>
    public Money Net { get; }

    private static void RequireCurrency(Money gross, Money fee, string parameterName)
    {
        if (!StringComparer.Ordinal.Equals(gross.Currency, fee.Currency))
        {
            throw new ArgumentException("Комиссия должна быть в валюте gross-суммы до отдельной FX-конверсии.", parameterName);
        }
    }
}
