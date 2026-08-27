using System.Linq.Expressions;
using Ricis.Core.Phases;

namespace Ricis.Core.Simplifiers;

/// <summary>
/// Provides a public, focused entry point for safe Boolean expression reduction.
/// </summary>
/// <remarks>
/// This façade applies the normative <see cref="LogicalReductionVisitor"/> rules without
/// running the complete arithmetic RICIS phase pipeline. It preserves short-circuit and
/// side-effect boundaries: only safe built-in Boolean identities are reduced, while impure,
/// lifted and user-defined Boolean operations retain their original evaluation semantics.
/// </remarks>
public static class LogicalSimplifier
{
    /// <summary>
    /// Reduces a Boolean expression tree through the normative safe logical reducer.
    /// </summary>
    /// <param name="expression">The Boolean expression or lambda expression to reduce.</param>
    /// <returns>The reduced expression, preserving the original node where no safe rule applies.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="expression"/> is null.</exception>
    public static Expression Apply(Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return new LogicalReductionVisitor().Visit(expression);
    }
}
