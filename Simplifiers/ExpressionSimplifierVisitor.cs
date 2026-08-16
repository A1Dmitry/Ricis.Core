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
    private readonly Dictionary<string, ParameterExpression> _parameters = new();


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
            case ExpressionType.Multiply when IsZero(left) || IsZero(right): return NumericConstants.ZeroOf(node.Type);
            case ExpressionType.Multiply when IsOne(left): return right;
            case ExpressionType.Multiply when IsOne(right): return left;
            case ExpressionType.Divide when IsZero(left): return left;
            case ExpressionType.Divide when IsOne(right): return left;
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

        if (operand is ConstantExpression c)
        {
            return SimplifyConstantsUnary(node.NodeType, c.Value);
        }

        return node.Update(operand);
    }

    private Expression SimplifyConstantsUnary(ExpressionType nodeType, object value)
    {
        try
        {
            var num = value.ToBigInteger();

            return nodeType switch
            {
                ExpressionType.Negate => Expression.Constant(-num, typeof(BigInteger)),
                ExpressionType.UnaryPlus => Expression.Constant(num, typeof(BigInteger)),
                ExpressionType.Not when value is bool b => Expression.Constant(!b, typeof(bool)),
                _ => throw new ArgumentException($"Unsupported unary operation: {nodeType}")
            };
        }
        catch
        {
            // Fallback для неподдерживаемых типов
            return Expression.MakeUnary(nodeType, Expression.Constant(value), value?.GetType() ?? typeof(object));
        }
    }


    /// <inheritdoc />
    protected override Expression VisitConditional(ConditionalExpression node)
    {
        var test = Visit(node.Test);
        var ifTrue = Visit(node.IfTrue);
        var ifFalse = Visit(node.IfFalse);

        if (test is ConstantExpression tc && (bool)tc.Value)
        {
            return ifTrue;
        }

        if (test is ConstantExpression tf && !(bool)tf.Value)
        {
            return ifFalse;
        }

        return node.Update(test, ifTrue, ifFalse);
    }

    internal Expression VisitLogical(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        // Идемпотентность: x && x → x, x || x → x
        if (AreIdentical(left, right))
        {
            return node.NodeType == ExpressionType.AndAlso ? left : right;
        }

        // x && true → x, x || false → x
        if (node.NodeType == ExpressionType.AndAlso)
        {
            if (IsTrue(right))
            {
                return left;
            }

            if (IsFalse(right))
            {
                return Expression.Constant(false, node.Type);
            }
        }
        else
        {
            if (IsTrue(right))
            {
                return right;
            }

            if (IsFalse(right))
            {
                return left;
            }
        }

        return node.Update(left, node.Conversion, right);
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

    private static Expression SimplifyFraction(BigInteger num, BigInteger den)
    {
        if (den == 0)
        {
            throw new DivideByZeroException();
        }

        if (num == 0)
        {
            return Expression.Constant(BigInteger.Zero);
        }

        var gcd = BigInteger.GreatestCommonDivisor(num < 0 ? -num : num, den);
        return Expression.Divide(
            Expression.Constant(num / gcd, typeof(BigInteger)),
            Expression.Constant(den / gcd, typeof(BigInteger)));
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

    private static Expression CreatePowerOrProduct(Expression @base)
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

    private static bool IsZero(Expression e)
    {
        return e is ConstantExpression c && IsNumericValue(c.Value, 0);
    }

    private static bool IsOne(Expression e)
    {
        return e is ConstantExpression c && IsNumericValue(c.Value, 1);
    }

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

    private static Expression CreateNumericConstant(int value, Type type)
    {
        if (type == typeof(double)) return Expression.Constant((double)value);
        if (type == typeof(float)) return Expression.Constant((float)value);
        if (type == typeof(decimal)) return Expression.Constant((decimal)value);
        if (type == typeof(long)) return Expression.Constant((long)value);
        if (type == typeof(BigInteger)) return Expression.Constant(new BigInteger(value));
        return Expression.Constant(value, type);
    }

    private static bool IsTrue(Expression e)
    {
        return e is ConstantExpression c && (bool)c.Value;
    }

    private static bool IsFalse(Expression e)
    {
        return e is ConstantExpression c && !(bool)c.Value;
    }

    private static BigInteger ToBigInteger(object value)
    {
        return value switch
        {
            BigInteger b => b,
            int i => i,
            long l => l,
            decimal m => (BigInteger)m,
            double d => (BigInteger)d,
            float f => (BigInteger)f,
            _ => 0
        };
    }
}