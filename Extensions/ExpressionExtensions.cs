using Ricis.Core.Expressions;
using Ricis.Core.Simplifiers;
using Ricis.Core.Resources;
using System.Linq.Expressions;
using System.Numerics;

namespace Ricis.Core.Extensions;

/// <summary>
/// Represents the RICIS public type <c>ExpressionExtensions</c>.
/// </summary>
public static class ExpressionExtensions
{

    /// <summary>
    /// Executes <c>Prepare</c> for the RICIS expression model.
    /// </summary>
    public static Expression<Func<double, double>> Prepare(this Expression expr, ParameterExpression param)
    {
        return Expression.Lambda<Func<double, double>>(expr, param);
    }

    /// <summary>
    /// Creates a typed lambda for an ordinary finite RICIS expression. The
    /// constraint permits <see cref="System.Numerics.BigInteger"/> and user
    /// scalar types that implement .NET generic math.
    /// </summary>
    public static Expression<Func<T, T>> Prepare<T>(this Expression expr, ParameterExpression param)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(expr);
        ArgumentNullException.ThrowIfNull(param);
        NumericConstants.Register<T>();

        if (expr.Type != typeof(T) || param.Type != typeof(T))
        {
            throw new ArgumentException(
                RicisLegacyTextResources.Format("report.legacy.986f2fab61ab", ("typeof(T).FullName", typeof(T).FullName)));
        }

        return Expression.Lambda<Func<T, T>>(expr, param);
    }

    /// <summary>
    /// Compiles an already derived, finite RICIS expression without converting
    /// it to <see cref="double"/>. This preserves arbitrary precision and the
    /// semantics of custom <c>INumber&lt;TSelf&gt;</c> scalar types.
    /// </summary>
    public static Func<T, T> CompileFinite<T>(this Expression expr, ParameterExpression param)
        where T : INumber<T> => expr.Prepare<T>(param).Compile();

    /// <summary>
    /// Executes <c>Evaluate</c> for the RICIS expression model.
    /// </summary>
    public static double Evaluate(this Expression expr, ParameterExpression param, double value)
    {
        return expr.Prepare(param).Compile()(value);
    }

    /// <summary>
    /// Evaluates a finite expression with a generic-math value. This API is
    /// intentionally separate from double-based singularity/root discovery.
    /// </summary>
    public static T EvaluateFinite<T>(this Expression expr, ParameterExpression param, T value)
        where T : INumber<T> => expr.CompileFinite<T>(param)(value);

    /// <summary>
    /// Executes <c>Evaluate</c> for the RICIS expression model.
    /// </summary>
    public static double Evaluate(this Expression expr, string paramName, double value)
    {
        var lambda = expr.Evaluate( value, paramName);
        return lambda.Compile()();
    }

    /// <summary>
    /// Executes <c>Evaluate</c> for the RICIS expression model.
    /// </summary>
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
        // Reordering is sound only for built-in arithmetic operations. User-defined
        // operators and logical nodes may carry side effects or custom semantics.
        return node is BinaryExpression { Method: null } binary &&
               binary.NodeType is ExpressionType.Add or ExpressionType.Multiply;
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
    /// Attempts to <c>Evaluate</c> within the RICIS model.
    /// </summary>
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


    /// <summary>
    /// Executes <c>EvaluateAtPoint</c> for the RICIS expression model.
    /// </summary>
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
            return Double.NaN;
        }
    }

    /// <summary>
    /// Adds a certified A1 pole while retaining the parent numerator expression
    /// as its deferred RICIS index. Root discovery and the preceding A4
    /// classification are owned by <c>RicisTransformVisitor</c>; this helper
    /// performs no numerical evaluation, tolerance-based index rewrite or
    /// singularity classification.
    /// </summary>
    public static void AddSingularityIfValid(this
        Expression numerator,
        ParameterExpression param,
        double value,
        List<InfinityExpression> singularities)
    {
        ArgumentNullException.ThrowIfNull(numerator);
        ArgumentNullException.ThrowIfNull(param);
        ArgumentNullException.ThrowIfNull(singularities);

        singularities.Add(new PoleInfinityExpression(numerator, [(param, value)], []));
    }

    /// <summary>
    /// Determines whether an expression denotes zero, including a RICIS indexed
    /// zero <c>0_F</c> whose deferred index is retained for symbolic work.
    /// </summary>
    public static bool IsZero(this Expression expr) => expr switch
    {
        ZeroInfinityExpression => true,
        UnaryExpression
        {
            NodeType: ExpressionType.Negate or ExpressionType.UnaryPlus,
            Operand: ZeroInfinityExpression
        } => true,
        ConstantExpression c => IsZeroValue(c.Value),
        _ => false
    };

    // Хелпер для поиска параметра (x)
    /// <summary>
    /// Executes <c>FindParameter</c> for the RICIS expression model.
    /// </summary>
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

    /// <summary>
    /// Determines whether <c>IsTranscendentalCandidate</c> holds for the supplied RICIS expression.
    /// </summary>
    public static bool IsTranscendentalCandidate(this Expression expr)
    {
        var hasTranscendental = false;

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
                    // FIX: Добавлены Exp, Log, Log10, Sqrt, Pow (если степень не целая)
                    if (name == "Cos" || name == "Sin" || name == "Tan" ||
                        name == "Cosh" || name == "Sinh" || name == "Tanh" ||
                        name == "Exp" || name == "Log" || name == "Log10" ||
                        name == "Sqrt" || name == "Pow")
                    {
                        hasTranscendental = true;
                    }

                    break;
                }
                case BinaryExpression:
                    _ = true; // Есть операции (+, -, *)
                    break;
            }
        });
        expressionTraverser.Visit(expr);

        // Если есть трансцендентная функция, считаем кандидатом
        return hasTranscendental;
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
        _ => NumericConstants.IsZero(value)
    };

    private static bool IsOneValue(object value) => value switch
    {
        1 or 1L or 1.0 or 1m or 1f => true,
        BigInteger b => b == 1,
        string s => s == "1",
        _ => NumericConstants.IsOne(value)
    };
}