using Ricis.Core.Phases;
using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

/// <summary>
/// Represents the RICIS public type <c>RicisExpression</c>.
/// </summary>
public abstract class RicisExpression : Expression
{
    
    /// <summary>
    /// Gets the <c>Numerator</c> value of <c>RicisExpression</c>.
    /// </summary>
    public abstract Expression Numerator { get; }
    // Универсальный структурный оператор
    /// <summary>
    /// Performs the <c>operator ==</c> operation.
    /// </summary>
    public static bool operator ==(RicisExpression a, RicisExpression b)
        => a.AreEqual(b);

    /// <summary>
    /// Performs the <c>operator !=</c> operation.
    /// </summary>
    public static bool operator !=(RicisExpression a, RicisExpression b)
        => !a.AreEqual(b);

    /// <inheritdoc />
    public override bool Equals(object obj)
        => obj is RicisExpression other && this.AreEqual(other);

    /// <inheritdoc />
    public override int GetHashCode()
        => ToString()?.GetHashCode() ?? 0;
    
   
}