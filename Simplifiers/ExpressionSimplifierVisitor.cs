using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;

namespace Ricis.Core.Simplifiers;

/// <summary>
/// Represents the RICIS public type <c>ExpressionSimplifierVisitor</c>.
/// </summary>
public sealed class ExpressionSimplifierVisitor : ExpressionVisitor, IExpressionVisitor
{
    private readonly IRicisScalarPolicy scalarPolicy;

    /// <summary>Initializes the legacy built-in scalar route.</summary>
    public ExpressionSimplifierVisitor()
        : this(RicisScalarPolicies.Legacy)
    {
    }

    internal ExpressionSimplifierVisitor(IRicisScalarPolicy scalarPolicy)
    {
        this.scalarPolicy = scalarPolicy ?? throw new ArgumentNullException(nameof(scalarPolicy));
    }

    /// <inheritdoc />
    protected override Expression VisitExtension(Expression node) =>
        RicisSpecialExpressionRebinder.Rebind(node, Visit);

    /// <inheritdoc />
    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        // RICIS extensions have already received their normative A/O(1)
        // semantics in earlier phases. Do not apply ordinary zero, power,
        // distribution, or commutative rewrites across indexed payload nodes.
        if (left is RicisExpression || right is RicisExpression)
        {
            return node.Update(left, node.Conversion, right);
        }

        // Базовые алгебраические тождества
        switch (node.NodeType)
        {
            case ExpressionType.Add when IsZero(left): return right;
            case ExpressionType.Add when IsZero(right): return left;
            case ExpressionType.Subtract when IsZero(right): return left;
            case ExpressionType.Multiply when IsZero(left) || IsZero(right): return scalarPolicy.ZeroOf(node.Type);
            case ExpressionType.Multiply when IsOne(left): return right;
            case ExpressionType.Multiply when IsOne(right): return left;
            case ExpressionType.Divide when IsZero(left): return left;
            case ExpressionType.Divide when IsOne(right): return left;
        }

        // Сокращение обратных операций: (A + B) - B → A, (A - B) + B → A
        if (node.NodeType == ExpressionType.Subtract && left is BinaryExpression { NodeType: ExpressionType.Add } sumLeft)
        {
            if (AreIdentical(sumLeft.Right, right)) return sumLeft.Left;
            if (AreIdentical(sumLeft.Left, right)) return sumLeft.Right;
        }
        if (node.NodeType == ExpressionType.Add && left is BinaryExpression { NodeType: ExpressionType.Subtract } subLeft)
        {
            if (AreIdentical(subLeft.Right, right)) return subLeft.Left;
        }

        // Нормализация: x+x → 2*x, x*x → Pow(x,2)
        if (AreIdentical(left, right))
        {
            return node.NodeType switch
            {
                ExpressionType.Add => Expression.Multiply(CreateNumericConstant(2, node.Type), left),
                ExpressionType.Multiply when node.Method is null => CreatePowerOrProduct(left),
                _ => node.Update(left, node.Conversion, right)
            };
        }

        // Коммутивность (нормализация порядка)
        if (node.IsCommutative() && ShouldCommute(left, right))
        {
            return node.Update(right, node.Conversion, left);
        }

        // Константы
        if (left is ConstantExpression lc && right is ConstantExpression rc)
        {
            return SimplifyConstants(node, lc, rc);
        }

        // Сложение/умножение дробей
        if (IsFraction(left) && IsFraction(right))
        {
            return node.NodeType switch
            {
                ExpressionType.Add => SimplifyFractionSum(GetFraction(left), GetFraction(right)),
                ExpressionType.Multiply => SimplifyFractionProduct(GetFraction(left), GetFraction(right)),
                _ => node.Update(left, node.Conversion, right)
            };
        }

        // Распределительный закон: (a+b)*c → a*c + b*c
        if (node.NodeType == ExpressionType.Multiply && IsSum(left))
        {
            return DistributeMultiplySum(node);
        }

