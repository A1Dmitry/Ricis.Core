using System.Linq.Expressions;

namespace Ricis.Core;

/// <summary>
/// Represents the RICIS public type <c>RicisType</c>.
/// </summary>
public class RicisType(string signature, bool isComposite = false) : IEquatable<RicisType>
{
    /// <summary>
    /// Gets the <c>Scalar</c> value of <c>RicisType</c>.
    /// </summary>
    public static readonly RicisType Scalar = new("Scalar");
    /// <summary>
    /// Gets the <c>InfinityZero</c> value of <c>RicisType</c>.
    /// </summary>
    public static readonly ConstantExpression InfinityZero = Expression.Constant(0.0);
    /// <summary>
    /// Gets the <c>InfinityOne</c> value of <c>RicisType</c>.
    /// </summary>
    public static readonly ConstantExpression InfinityOne = Expression.Constant(1.0);

    /// <summary>
    /// Gets the <c>Signature</c> value of <c>RicisType</c>.
    /// </summary>
    public string Signature { get; } = signature;
    /// <summary>
    /// Gets the <c>IsComposite</c> value of <c>RicisType</c>.
    /// </summary>
    public bool IsComposite { get; } = isComposite;

    /// <summary>
    /// Executes <c>Equals</c> for the RICIS expression model.
    /// </summary>
    public bool Equals(RicisType other)
    {
        return other != null && Signature == other.Signature;
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return obj is RicisType rt && Signature.Equals(rt.Signature);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Signature, IsComposite);
    }

    // Логика совместимости (L1 Identity)
    /// <summary>
    /// Determines whether <c>IsCompatibleWith</c> holds for the supplied RICIS expression.
    /// </summary>
    public bool IsCompatibleWith(RicisType other)
    {
        if (Signature == "Scalar" || other.Signature == "Scalar")
        {
            return true; // Скаляры универсальны
        }

        return Signature == other.Signature;
    }

    // Алгебра типов: Умножение/Деление
    /// <summary>
    /// Executes <c>Operate</c> for the RICIS expression model.
    /// </summary>
    public static RicisType Operate(RicisType a, RicisType b, string op)
    {
        if (a.Signature == "Scalar")
        {
            return b;
        }

        if (b.Signature == "Scalar")
        {
            return a;
        }

        // Упрощение: x/x = Scalar
        if (op == "/" && a.Signature == b.Signature)
        {
            return Scalar;
        }

        return new RicisType($"({a.Signature}{op}{b.Signature})", true);
    }

    // Алгебра типов: Сложение (Создание Монолита)
    /// <summary>
    /// Executes <c>CreateTuple</c> for the RICIS expression model.
    /// </summary>
    public static RicisType CreateTuple(RicisType a, RicisType b)
    {
        // Сортировка для канонической формы (Space, Time) == (Time, Space)
        var parts = new[] { a.Signature, b.Signature }.OrderBy(x => x);
        return new RicisType($"Tuple<{string.Join(",", parts)}>", true);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Signature;
    }
}