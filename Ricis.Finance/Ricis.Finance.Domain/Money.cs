using Ricis.Core.Resources;
using System.Globalization;

namespace Ricis.Finance.Domain;

/// <summary>
/// Immutable non-negative monetary amount with an explicit ISO 4217 currency.
/// Domain operations reject currency mixing instead of silently converting it.
/// </summary>
public readonly record struct Money
{
    /// <summary>Creates a non-negative monetary value in the supplied ISO currency.</summary>
    public Money(decimal amount, string currency)
    {
        if (amount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, RicisLegacyTextResources.Get("runtime.legacy.caebefb3ad5d"));
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("runtime.legacy.72b1658e50f4"), nameof(currency));
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.ToEven);
        Currency = currency.Trim().ToUpperInvariant();
    }

    /// <summary>Gets the decimal amount rounded to currency precision used by this library.</summary>
    public decimal Amount { get; }

    /// <summary>Gets the canonical uppercase ISO 4217 currency code.</summary>
    public string Currency { get; }

    /// <summary>Creates an additive total after enforcing equal currency.</summary>
    public Money Add(Money other) => new(Amount + RequireSameCurrency(other).Amount, Currency);

    /// <summary>Creates a subtraction result and rejects negative monetary outcomes.</summary>
    public Money Subtract(Money other)
    {
        var sameCurrency = RequireSameCurrency(other);
        if (sameCurrency.Amount > Amount)
        {
            throw new InvalidOperationException(RicisLegacyTextResources.Get("runtime.legacy.24d3cdd7ff21"));
        }

        return new Money(Amount - sameCurrency.Amount, Currency);
    }

    /// <summary>Creates a zero value in the supplied currency.</summary>
    public static Money Zero(string currency) => new(0m, currency);

    /// <inheritdoc />
    public override string ToString() => string.Create(
        CultureInfo.InvariantCulture,
        $"{Amount:0.00} {Currency}");

    private Money RequireSameCurrency(Money other)
    {
        if (!StringComparer.Ordinal.Equals(Currency, other.Currency))
        {
            throw new InvalidOperationException(RicisLegacyTextResources.Format("runtime.legacy.917a580ee2e5", ("Currency", Currency), ("other.Currency", other.Currency)));
        }

        return other;
    }
}
