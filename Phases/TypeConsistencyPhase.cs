using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Simplifiers;
using Ricis.Core.Resources;

namespace Ricis.Core.Phases;

/// <summary>
/// Phase 4: Type Consistency Protocol (SP3).
/// Validates indexed RICIS nodes without reducing or rewriting their payload.
/// </summary>
public static class TypeConsistencyPhase
{
    /// <summary>
    /// Validates type consistency for the supplied RICIS expression and returns
    /// the original expression unchanged.
    /// </summary>
    /// <param name="expr">The expression to validate.</param>
    /// <returns>The same expression instance after validation.</returns>
    /// <exception cref="ArgumentException">Thrown when an indexed node has an invalid payload or key.</exception>
    public static Expression Apply(Expression expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        return new TypeConsistencyVisitor().Visit(expr)
            ?? throw new InvalidOperationException("Type consistency visitor returned null.");
    }
}

/// <summary>
/// Non-reducing SP3 visitor for indexed RICIS nodes.
/// </summary>
public sealed class TypeConsistencyVisitor : ExpressionVisitor, IExpressionVisitor
{
    /// <inheritdoc />
    protected override Expression VisitExtension(Expression node)
    {
        if (node is not InfinityExpression singularity)
        {
            return node;
        }

        ArgumentNullException.ThrowIfNull(singularity.Numerator);
        if (singularity.Numerator.Type != singularity.Type)
        {
            throw new ArgumentException(
                RicisLegacyTextResources.Get("runtime.legacy.96e01bfaea4a"),
                nameof(node));
        }

        foreach (var (parameter, value) in singularity.Roots)
        {
            ArgumentNullException.ThrowIfNull(parameter);
            if (!double.IsFinite(value))
            {
                throw new ArgumentException(
                    RicisLegacyTextResources.Get("runtime.legacy.e84ad285c041"),
                    nameof(node));
            }
        }

        // Do not call base.VisitExtension: it may reduce CanReduce nodes and
        // destroy indexed payload. SP3 validates the node in place.
        return node;
    }
}
