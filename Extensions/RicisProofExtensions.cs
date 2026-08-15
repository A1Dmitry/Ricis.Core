using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

namespace Ricis.Core.Extensions;

/// <summary>
/// Proof-oriented transformations for deferred RICIS lambdas.
/// Each operation constructs an independent expression tree, never invokes a
/// delegate while building it, and delegates all normalization to the ordinary
/// RICIS phase pipeline.
/// </summary>
public static class RicisProofExtensions
{
    /// <summary>
    /// Builds the formal composition F∘G by structurally substituting the body
    /// of G for the parameter of F. No numerical evaluation is performed.
    /// </summary>
    public static Expression<Func<T, T>> Compose<T>(
        this Expression<Func<T, T>> outer,
        Expression<Func<T, T>> inner)
        where T : INumber<T>
    {
        var (normalizedOuter, normalizedInner) = NormalizePair(outer, inner, nameof(Compose));
        var body = new ParameterSubstitutionVisitor(
                normalizedOuter.Parameters[0],
                normalizedInner.Body)
            .Visit(normalizedOuter.Body)
            ?? throw new InvalidOperationException("Не удалось подставить тело внутренней функции в Compose.");

        return Normalize(Expression.Lambda<Func<T, T>>(body, normalizedInner.Parameters[0]), "Compose");
    }

    /// <summary>
    /// Explicit proof-reading alias for structural application F[G].
    /// </summary>
    public static Expression<Func<T, T>> At<T>(
        this Expression<Func<T, T>> function,
        Expression<Func<T, T>> argument)
        where T : INumber<T> => function.Compose(argument);

    /// <summary>
    /// Builds the independent difference F−G. If the normalized deferred
    /// expressions have the same RICIS identity, it returns the typed zero.
    /// </summary>
    public static Expression<Func<T, T>> Difference<T>(
        this Expression<Func<T, T>> left,
        Expression<Func<T, T>> right)
        where T : INumber<T>
    {
        var (normalizedLeft, normalizedRight) = NormalizePair(left, right, nameof(Difference));
        var common = normalizedLeft.Parameters[0];
        var reboundRight = Rebind(normalizedRight, common, "Difference");

        Expression body = normalizedLeft.Body.AreEqual(reboundRight)
            ? NumericConstants.ZeroOf(typeof(T))
            : Expression.Subtract(normalizedLeft.Body, reboundRight);

        return Normalize(Expression.Lambda<Func<T, T>>(body, common), "Difference");
    }

    /// <summary>
    /// Builds the independent ratio F/G. The phase-0 identity rule is then
    /// allowed to reduce equal expressions to the typed one before bridges or
    /// singular transformations are considered.
    /// </summary>
    public static Expression<Func<T, T>> Ratio<T>(
        this Expression<Func<T, T>> numerator,
        Expression<Func<T, T>> denominator)
        where T : INumber<T>
    {
        var (normalizedNumerator, normalizedDenominator) = NormalizePair(numerator, denominator, nameof(Ratio));
        var common = normalizedNumerator.Parameters[0];
        var reboundDenominator = Rebind(normalizedDenominator, common, "Ratio");
        return Normalize(
            Expression.Lambda<Func<T, T>>(
                Expression.Divide(normalizedNumerator.Body, reboundDenominator),
                common),
            "Ratio");
    }

    /// <summary>
    /// Builds the independent product F·G. It is ordinary structural product,
    /// distinct from Integral(F,L), whose geometric meaning is the A6 result.
    /// </summary>
    public static Expression<Func<T, T>> Product<T>(
        this Expression<Func<T, T>> left,
        Expression<Func<T, T>> right)
        where T : INumber<T>
    {
        var (normalizedLeft, normalizedRight) = NormalizePair(left, right, nameof(Product));
        var common = normalizedLeft.Parameters[0];
        var reboundRight = Rebind(normalizedRight, common, "Product");
        return Normalize(
            Expression.Lambda<Func<T, T>>(
                Expression.Multiply(normalizedLeft.Body, reboundRight),
                common),
            "Product");
    }

    private static (Expression<Func<T, T>> Left, Expression<Func<T, T>> Right) NormalizePair<T>(
        Expression<Func<T, T>> left,
        Expression<Func<T, T>> right,
        string operation)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        EnsureUnary(left, operation);
        EnsureUnary(right, operation);
        NumericConstants.Register<T>();
        return (Normalize(left, operation), Normalize(right, operation));
    }

    private static Expression<Func<T, T>> Normalize<T>(
        Expression<Func<T, T>> expression,
        string operation)
        where T : INumber<T>
    {
        var transformed = RicisPhasePipeline.Simplify(expression);
        return transformed as Expression<Func<T, T>>
            ?? throw new InvalidOperationException(
                $"RICIS-конвейер должен сохранить Expression<Func<{typeof(T).Name}, {typeof(T).Name}>> " +
                $"для операции {operation}.");
    }

    private static void EnsureUnary<T>(Expression<Func<T, T>> expression, string operation)
        where T : INumber<T>
    {
        if (expression.Parameters.Count != 1)
        {
            throw new ArgumentException($"{operation} требует лямбды ровно с одним параметром.");
        }
    }

    private static Expression Rebind<T>(
        Expression<Func<T, T>> expression,
        ParameterExpression target,
        string operation)
        where T : INumber<T> =>
        new ParameterSubstitutionVisitor(expression.Parameters[0], target).Visit(expression.Body)
        ?? throw new InvalidOperationException($"Не удалось связать параметр правого операнда {operation}.");

    private sealed class ParameterSubstitutionVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _source;
        private readonly Expression _replacement;

        public ParameterSubstitutionVisitor(ParameterExpression source, Expression replacement)
        {
            _source = source;
            _replacement = replacement;
        }

        protected override Expression VisitExtension(Expression node) =>
            RicisSpecialExpressionRebinder.Rebind(node, Visit);

        protected override Expression VisitParameter(ParameterExpression node) =>
            ReferenceEquals(node, _source) ? _replacement : base.VisitParameter(node);
    }
}
