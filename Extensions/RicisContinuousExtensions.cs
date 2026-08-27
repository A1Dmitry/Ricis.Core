using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;
using Ricis.Core.Resources;

namespace Ricis.Core.Extensions;

/// <summary>
/// Continuous piecewise mathematical sugar for deferred RICIS lambdas.
/// Every operation constructs a finite <see cref="ConditionalExpression"/>
/// tree, performs no delegate invocation, and applies the ordinary RICIS
/// phase pipeline only after the tree has been constructed.
/// </summary>
public static class RicisContinuousExtensions
{
    /// <summary>
    /// Builds the absolute-value expression |F| as the finite piecewise tree
    /// <c>F &lt; 0 ? −F : F</c>. For unsigned built-in scalar types, |F| is F.
    /// </summary>
    public static Expression<Func<T, T>> Abs<T>(this Expression<Func<T, T>> function)
        where T : INumber<T>
    {
        var normalized = NormalizeUnary(function, nameof(Abs));
        if (IsBuiltInUnsigned(typeof(T)))
        {
            return normalized;
        }

        var zero = NumericConstants.ZeroOf(typeof(T));
        var negated = UsesCheckedNegation(typeof(T))
            ? Expression.NegateChecked(normalized.Body)
            : Expression.Negate(normalized.Body);
        var body = Expression.Condition(
            Expression.LessThan(normalized.Body, zero),
            negated,
            normalized.Body);
        return Normalize(Expression.Lambda<Func<T, T>>(body, normalized.Parameters[0]), nameof(Abs));
    }

    /// <summary>
    /// Builds the pointwise minimum min(F,G) as a finite conditional expression.
    /// Structurally identical normalized operands return their common tree.
    /// </summary>
    public static Expression<Func<T, T>> Min<T>(
        this Expression<Func<T, T>> left,
        Expression<Func<T, T>> right)
        where T : INumber<T>
    {
        var (normalizedLeft, normalizedRight) = NormalizePair(left, right, nameof(Min));
        return BuildExtremum(normalizedLeft, normalizedRight, chooseMaximum: false, nameof(Min));
    }

    /// <summary>
    /// Builds the pointwise maximum max(F,G) as a finite conditional expression.
    /// Structurally identical normalized operands return their common tree.
    /// </summary>
    public static Expression<Func<T, T>> Max<T>(
        this Expression<Func<T, T>> left,
        Expression<Func<T, T>> right)
        where T : INumber<T>
    {
        var (normalizedLeft, normalizedRight) = NormalizePair(left, right, nameof(Max));
        return BuildExtremum(normalizedLeft, normalizedRight, chooseMaximum: true, nameof(Max));
    }

