// RicisPhasePipeline.cs

using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Simplifiers;

namespace Ricis.Core.Phases;

/// <summary>
/// Оркестратор фаз упрощения по RICIS 7.3_safety_patched
/// Строго следует порядку фаз от -1 до 6
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
                    if (visitor is RicisTransformVisitor && result is LambdaExpression { Body: LazyInfinityExpression { CanReduce: true } })
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