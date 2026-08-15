using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

namespace Ricis.Core.Extensions;

/// <summary>
/// Analytic mathematical sugar for deferred double-domain RICIS functions.
/// Every extension creates an explicit <see cref="Math"/> method-call node over
/// a normalized expression tree; no input delegate is invoked while the new
/// function is built.
/// </summary>
public static class RicisAnalyticExtensions
{
    /// <summary>
    /// Builds sin(F) as an explicit <see cref="Math.Sin(double)"/> expression node.
    /// </summary>
    public static Expression<Func<double, double>> Sin(this Expression<Func<double, double>> function) =>
        Unary(function, nameof(Math.Sin));

    /// <summary>
    /// Builds cos(F) as an explicit <see cref="Math.Cos(double)"/> expression node.
    /// </summary>
    public static Expression<Func<double, double>> Cos(this Expression<Func<double, double>> function) =>
        Unary(function, nameof(Math.Cos));

    /// <summary>
    /// Builds tan(F) as an explicit <see cref="Math.Tan(double)"/> expression node.
    /// </summary>
    public static Expression<Func<double, double>> Tan(this Expression<Func<double, double>> function) =>
        Unary(function, nameof(Math.Tan));

    /// <summary>
    /// Builds sinh(F) as an explicit <see cref="Math.Sinh(double)"/> expression node.
    /// </summary>
    public static Expression<Func<double, double>> Sinh(this Expression<Func<double, double>> function) =>
        Unary(function, nameof(Math.Sinh));

    /// <summary>
    /// Builds cosh(F) as an explicit <see cref="Math.Cosh(double)"/> expression node.
    /// </summary>
    public static Expression<Func<double, double>> Cosh(this Expression<Func<double, double>> function) =>
        Unary(function, nameof(Math.Cosh));

    /// <summary>
    /// Builds tanh(F) as an explicit <see cref="Math.Tanh(double)"/> expression node.
    /// </summary>
    public static Expression<Func<double, double>> Tanh(this Expression<Func<double, double>> function) =>
        Unary(function, nameof(Math.Tanh));

    /// <summary>
    /// Builds exp(F) as an explicit <see cref="Math.Exp(double)"/> expression node.
    /// </summary>
    public static Expression<Func<double, double>> Exp(this Expression<Func<double, double>> function) =>
        Unary(function, nameof(Math.Exp));

    /// <summary>
    /// Builds the natural logarithm log(F) as an explicit <see cref="Math.Log(double)"/> expression node.
    /// </summary>
    public static Expression<Func<double, double>> Log(this Expression<Func<double, double>> function) =>
        Unary(function, nameof(Math.Log));

    /// <summary>
    /// Builds the decimal logarithm log10(F) as an explicit <see cref="Math.Log10(double)"/> expression node.
    /// </summary>
    public static Expression<Func<double, double>> Log10(this Expression<Func<double, double>> function) =>
        Unary(function, nameof(Math.Log10));

    /// <summary>
    /// Builds sqrt(F) as an explicit <see cref="Math.Sqrt(double)"/> expression node.
    /// </summary>
    public static Expression<Func<double, double>> Sqrt(this Expression<Func<double, double>> function) =>
        Unary(function, nameof(Math.Sqrt));

    /// <summary>
    /// Builds F^p with a constant double exponent p as an explicit
    /// <see cref="Math.Pow(double, double)"/> expression node.
    /// </summary>
    public static Expression<Func<double, double>> Pow(
        this Expression<Func<double, double>> function,
        double exponent)
    {
        var normalized = Normalize(function, nameof(Pow));
        var body = Expression.Call(
            typeof(Math),
            nameof(Math.Pow),
            Type.EmptyTypes,
            normalized.Body,
            Expression.Constant(exponent));
        return Normalize(Expression.Lambda<Func<double, double>>(body, normalized.Parameters[0]), nameof(Pow));
    }

    /// <summary>
    /// Builds F^G from two deferred double-domain functions. Both operands are
    /// normalized and rebound to one shared parameter before the explicit
    /// <see cref="Math.Pow(double, double)"/> node is created.
    /// </summary>
    public static Expression<Func<double, double>> Pow(
        this Expression<Func<double, double>> function,
        Expression<Func<double, double>> exponent)
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(exponent);
        EnsureUnary(function, nameof(Pow));
        EnsureUnary(exponent, nameof(Pow));
        var normalizedFunction = Normalize(function, nameof(Pow));
        var normalizedExponent = Normalize(exponent, nameof(Pow));
        var common = normalizedFunction.Parameters[0];
        var reboundExponent = Rebind(normalizedExponent, common, nameof(Pow));
        var body = Expression.Call(
            typeof(Math),
            nameof(Math.Pow),
            Type.EmptyTypes,
            normalizedFunction.Body,
            reboundExponent);
        return Normalize(Expression.Lambda<Func<double, double>>(body, common), nameof(Pow));
    }

    private static Expression<Func<double, double>> Unary(
        Expression<Func<double, double>> function,
        string methodName)
    {
        var normalized = Normalize(function, methodName);
        var body = Expression.Call(typeof(Math), methodName, Type.EmptyTypes, normalized.Body);
        return Normalize(Expression.Lambda<Func<double, double>>(body, normalized.Parameters[0]), methodName);
    }

    private static Expression<Func<double, double>> Normalize(
        Expression<Func<double, double>> function,
        string operation)
    {
        ArgumentNullException.ThrowIfNull(function);
        EnsureUnary(function, operation);
        var transformed = RicisPhasePipeline.Simplify(function);
        return transformed as Expression<Func<double, double>>
            ?? throw new InvalidOperationException(
                $"RICIS-конвейер должен сохранить Expression<Func<double,double>> для операции {operation}.");
    }

    private static void EnsureUnary(Expression<Func<double, double>> function, string operation)
    {
        if (function.Parameters.Count != 1)
        {
            throw new ArgumentException($"{operation} требует лямбду ровно с одним параметром.");
        }
    }

    private static Expression Rebind(
        Expression<Func<double, double>> expression,
        ParameterExpression target,
        string operation) =>
        new ParameterRebindVisitor(expression.Parameters[0], target).Visit(expression.Body)
        ?? throw new InvalidOperationException($"Не удалось связать параметр показателя для {operation}.");

    private sealed class ParameterRebindVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        public ParameterRebindVisitor(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        protected override Expression VisitExtension(Expression node) =>
            RicisSpecialExpressionRebinder.Rebind(node, Visit);

        protected override Expression VisitParameter(ParameterExpression node) =>
            ReferenceEquals(node, _from) ? _to : base.VisitParameter(node);
    }
}
