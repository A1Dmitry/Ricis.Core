using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

/// <summary>
/// Универсальная дженерик-обёртка для Expression-потомков.
/// Даёт оператор ==, != и строгую структурную идентичность.
/// </summary>
public readonly struct ExpressionComparable<T>(T expr) : IEquatable<ExpressionComparable<T>>
    where T : Expression
{
    /// <summary>
    /// Gets the <c>Expr</c> value of <c>ExpressionComparable&lt;T&gt;</c>.
    /// </summary>
    public T Expr { get; } = expr;

    /// <summary>
    /// Converts a value through <c>implicit operator ExpressionComparable&lt;T&gt;</c>.
    /// </summary>
    public static implicit operator ExpressionComparable<T>(T expr)
        => new(expr);

    /// <summary>
    /// Performs the <c>operator ==</c> operation.
    /// </summary>
    public static bool operator ==(ExpressionComparable<T> a, ExpressionComparable<T> b)
        => a.Expr.AreEqual(b.Expr);

    /// <summary>
    /// Performs the <c>operator !=</c> operation.
    /// </summary>
    public static bool operator !=(ExpressionComparable<T> a, ExpressionComparable<T> b)
        => !a.Expr.AreEqual(b.Expr);

    /// <summary>
    /// Executes <c>Equals</c> for the RICIS expression model.
    /// </summary>
    public bool Equals(ExpressionComparable<T> other)
        => Expr.AreEqual(other.Expr);

    /// <inheritdoc />
    public override bool Equals(object obj)
        => obj is ExpressionComparable<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
        => Expr?.ToString().GetHashCode() ?? 0;
}