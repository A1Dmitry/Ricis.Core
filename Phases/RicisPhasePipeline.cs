// RicisPhasePipeline.cs — strict RICIS v7.7 (no classical limits)

using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Simplifiers;

namespace Ricis.Core.Phases;

/// <summary>
/// Phase order (theory COMPUTATION_ALGORITHM + polar):
///   Priority contract: explicit RICIS rules and structural algebra always run
///   before a permitted classical bridge. Operations outside an explicit RICIS
///   rule preserve their classical expression semantics unchanged.
///   Phase 0.5 PolarTrigVisitor          — trig → polar sector → exact collapse
///   Phase 1   AlgebraicReductionVisitor — SP2 cancel identical factors
///   Phase 1.5 LimitBridgeVisitor        — O(1) bridges F·0→0_F, F/0→∞_F
///   Phase 2   RicisTransformVisitor     — A4/A1 0_F/0_G=F/G, F/0=∞_F
///   Phase 5   StandardOperationsVisitor — ∞ algebra (A5/A6/A7)
/// </summary>
public static class RicisPhasePipeline
{
    private static readonly List<IExpressionVisitor> _visitors =
    [
        new PolarTrigVisitor(),
        new AlgebraicReductionVisitor(),
        new LimitBridgeVisitor(),
        new RicisTransformVisitor(),
        new StandardOperationsVisitor(),
    ];

    public static Expression Simplify(Expression expr)
    {
        try
        {
            var result = expr;
            foreach (var visitor in _visitors)
            {
                try
                {
                    if (visitor is RicisTransformVisitor &&
                        result is LambdaExpression { Body: LazyInfinityExpression { CanReduce: true } })
                    {
                        continue;
                    }

                    result = visitor.Visit(result);
                }
                catch (Exception ve)
                {
                    Console.WriteLine(ve.Message);
                }
            }

            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
