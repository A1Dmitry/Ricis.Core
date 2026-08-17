using System.Linq.Expressions;

namespace Ricis.Core.Execution;

/// <summary>
/// Classifies expression trees that may be evaluated by the double-domain
/// numerical root facilities without executing caller-provided code.
/// </summary>
internal static class NumericalEvaluationSafety
{
    /// <summary>
    /// Returns whether an expression contains only finite built-in operators,
    /// constants, parameters, conditionals, and static <see cref="Math"/> calls.
    /// </summary>
    internal static bool IsSafeDoubleExpression(Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var visitor = new SafeDoubleExpressionVisitor();
        visitor.Visit(expression);
        return visitor.IsSafe;
    }

    private sealed class SafeDoubleExpressionVisitor : ExpressionVisitor
    {
        public bool IsSafe { get; private set; } = true;

        public override Expression Visit(Expression node)
        {
            if (!IsSafe || node is null)
            {
                return node;
            }

            return base.Visit(node);
        }

        protected override Expression VisitConstant(ConstantExpression node) => node;

        protected override Expression VisitParameter(ParameterExpression node) => node;

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.Method is not null || node.NodeType is not (
                ExpressionType.Negate or
                ExpressionType.NegateChecked or
                ExpressionType.UnaryPlus or
                ExpressionType.Convert or
                ExpressionType.ConvertChecked or
                ExpressionType.Not))
            {
                IsSafe = false;
                return node;
            }

            return base.VisitUnary(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if ((node.Method is not null && !NumericConstants.IsIntrinsicNumeric(node.Type)) ||
                node.NodeType is not (
                ExpressionType.Add or
                ExpressionType.AddChecked or
                ExpressionType.Subtract or
                ExpressionType.SubtractChecked or
                ExpressionType.Multiply or
                ExpressionType.MultiplyChecked or
                ExpressionType.Divide or
                ExpressionType.Modulo or
                ExpressionType.Power or
                ExpressionType.Equal or
                ExpressionType.NotEqual or
                ExpressionType.GreaterThan or
                ExpressionType.GreaterThanOrEqual or
                ExpressionType.LessThan or
                ExpressionType.LessThanOrEqual or
                ExpressionType.AndAlso or
                ExpressionType.OrElse))
            {
                IsSafe = false;
                return node;
            }

            return base.VisitBinary(node);
        }

        protected override Expression VisitConditional(ConditionalExpression node) => base.VisitConditional(node);

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Object is not null || node.Method.DeclaringType != typeof(Math))
            {
                IsSafe = false;
                return node;
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitExtension(Expression node)
        {
            IsSafe = false;
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            IsSafe = false;
            return node;
        }

        protected override Expression VisitInvocation(InvocationExpression node)
        {
            IsSafe = false;
            return node;
        }

        protected override Expression VisitNew(NewExpression node)
        {
            IsSafe = false;
            return node;
        }
    }
}
