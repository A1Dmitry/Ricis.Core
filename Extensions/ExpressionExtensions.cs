using Ricis.Core.Expressions;
using Ricis.Core.Simplifiers;
using System.Linq.Expressions;
using System.Numerics;

namespace Ricis.Core.Extensions;

public static class ExpressionExtensions
{

    public static Expression<Func<double, double>> Prepare(this Expression expr, ParameterExpression param)
    {
        return Expression.Lambda<Func<double, double>>(expr, param);
    }
    public static double Evaluate(this Expression expr, ParameterExpression param, double value)
    {
        return expr.Prepare(param).Compile()(value);
    }

    public static double Evaluate(this Expression expr, string paramName, double value)
    {
        var lambda = expr.Evaluate( value, paramName);
        return lambda.Compile()();
    }

    public static Expression<Func<double>> Evaluate(this Expression expr, double value, string paramName = null)
    {
        // Используем SubstitutionVisitor для безопасной подмены параметра
        var visitor = new SubstitutionVisitor(value, paramName);
        var body = visitor.Visit(expr);

        // Оптимизация: если выражение стало константой
        if (body is ConstantExpression c)
        {
            switch (c.Value)
            {
                case double d:
                    return Expression.Lambda<Func<double>>(Expression.Constant(Convert.ToDouble(d)));
                case int i:
                    return Expression.Lambda<Func<double>>(Expression.Constant(Convert.ToDouble(i)));
                //default:
                //    try
                //    {
                //        return Expression.Lambda<Func<double>>(Expression.Constant(Convert.ToDouble(c.Value))); ;
                //    }
                //    catch
                //    {
                //        return Expression.Lambda<Func<double>>(Expression.Constant(double.NaN));
                //    }

                //    break;
            }
            return Expression.Lambda<Func<double>>(Expression.Convert(body, typeof(double)));
        }

        return Expression.Lambda<Func<double>>(Expression.Convert(body, typeof(double)));
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
    public static bool TryEvaluate(this Expression expr, string paramName, double value, out double result)
    {
        try
        {
            result = expr.Evaluate(paramName, value);
            // Отсеиваем NaN и бесконечности, чтобы не ломать логику упрощения
            return !double.IsNaN(result) && !double.IsInfinity(result);
        }
        catch
        {
            result = double.NaN;
            return false;
        }
    }


    public static double EvaluateAtPoint(this Expression expr, double value, string paramName = null)
    {
        try
        {
            var visitor = new SubstitutionVisitor(value, paramName);
            var substituted = visitor.Visit(expr);
            var lambda = Expression.Lambda<Func<double>>(Expression.Convert(substituted, typeof(double)));
            return lambda.Compile()();
        }
        catch
        {
            return 1.0;
        }
    }

    public static void AddSingularityIfValid(this
        Expression numerator,
        ParameterExpression param,
        double value,
        List<InfinityExpression> singularities)
    {
        var numAtRoot = numerator.EvaluateAtPoint(value, param.Name);

        var infinity =
            // Полюс C/0 -> Индекс C (числитель)
            InfinityExpression.CreateLazy(numAtRoot == 0.0
                    ? RicisType.InfinityZero
                    : numerator,
                param, value);

        singularities.Add(infinity);
    }

    /// <summary>
    /// Является ли выражение нулем (поддержка всех числовых типов)
    /// </summary>
    public static bool IsZero(this Expression expr) => expr switch
    {
        ConstantExpression c => IsZeroValue(c.Value),
        _ => false
    };

    // Хелпер для поиска параметра (x)
    public static ParameterExpression FindParameter(this Expression expr)
    {
        ParameterExpression found = null;
        IExpressionVisitor visitor = new ExpressionTraverser(node =>
        {
            if (found == null && node is ParameterExpression p)
            {
                found = p;
            }
        });
        visitor.Visit(expr);
        return found;
    }

    public static bool IsTranscendentalCandidate(this Expression expr)
    {
        var hasTrig = false;
        var isComplex = false;

        // Простой обход дерева выражения
        var expressionTraverser = new ExpressionTraverser(node =>
        {
            switch (node)
            {
                case MethodCallExpression call when call.Method.DeclaringType != typeof(Math):
                    return;
                case MethodCallExpression call:
                {
                    var name = call.Method.Name;
                    if (name == "Cos" || name == "Sin" || name == "Tan" ||
                        name == "Cosh" || name == "Sinh" || name == "Tanh")
                    {
                        hasTrig = true;
                    }

                    break;
                }
                case BinaryExpression:
                    isComplex = true; // Есть операции (+, -, *)
                    break;
            }
        });
        expressionTraverser.Visit(expr);

        return hasTrig && isComplex;
    }

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