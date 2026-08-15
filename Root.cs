using Ricis.Core.Rationals;
using System.Linq.Expressions;

namespace Ricis.Core;

/// <summary>
///     Представление одного точного корня
/// </summary>
public readonly struct Root : IEquatable<Root>
{
    /// <summary>
    /// Gets the <c>Parameter</c> value of <c>Root</c>.
    /// </summary>
    public ParameterExpression Parameter { get; }
    /// <summary>
    /// Gets the <c>RationalValue</c> value of <c>Root</c>.
    /// </summary>
    public Rational? RationalValue { get; } // если корень рациональный
    /// <summary>
    /// Gets the <c>DoubleValue</c> value of <c>Root</c>.
    /// </summary>
    public double DoubleValue { get; } // всегда есть (для подстановки)

    /// <summary>
    /// Initializes a new instance of <c>Root</c>.
    /// </summary>
    public Root(ParameterExpression param, Rational value)
    {
        Parameter = param;
        RationalValue = value;
        DoubleValue = value.ToDouble();
    }

    /// <summary>
    /// Initializes a new instance of <c>Root</c>.
    /// </summary>
    public Root(ParameterExpression param, double value)
    {
        Parameter = param;
        RationalValue = null;
        DoubleValue = value;
    }

    /// <summary>
    /// Executes <c>Equals</c> for the RICIS expression model.
    /// </summary>
    public bool Equals(Root other)
    {
        return Parameter == other.Parameter &&
               Math.Abs(DoubleValue - other.DoubleValue) == 0;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Parameter, DoubleValue);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return RationalValue.HasValue
            ? $"{Parameter.Name} = {RationalValue.Value}"
            : $"{Parameter.Name} ≈ {DoubleValue:R}";
    }
}