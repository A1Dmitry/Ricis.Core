using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

/// <summary>
/// Rebinds parameter references inside RICIS extension nodes without invoking
/// <see cref="Expression.Reduce"/>. This is an implementation utility only:
/// it does not introduce or alter any RICIS axiom.
/// </summary>
internal static class RicisSpecialExpressionRebinder
{
    /// <summary>
    /// Rebinds a special expression node using the supplied ordinary expression
    /// visitor for its deferred operands and certified root parameters.
    /// </summary>
    public static Expression Rebind(Expression node, Func<Expression, Expression> visit)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(visit);

        return node switch
        {
            KeyedInfinityExpression keyed => new KeyedInfinityExpression(
                keyed.Branches
                    .Select(branch => (PoleInfinityExpression)RebindPole(branch, visit))
                    .ToArray()),
            PoleInfinityExpression pole => RebindPole(pole, visit),
            ZeroInfinityExpression zero => new ZeroInfinityExpression(
                visit(zero.Numerator),
                RebindRoots(zero.Roots, visit)),
            LazyInfinityExpression lazy => InfinityExpression.CreateLazy(
                visit(lazy.Numerator),
                RebindRoots(lazy.Roots, visit)),
            DeferredDerivativeExpression derivative => new DeferredDerivativeExpression(
                visit(derivative.Operand),
                (ParameterExpression)visit(derivative.DifferentiationVariable)),
            _ => node,
        };
    }

    private static PoleInfinityExpression RebindPole(
        PoleInfinityExpression pole,
        Func<Expression, Expression> visit) =>
        new(
            visit(pole.Numerator),
            RebindRoots(pole.Roots, visit),
            RebindRoots(pole.NumeratorRoots, visit));

    private static List<(ParameterExpression Param, double Value)> RebindRoots(
        IEnumerable<(ParameterExpression Param, double Value)> roots,
        Func<Expression, Expression> visit) =>
        roots
            .Select(root => ((ParameterExpression)visit(root.Param), root.Value))
            .ToList();
}