    /// <summary>
    /// Builds the pointwise clamp of F to finite constant bounds as
    /// min(max(F, lower), upper). Bounds are represented as typed expression
    /// constants and are never used to invoke F during construction.
    /// </summary>
    public static Expression<Func<T, T>> Clamp<T>(
        this Expression<Func<T, T>> function,
        T lower,
        T upper)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(function);
        NumericConstants.Register<T>();
        ExpressionValidation.EnsureUnary(function, nameof(Clamp));
        var parameter = function.Parameters[0];
        var lowerLambda = Expression.Lambda<Func<T, T>>(
            Expression.Constant(lower, typeof(T)), parameter);
        var upperLambda = Expression.Lambda<Func<T, T>>(
            Expression.Constant(upper, typeof(T)), parameter);
        return function.Clamp(lowerLambda, upperLambda);
    }

    /// <summary>
    /// Builds the pointwise clamp min(max(F,L),U) for three deferred lambdas.
    /// Each operand is normalized and rebound to one shared parameter without
    /// numerical evaluation or a limit construction.
    /// </summary>
    public static Expression<Func<T, T>> Clamp<T>(
        this Expression<Func<T, T>> function,
        Expression<Func<T, T>> lower,
        Expression<Func<T, T>> upper)
        where T : INumber<T>
    {
        var (normalizedFunction, normalizedLower, normalizedUpper) =
            NormalizeTriple(function, lower, upper, nameof(Clamp));

        if (normalizedLower.Body.AreEqual(normalizedUpper.Body))
        {
            return normalizedLower;
        }

        var lowerBounded = Expression.Condition(
            Expression.GreaterThan(normalizedFunction.Body, normalizedLower.Body),
            normalizedFunction.Body,
            normalizedLower.Body);
        var body = Expression.Condition(
            Expression.GreaterThan(lowerBounded, normalizedUpper.Body),
            normalizedUpper.Body,
            lowerBounded);
        return Normalize(Expression.Lambda<Func<T, T>>(body, normalizedFunction.Parameters[0]), nameof(Clamp));
    }

    /// <summary>
    /// Builds the continuous positive part F⁺ = max(F,0). The result is a
    /// finite conditional tree and not a call to a numerical library routine.
    /// </summary>
    public static Expression<Func<T, T>> PositivePart<T>(this Expression<Func<T, T>> function)
        where T : INumber<T>
    {
        var normalized = NormalizeUnary(function, nameof(PositivePart));
        if (IsBuiltInUnsigned(typeof(T)))
        {
            return normalized;
        }

        var zero = NumericConstants.ZeroOf(typeof(T));
        var body = Expression.Condition(
            Expression.GreaterThan(normalized.Body, zero),
            normalized.Body,
            zero);
        return Normalize(Expression.Lambda<Func<T, T>>(body, normalized.Parameters[0]), nameof(PositivePart));
    }

    /// <summary>
    /// Builds the continuous negative part F⁻ = min(F,0). The result is a
    /// finite conditional tree and preserves the scalar type T exactly.
    /// </summary>
    public static Expression<Func<T, T>> NegativePart<T>(this Expression<Func<T, T>> function)
        where T : INumber<T>
    {
        var normalized = NormalizeUnary(function, nameof(NegativePart));
        var zero = NumericConstants.ZeroOf(typeof(T));
        if (IsBuiltInUnsigned(typeof(T)))
        {
            return Expression.Lambda<Func<T, T>>(zero, normalized.Parameters[0]);
        }

        var body = Expression.Condition(
            Expression.LessThan(normalized.Body, zero),
            normalized.Body,
            zero);
        return Normalize(Expression.Lambda<Func<T, T>>(body, normalized.Parameters[0]), nameof(NegativePart));
    }

    /// <summary>
    /// Builds the pointwise distance |F−G| from two independent deferred
    /// lambdas. The difference is normalized before the absolute-value tree
    /// is formed, so the ordinary RICIS identity and algebraic priorities hold.
    /// </summary>
    public static Expression<Func<T, T>> Distance<T>(
        this Expression<Func<T, T>> left,
        Expression<Func<T, T>> right)
        where T : INumber<T> => left.Difference(right).Abs();

    private static Expression<Func<T, T>> BuildExtremum<T>(
        Expression<Func<T, T>> left,
        Expression<Func<T, T>> right,
        bool chooseMaximum,
        string operation)
        where T : INumber<T>
    {
        var common = left.Parameters[0];
        var reboundRight = Rebind(right, common, operation);
        if (left.Body.AreEqual(reboundRight))
        {
            return left;
        }

        var body = Expression.Condition(
            Expression.GreaterThan(left.Body, reboundRight),
            chooseMaximum ? left.Body : reboundRight,
            chooseMaximum ? reboundRight : left.Body);
        return Normalize(Expression.Lambda<Func<T, T>>(body, common), operation);
    }

    private static Expression<Func<T, T>> NormalizeUnary<T>(
        Expression<Func<T, T>> function,
        string operation)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(function);
        ExpressionValidation.EnsureUnary(function, operation);
        NumericConstants.Register<T>();
        return Normalize(function, operation);
    }

    private static (Expression<Func<T, T>> Left, Expression<Func<T, T>> Right) NormalizePair<T>(
        Expression<Func<T, T>> left,
        Expression<Func<T, T>> right,
        string operation)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        ExpressionValidation.EnsureUnary(left, operation);
        ExpressionValidation.EnsureUnary(right, operation);
        NumericConstants.Register<T>();
        return (Normalize(left, operation), Normalize(right, operation));
    }

    private static (Expression<Func<T, T>> Function, Expression<Func<T, T>> Lower, Expression<Func<T, T>> Upper)
        NormalizeTriple<T>(
            Expression<Func<T, T>> function,
            Expression<Func<T, T>> lower,
            Expression<Func<T, T>> upper,
            string operation)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(lower);
        ArgumentNullException.ThrowIfNull(upper);
        ExpressionValidation.EnsureUnary(function, operation);
        ExpressionValidation.EnsureUnary(lower, operation);
        ExpressionValidation.EnsureUnary(upper, operation);
        NumericConstants.Register<T>();

        var normalizedFunction = Normalize(function, operation);
        var normalizedLower = Normalize(lower, operation);
        var normalizedUpper = Normalize(upper, operation);
        var common = normalizedFunction.Parameters[0];
        return (
            normalizedFunction,
            Expression.Lambda<Func<T, T>>(Rebind(normalizedLower, common, operation), common),
            Expression.Lambda<Func<T, T>>(Rebind(normalizedUpper, common, operation), common));
    }

    private static Expression<Func<T, T>> Normalize<T>(
        Expression<Func<T, T>> expression,
        string operation)
        where T : INumber<T>
    {
        var transformed = RicisPhasePipeline.Simplify(expression);
        return transformed as Expression<Func<T, T>>
            ?? throw new InvalidOperationException(
                RicisLegacyTextResources.Get("report.legacy.8c78f17a7aee") +
                typeof(T).Name + ", " + typeof(T).Name + ">> " +
                RicisLegacyTextResources.Format("report.legacy.faeacf87abee", ("operation", operation)));
    }


    private static Expression Rebind<T>(
        Expression<Func<T, T>> expression,
        ParameterExpression target,
        string operation)
        where T : INumber<T> =>
        new ParameterRebindVisitor(expression.Parameters[0], target).Visit(expression.Body)
        ?? throw new InvalidOperationException(RicisLegacyTextResources.Format("report.legacy.b4b9b4bafab5", ("operation", operation)));

    private static bool IsBuiltInUnsigned(Type type) =>
        type == typeof(byte) || type == typeof(ushort) || type == typeof(uint) ||
        type == typeof(ulong) || type == typeof(nuint) || type == typeof(UInt128);

    private static bool UsesCheckedNegation(Type type) =>
        type == typeof(sbyte) || type == typeof(short) || type == typeof(int) ||
        type == typeof(long) || type == typeof(nint) || type == typeof(Int128);

    private sealed class ParameterRebindVisitor : ParameterRebindingVisitorBase
    {
        public ParameterRebindVisitor(ParameterExpression from, ParameterExpression to)
            : base(from, to)
        {
        }
    }
}
