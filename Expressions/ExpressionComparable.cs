using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

/// <summary>
/// Универсальная дженерик-обёртка для Expression-потомков.
/// Даёт оператор ==, != и строгую структурную идентичность.
/// </summary>
public readonly struct ExpressionComparable<T>(T expr) : IEquatable<ExpressionComparable<T>>
    where T : Expression
{
    public T Expr { get; } = expr;

    public static implicit operator ExpressionComparable<T>(T expr)
        => new(expr);

    public static bool operator ==(ExpressionComparable<T> a, ExpressionComparable<T> b)
        => a.Expr.AreEqual(b.Expr);

    public static bool operator !=(ExpressionComparable<T> a, ExpressionComparable<T> b)
        => !a.Expr.AreEqual(b.Expr);

    public bool Equals(ExpressionComparable<T> other)
        => Expr.AreEqual(other.Expr);

    public override bool Equals(object obj)
        => obj is ExpressionComparable<T> other && Equals(other);

    public override int GetHashCode()
        => Expr?.ToString().GetHashCode() ?? 0;
}