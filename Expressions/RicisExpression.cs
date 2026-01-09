using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

public abstract class RicisExpression : Expression
{
    
    public abstract Expression Numerator { get; }
    // Универсальный структурный оператор
    public static bool operator ==(RicisExpression a, RicisExpression b)
        => a.AreEqual(b);

    public static bool operator !=(RicisExpression a, RicisExpression b)
        => !a.AreEqual(b);

    public override bool Equals(object obj)
        => obj is RicisExpression other && this.AreEqual(other);

    public override int GetHashCode()
        => ToString()?.GetHashCode() ?? 0;
}