        return node.Update(left, node.Conversion, right);
    }

    /// <inheritdoc />
    protected override Expression VisitUnary(UnaryExpression node)
    {
        var operand = Visit(node.Operand);

        if (operand is RicisExpression)
        {
            return node.Update(operand);
        }

        // Двойное отрицание
        if (node.NodeType == ExpressionType.Negate && operand is UnaryExpression innerNegate &&
            innerNegate.NodeType == ExpressionType.Negate)
        {
            return innerNegate.Operand;
        }

        // Keep unary constants in their original scalar type. Converting every
        // value through BigInteger truncates fractional values and can make a
        // later indexed payload type-inconsistent.
        return node.Update(operand);
    }


    /// <inheritdoc />
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var visited = base.VisitMethodCall(node);
        if (visited is not MethodCallExpression call)
        {
            return visited;
        }

        if (call.Method.DeclaringType == typeof(Math) && call.Arguments.Count == 1)
        {
            var arg = call.Arguments[0];

            if (call.Method.Name == "Log")
            {
                // ln(1) => 0
                if (IsOne(arg))
                {
                    return scalarPolicy.ZeroOf(call.Type);
                }

                // ln(exp(x)) => x
                if (arg is MethodCallExpression innerCall &&
                    innerCall.Method.DeclaringType == typeof(Math) &&
                    innerCall.Method.Name == "Exp" &&
                    innerCall.Arguments.Count == 1)
                {
                    return innerCall.Arguments[0];
                }
            }
            else if (call.Method.Name == "Exp")
            {
                // exp(0) => 1
                if (IsZero(arg))
                {
                    return CreateNumericConstant(1, call.Type);
                }

                // exp(ln(x)) => x
                if (arg is MethodCallExpression innerCall &&
                    innerCall.Method.DeclaringType == typeof(Math) &&
                    innerCall.Method.Name == "Log" &&
                    innerCall.Arguments.Count == 1)
                {
                    return innerCall.Arguments[0];
                }
            }
        }

        return call;
    }

    /// <inheritdoc />
    protected override Expression VisitConditional(ConditionalExpression node)
    {
        var test = Visit(node.Test);
        var ifTrue = Visit(node.IfTrue);
        var ifFalse = Visit(node.IfFalse);

        if (test is ConstantExpression { Value: true })
        {
            return ifTrue;
        }

        if (test is ConstantExpression { Value: false })
        {
            return ifFalse;
        }

        return node.Update(test, ifTrue, ifFalse);
    }

    // Распределение: (a+b)*c → a*c + b*c
    private Expression DistributeMultiplySum(BinaryExpression node)
    {
        var sum = (BinaryExpression)node.Left;
        var factor = node.Right;
        var term1 = Expression.Multiply(sum.Left, factor);
        var term2 = Expression.Multiply(sum.Right, factor);
        return Visit(Expression.Add(term1, term2));
    }

    private static bool AreIdentical(Expression a, Expression b)
    {
        return ReferenceEquals(a, b) || a.AreEqual(b);
    }

    private bool ShouldCommute(Expression left, Expression right)
    {
        // Лексикографическая нормализация для консистентности
        return GetComplexityScore(left) > GetComplexityScore(right);
    }

    private int GetComplexityScore(Expression node)
    {
        return node switch
        {
            ParameterExpression => 1,
            ConstantExpression => 2,
            BinaryExpression b => 3 + GetComplexityScore(b.Left) + GetComplexityScore(b.Right),
            _ => 10
        };
    }

    // Фракционные операции
    private (Expression num, Expression den) GetFraction(Expression expr)
    {
        return expr is BinaryExpression div ? (div.Left, div.Right) : (expr, Expression.Constant(1));
    }

    private bool IsFraction(Expression expr)
    {
        return expr is BinaryExpression b && b.NodeType == ExpressionType.Divide;
    }

    private bool IsSum(Expression expr)
    {
        return expr is BinaryExpression b && b.NodeType == ExpressionType.Add;
    }

    private static Expression SimplifyConstants(BinaryExpression node, ConstantExpression left, ConstantExpression right)
    {
        try
        {
            // Fold using the original expression-tree operator, then retain its
            // declared type. This avoids truncating doubles/decimals to BigInteger.
            var folded = Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
            var boxed = Expression.Convert(folded, typeof(object));
            var value = Expression.Lambda<Func<object>>(boxed).Compile()();
            return Expression.Constant(value, node.Type);
        }
        catch
        {
            // Preserve the valid tree when an operator cannot be folded safely.
            return node.Update(left, node.Conversion, right);
        }
    }

    private static Expression SimplifyFractionSum((Expression, Expression) f1, (Expression, Expression) f2)
    {
        var (a, b) = f1;
        var (c, d) = f2;
        var num = Expression.Add(Expression.Multiply(a, d), Expression.Multiply(c, b));
        var den = Expression.Multiply(b, d);
        return Expression.Divide(num, den);
    }

    private static Expression SimplifyFractionProduct((Expression, Expression) f1, (Expression, Expression) f2)
    {
        var (a, b) = f1;
        var (c, d) = f2;
        return Expression.Divide(
            Expression.Multiply(a, c),
            Expression.Multiply(b, d));
    }

    private Expression CreatePowerOrProduct(Expression @base)
    {
        try
        {
            return Expression.Power(@base, CreateNumericConstant(2, @base.Type));
        }
        catch (ArgumentException)
        {
            // Expression.Power is not defined for every INumber type (notably
            // BigInteger). Preserve the exact ordinary product instead of
            // changing the scalar domain or throwing from a simplifier.
            return Expression.Multiply(@base, @base);
        }
    }

    private bool IsZero(Expression expression) =>
        expression is ConstantExpression constant && scalarPolicy.IsZeroValue(constant.Value);

    private bool IsOne(Expression expression) =>
        expression is ConstantExpression constant && scalarPolicy.IsOneValue(constant.Value);

    private static bool IsNumericValue(object value, int expected) => value switch
    {
        byte v => v == expected,
        sbyte v => v == expected,
        short v => v == expected,
        ushort v => v == expected,
        int v => v == expected,
        uint v => v == expected,
        long v => v == expected,
        ulong v => expected >= 0 && v == (ulong)expected,
        float v => v == expected,
        double v => v == expected,
        decimal v => v == expected,
        BigInteger v => v == expected,
        _ => false,
    };

    private Expression CreateNumericConstant(int value, Type type) => scalarPolicy.FromInt32(value, type);

}