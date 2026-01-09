using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Polynomial;
using System.Linq.Expressions;

namespace Ricis.Core.Simplifiers;

public class AlgebraicReductionVisitor : ExpressionVisitor, IExpressionVisitor
{
    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        if (node.NodeType != ExpressionType.Divide)
        {
            return left == node.Left && right == node.Right
                ? node
                : Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
        }

        // DRY FIX: Используем RicisType.InfinityOne
        if (left.AreEqual(right))
        {
            return RicisType.InfinityOne;
        }

        var parameter = FindSingleParameter(node);
        if (parameter == null)
        {
            return left == node.Left && right == node.Right
                ? node
                : Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
        }

        var cache = AnalyzeDenominator(right, parameter);

        // 1. 0/0 detection  
        var roots = cache.roots.Where(root => root.RationalValue.HasValue);
        foreach (var root in roots)
        {
            if (left.TryEvaluate(parameter.Name, (double)root.RationalValue.Value, out var res) && Math.Abs(res) < double.Epsilon)
            {
                // DRY FIX: Используем RicisType.InfinityZero вместо new Constant(0.0)
                return InfinityExpression.CreateLazy(
                    RicisType.InfinityZero,
                    new List<(ParameterExpression, double)> { (parameter, (double)root.RationalValue.Value) }
                );
            }
        }

        // 2. LongDivision
        if (!cache.isPolynomial)
        {
            return left == node.Left && right == node.Right
                ? node
                : Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
        }

        var divided = left.TryDivide(right, parameter);
        if (divided != null)
        {
            return Visit(divided);
        }

        return left == node.Left && right == node.Right ? node :
            Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
    }

    private static (List<Root> roots, bool isPolynomial) AnalyzeDenominator(Expression denominator, ParameterExpression param)
    {
        var collector = new PolynomialCoefficientCollector(param);
        collector.Visit(denominator);

        var roots = collector.IsPolynomial
            ? denominator.FindRoots(param)
            : denominator.FindNumericalRoots(param);

        return (roots, collector.IsPolynomial);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var obj = Visit(node.Object);
        var args = node.Arguments.Select(Visit);
        if (obj == node.Object && args.SequenceEqual(node.Arguments))
        {
            return node;
        }
        return Expression.Call(obj, node.Method, args);
    }

    protected override Expression VisitExtension(Expression node) => node;

    private static ParameterExpression FindSingleParameter(Expression expr)
    {
        var finder = new ParameterVisitor();
        finder.Visit(expr);
        return finder.FoundParameter;
    }
}