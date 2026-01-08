using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Polynomial;

namespace Ricis.Core.Simplifiers;

public class AlgebraicReductionVisitor : ExpressionVisitor, IExpressionVisitor
{

    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        if (node.NodeType == ExpressionType.Divide)
        {
            if (left.AreEqual(right)) return RicisType.InfinityOne;

            var parameter = FindSingleParameter(node);
            if (parameter != null)
            {
                // ✅ КЭШ: один вызов FindRoots на denominator
                var cache = AnalyzeDenominator(right, parameter);

                // 1. 0/0 detection  
                var roots = cache.roots.Where(root => root.RationalValue.HasValue);
                foreach (var root in roots)
                {
                    if (left.TryEvaluate(parameter.Name, root.RationalValue.Value, out var res) && res.IsZero)
                    {
                        return RicisType.InfinityZero;
                    }
                }

                // 2. LongDivision (только если полином)
                if (cache.isPolynomial)
                {
                    var divided = left.TryDivide(right, parameter);
                    if (divided != null) return Visit(divided);
                }
            }
        }

        return left == node.Left && right == node.Right ? node :
            Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
    }

    //ЛОКАЛЬНАЯ функция кэша(внутри класса)
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