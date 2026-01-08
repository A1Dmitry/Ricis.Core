using System.Linq.Expressions;
using System.Numerics;

namespace Ricis.Core.Extensions;

public static class ExpressionExtensions
{
    public static double Evaluate(this Expression expr, string paramName, double value)
    {
        var param = Expression.Parameter(typeof(double), paramName);
        var lambda = Expression.Lambda<Func<double, double>>(expr, param);
        return lambda.Compile()(value);
    }
    public static double Evaluate(this Expression expr, ParameterExpression param, double value)
    {
        var lambda = Expression.Lambda<Func<double, double>>(expr, param);
        return lambda.Compile()(value);
    }
    /// <summary>
    /// Определяет, является ли бинарная операция коммутативной (a+b = b+a)
    /// </summary>
    public static bool IsCommutative(this Expression node)
    {
        var nodeType  = node.NodeType;
        return nodeType switch
        {
            ExpressionType.Add => true,
            ExpressionType.Multiply => true,
            ExpressionType.Equal => true,
            ExpressionType.NotEqual => true,
            ExpressionType.AndAlso => true,
            ExpressionType.OrElse => true,
            ExpressionType.And => true,
            ExpressionType.Or => true,
            ExpressionType.Power => true,
            _ => false
        };
    }

    /// <summary>
    /// Лексикографическая нормализация для коммутации (x+y → y+x если y проще)
    /// </summary>
    public static bool ShouldCommute(this Expression left, Expression right)
    {
        var leftScore = left.GetComplexityScore();
        var rightScore = right.GetComplexityScore();
        return leftScore > rightScore;
    }

    /// <summary>
    /// Сложность поддерева для нормализации
    /// </summary>
    private static int GetComplexityScore(this Expression node) => node switch
    {
        ParameterExpression => 1,
        ConstantExpression => 2,
        MemberExpression => 3,
        UnaryExpression u => 4 + u.Operand.GetComplexityScore(),
        BinaryExpression b => 5 + b.Left.GetComplexityScore() + b.Right.GetComplexityScore(),
        MethodCallExpression m => 10 + m.Arguments.Sum(a => a.GetComplexityScore()),
        _ => 20
    };

    /// <summary>
    /// Является ли выражение нулем (поддержка всех числовых типов)
    /// </summary>
    public static bool IsZero(this Expression expr) => expr switch
    {
        ConstantExpression c => IsZeroValue(c.Value),
        _ => false
    };

    /// <summary>
    /// Является ли выражение единицей
    /// </summary>
    public static bool IsOne(this Expression expr) => expr switch
    {
        ConstantExpression c => IsOneValue(c.Value),
        _ => false
    };

    /// <summary>
    /// Безопасное приведение к BigInteger
    /// </summary>
    public static BigInteger ToBigInteger(this object value) => value switch
    {
        BigInteger b => b,
        int i => i,
        long l => l,
        decimal m => (BigInteger)m,
        double d => (BigInteger)d,
        float f => (BigInteger)f,
        sbyte sb => sb,
        short s => s,
        ushort us => us,
        uint ui => ui,
        ulong ul => (BigInteger)ul,
        byte bt => bt,
        char ch => ch,
        _ => 0
    };

    private static bool IsZeroValue(object value) => value switch
    {
        0 or 0L or 0.0 or 0m or 0f => true,
        BigInteger b => b == 0,
        string s => s == "0",
        _ => false
    };

    private static bool IsOneValue(object value) => value switch
    {
        1 or 1L or 1.0 or 1m or 1f => true,
        BigInteger b => b == 1,
        string s => s == "1",
        _ => false
    };
}