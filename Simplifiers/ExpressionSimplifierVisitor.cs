using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Extensions;

namespace Ricis.Core.Simplifiers;

public sealed class ExpressionSimplifierVisitor : ExpressionVisitor
{
    private readonly Dictionary<string, ParameterExpression> _parameters = new();


    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        // Базовые алгебраические тождества
        switch (node.NodeType)
        {
            case ExpressionType.Add when IsZero(left): return right;
            case ExpressionType.Add when IsZero(right): return left;
            case ExpressionType.Multiply when IsZero(left) || IsZero(right): return Expression.Constant(0, node.Type);
            case ExpressionType.Multiply when IsOne(left): return right;
            case ExpressionType.Multiply when IsOne(right): return left;
            case ExpressionType.Divide when IsZero(left): return left;
            case ExpressionType.Divide when IsOne(right): return left;
        }

        // Нормализация: x+x → 2*x, x*x → Pow(x,2)
        if (AreIdentical(left, right))
            return node.NodeType switch
            {
                ExpressionType.Add => Expression.Multiply(Expression.Constant(2, node.Type), left),
                ExpressionType.Multiply => CreatePower(left, 2),
                _ => node.Update(left, node.Conversion, right)
            };

        // Коммутивность (нормализация порядка)
        if (node.IsCommutative() && ShouldCommute(left, right))
            return node.Update(right, node.Conversion, left);

        // Константы
        if (left is ConstantExpression lc && right is ConstantExpression rc)
            return SimplifyConstants(node.NodeType, lc.Value, rc.Value);

        // Сложение/умножение дробей
        if (IsFraction(left) && IsFraction(right))
            return node.NodeType switch
            {
                ExpressionType.Add => SimplifyFractionSum(GetFraction(left), GetFraction(right)),
                ExpressionType.Multiply => SimplifyFractionProduct(GetFraction(left), GetFraction(right)),
                _ => node.Update(left, node.Conversion, right)
            };

        // Распределительный закон: (a+b)*c → a*c + b*c
        if (node.NodeType == ExpressionType.Multiply && IsSum(left))
            return DistributeMultiplySum(node);

        return node.Update(left, node.Conversion, right);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        var operand = Visit(node.Operand);

        // Двойное отрицание
        if (node.NodeType == ExpressionType.Negate && operand is UnaryExpression innerNegate &&
            innerNegate.NodeType == ExpressionType.Negate)
            return innerNegate.Operand;

        if (operand is ConstantExpression c)
            return SimplifyConstantsUnary(node.NodeType, c.Value);

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


    protected override Expression VisitConditional(ConditionalExpression node)
    {
        var test = Visit(node.Test);
        var ifTrue = Visit(node.IfTrue);
        var ifFalse = Visit(node.IfFalse);

        if (test is ConstantExpression tc && (bool)tc.Value) return ifTrue;
        if (test is ConstantExpression tf && !(bool)tf.Value) return ifFalse;

        return node.Update(test, ifTrue, ifFalse);
    }

    internal Expression VisitLogical(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        // Идемпотентность: x && x → x, x || x → x
        if (AreIdentical(left, right))
            return node.NodeType == ExpressionType.AndAlso ? left : right;

        // x && true → x, x || false → x
        if (node.NodeType == ExpressionType.AndAlso)
        {
            if (IsTrue(right)) return left;
            if (IsFalse(right)) return Expression.Constant(false, node.Type);
        }
        else
        {
            if (IsTrue(right)) return right;
            if (IsFalse(right)) return left;
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

    private bool AreIdentical(Expression a, Expression b)
    {
        // Простое сравнение с кэшированием для одинаковых поддеревьев
        return ReferenceEquals(a, b) || NormalizeForComparison(a) == NormalizeForComparison(b);
    }

    private string NormalizeForComparison(Expression node)
    {
        return node switch
        {
            ParameterExpression p => $"P{p.Name}",
            ConstantExpression c => $"C{c.Value}",
            _ => $"{node.NodeType}"
        };
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

    private Expression SimplifyConstants(ExpressionType op, object l, object r)
    {
        var left = ToBigInteger(l);
        var right = ToBigInteger(r);
        return op switch
        {
            ExpressionType.Add => Expression.Constant(left + right),
            ExpressionType.Subtract => Expression.Constant(left - right),
            ExpressionType.Multiply => Expression.Constant(left * right),
            ExpressionType.Divide => SimplifyFraction(left, right),
            ExpressionType.Power => Expression.Constant(BigInteger.Pow(left, (int)right)),
            _ => throw new ArgumentException()
        };
    }

    private static Expression SimplifyFraction(BigInteger num, BigInteger den)
    {
        if (den == 0) throw new DivideByZeroException();
        if (num == 0) return Expression.Constant(BigInteger.Zero);

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

    private Expression CreatePower(Expression @base, int exponent)
    {
        return Expression.Power(@base, Expression.Constant(exponent));
    }

    private static bool IsZero(Expression e)
    {
        return e is ConstantExpression c && ToBigInteger(c.Value) == 0;
    }

    private static bool IsOne(Expression e)
    {
        return e is ConstantExpression c && ToBigInteger(c.Value) == 1;
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