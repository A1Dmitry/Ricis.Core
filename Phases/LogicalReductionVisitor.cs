using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Simplifiers;

namespace Ricis.Core.Phases;

/// <summary>
/// Applies only side-effect-preserving reductions for built-in Boolean expression nodes.
/// It intentionally does not execute user expressions and does not collapse a short-circuit
/// branch when that could suppress evaluation of the other operand.
/// </summary>
internal sealed class LogicalReductionVisitor : ExpressionVisitor, IExpressionVisitor
{
    protected override Expression VisitExtension(Expression node) => node;

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        var visited = (LambdaExpression)base.VisitLambda(node);
        var minimizedBody = BooleanQuineMcCluskeyMinimizer.TryMinimize(visited.Body);
        if (minimizedBody.AreEqual(visited.Body))
        {
            return visited;
        }

        return Expression.Lambda(
            visited.Type,
            minimizedBody,
            visited.Name,
            visited.TailCall,
            visited.Parameters);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        if (node.NodeType != ExpressionType.Not ||
            node.Method is not null ||
            node.Type != typeof(bool) ||
            node.Operand.Type != typeof(bool))
        {
            return base.VisitUnary(node);
        }

        var operand = Visit(node.Operand);
        if (operand is ConstantExpression { Value: bool value })
        {
            return Expression.Constant(!value);
        }

        if (operand is UnaryExpression
            {
                NodeType: ExpressionType.Not,
                Method: null,
                Type: var innerType,
                Operand: var innerOperand
            } && innerType == typeof(bool) && innerOperand.Type == typeof(bool))
        {
            return innerOperand;
        }

        return ReferenceEquals(operand, node.Operand) ? node : Expression.Not(operand);
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType is not (ExpressionType.AndAlso or ExpressionType.OrElse) ||
            node.Method is not null ||
            node.Type != typeof(bool) ||
            node.Left.Type != typeof(bool) ||
            node.Right.Type != typeof(bool) ||
            node.IsLifted ||
            node.IsLiftedToNull)
        {
            return base.VisitBinary(node);
        }

        var left = Visit(node.Left);
        var right = Visit(node.Right);
        var result = TryReduceShortCircuit(node.NodeType, left, right);
        if (result is not null)
        {
            return result;
        }

        return ReferenceEquals(left, node.Left) && ReferenceEquals(right, node.Right)
            ? node
            : Expression.MakeBinary(node.NodeType, left, right);
    }

    protected override Expression VisitConditional(ConditionalExpression node)
    {
        var test = Visit(node.Test);
        var ifTrue = Visit(node.IfTrue);
        var ifFalse = Visit(node.IfFalse);

        if (ifTrue.AreEqual(ifFalse))
        {
            return ifTrue;
        }

        if (test is ConstantExpression { Value: bool condition })
        {
            return condition ? ifTrue : ifFalse;
        }

        return RebuildConditional(node, test, ifTrue, ifFalse, preserveBranches: true);
    }

    private static Expression TryReduceShortCircuit(ExpressionType nodeType, Expression left, Expression right)
    {
        if (nodeType == ExpressionType.AndAlso)
        {
            if (IsBooleanConstant(left, true)) return right;
            if (IsBooleanConstant(right, true)) return left;
            return null;
        }

        if (IsBooleanConstant(left, false)) return right;
        if (IsBooleanConstant(right, false)) return left;
        return null;
    }

    private static Expression RebuildConditional(
        ConditionalExpression node,
        Expression test,
        Expression ifTrue,
        Expression ifFalse,
        bool preserveBranches)
    {
        if (!preserveBranches && test is ConstantExpression { Value: bool condition })
        {
            return condition ? ifTrue : ifFalse;
        }

        return ReferenceEquals(test, node.Test) &&
               ReferenceEquals(ifTrue, node.IfTrue) &&
               ReferenceEquals(ifFalse, node.IfFalse)
            ? node
            : Expression.Condition(test, ifTrue, ifFalse);
    }

    private static bool IsBooleanConstant(Expression expression, bool expected) =>
        expression is ConstantExpression { Value: bool value } && value == expected;
}
