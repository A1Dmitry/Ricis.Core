using System.Linq.Expressions;
using Ricis.Core.Expressions;

namespace Ricis.Core.Simplifiers;

/// <summary>
/// RICIS polar phase: trig → rational sector of the circle → exact collapse.
/// Runs before algebraic SP2 so half of trig singularities become
/// constants / 0_F / ∞_F without limits or series.
/// </summary>
public class PolarTrigVisitor : ExpressionVisitor, IExpressionVisitor
{
    /// <inheritdoc />
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var obj = Visit(node.Object);
        var args = node.Arguments.Select(Visit).ToList();

        // rebuild if children changed
        MethodCallExpression call = node;
        if (obj != node.Object || !args.SequenceEqual(node.Arguments))
        {
            call = Expression.Call(obj, node.Method, args);
        }

        var collapsed = PolarConverter.CollapseConstantTrig(call);
        return collapsed;
    }

    /// <inheritdoc />
    protected override Expression VisitExtension(Expression node) => node;
}
