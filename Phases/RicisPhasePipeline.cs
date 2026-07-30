// RicisPhasePipeline.cs — strict RICIS v7.7 (no classical limits)

using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Simplifiers;

namespace Ricis.Core.Phases;

/// <summary>
/// Phase order (theory COMPUTATION_ALGORITHM):
///   Phase 1  SP2  AlgebraicReductionVisitor  — cancel identical factors first
///   Phase 2  A4/A1 RicisTransformVisitor     — 0_F/0_G=F/G, F/0=∞_F (no L'Hôpital)
///   Phase 5  StandardOperationsVisitor      — ∞ algebra (A5/A6/A7)
/// </summary>
public static class RicisPhasePipeline
{
    private static readonly List<IExpressionVisitor> _visitors =
    [
        new AlgebraicReductionVisitor(),
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
