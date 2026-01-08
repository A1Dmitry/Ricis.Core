// RicisPhasePipeline.cs

using System.Linq.Expressions;
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
        return _visitors.Aggregate(expr, (current, visitor) => visitor.Visit(current));
    }
}