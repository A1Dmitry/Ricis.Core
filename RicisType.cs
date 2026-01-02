using System.Linq.Expressions;

namespace Ricis.Core;

public class RicisType(string signature, bool isComposite = false) : IEquatable<RicisType>
{
    public static readonly RicisType Scalar = new("Scalar");
    public static readonly ConstantExpression InfinityZero = Expression.Constant(0.0);
    public static readonly ConstantExpression InfinityOne = Expression.Constant(1.0);

    public string Signature { get; } = signature;
    public bool IsComposite { get; } = isComposite;

    public bool Equals(RicisType other)
    {
        return other != null && Signature == other.Signature;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        return obj is RicisType rt && Signature.Equals(rt.Signature);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Signature, IsComposite);
    }

    // Логика совместимости (L1 Identity)
    public bool IsCompatibleWith(RicisType other)
    {
        if (Signature == "Scalar" || other.Signature == "Scalar") return true; // Скаляры универсальны

        return Signature == other.Signature;
    }

    // Алгебра типов: Умножение/Деление
    public static RicisType Operate(RicisType a, RicisType b, string op)
    {
        if (a.Signature == "Scalar") return b;

        if (b.Signature == "Scalar") return a;

        // Упрощение: x/x = Scalar
        if (op == "/" && a.Signature == b.Signature) return Scalar;

        return new RicisType($"({a.Signature}{op}{b.Signature})", true);
    }

    // Алгебра типов: Сложение (Создание Монолита)
    public static RicisType CreateTuple(RicisType a, RicisType b)
    {
        // Сортировка для канонической формы (Space, Time) == (Time, Space)
        var parts = new[] { a.Signature, b.Signature }.OrderBy(x => x);
        return new RicisType($"Tuple<{string.Join(",", parts)}>", true);
    }

    public override string ToString()
    {
        return Signature;
    }
}