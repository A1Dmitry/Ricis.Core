using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

namespace Ricis.Core.Extensions;

/// <summary>
/// Deferred RICIS builders for the compound-interest formula
/// P=S·(1+r/100)^n. The extensions construct formula trees only; they do not
/// perform a forecast, invoke source delegates, or introduce limits.
/// </summary>
public static class RicisCompoundInterestExtensions
{
    /// <summary>
    /// Builds P=S·(1+r/100)^n from deferred principal S, deferred rate r in
    /// percent, and a non-negative integer number of periods n. The scalar
    /// domain must represent r/100 without integer truncation; known integral
    /// .NET domains are rejected explicitly instead of silently losing a rate.
    /// The power is represented by repeated typed multiplication without a
    /// conversion to <see cref="double"/>.
    /// </summary>
    public static Expression<Func<T, T>> CompoundInterest<T>(
        this Expression<Func<T, T>> principal,
        Expression<Func<T, T>> ratePercent,
        int periods)
        where T : INumber<T>
    {
        if (periods < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(periods), "Число периодов должно быть неотрицательным.");
        }

        var (normalizedPrincipal, normalizedRate) = NormalizePair(principal, ratePercent, nameof(CompoundInterest));
        var common = normalizedPrincipal.Parameters[0];
        var reboundRate = Rebind(normalizedRate, common, nameof(CompoundInterest));
        EnsureRateIsRepresentable<T>(reboundRate, periods);
        var growth = Expression.Add(
            NumericConstants.OneOf(typeof(T)),
            Expression.Divide(reboundRate, Expression.Constant(T.CreateChecked(100), typeof(T))));

        Expression multiplier = NumericConstants.OneOf(typeof(T));
        for (var period = 0; period < periods; period++)
        {
            multiplier = Expression.Multiply(multiplier, growth);
        }

        return Normalize(
            Expression.Lambda<Func<T, T>>(
                Expression.Multiply(normalizedPrincipal.Body, multiplier),
                common),
            nameof(CompoundInterest));
    }

    /// <summary>
    /// Builds P=S·(1+r/100)^n from deferred principal S, a typed constant rate
    /// r in percent, and a non-negative integer number of periods n.
    /// </summary>
    public static Expression<Func<T, T>> CompoundInterest<T>(
        this Expression<Func<T, T>> principal,
        T ratePercent,
        int periods)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(principal);
        if (principal.Parameters.Count != 1)
        {
            throw new ArgumentException("CompoundInterest требует лямбду S ровно с одним параметром.", nameof(principal));
        }

        var constantRate = Expression.Lambda<Func<T, T>>(
            Expression.Constant(ratePercent, typeof(T)),
            principal.Parameters[0]);
        return principal.CompoundInterest(constantRate, periods);
    }

    /// <summary>
    /// Builds P=S·(1+r/100)^n with a deferred double-domain period n. The
    /// result preserves <see cref="Math.Pow(double, double)"/> explicitly in
    /// the expression tree, allowing non-integer periods without hidden
    /// evaluation during construction.
    /// </summary>
    public static Expression<Func<double, double>> CompoundInterest(
        this Expression<Func<double, double>> principal,
        Expression<Func<double, double>> ratePercent,
        Expression<Func<double, double>> periods)
    {
        var (normalizedPrincipal, normalizedRate, normalizedPeriods) =
            NormalizeTriple(principal, ratePercent, periods, nameof(CompoundInterest));
        var common = normalizedPrincipal.Parameters[0];
        var growth = Expression.Add(
            Expression.Constant(1.0),
            Expression.Divide(normalizedRate.Body, Expression.Constant(100.0)));
        var factor = Expression.Call(
            typeof(Math),
            nameof(Math.Pow),
            Type.EmptyTypes,
            growth,
            normalizedPeriods.Body);

        return Normalize(
            Expression.Lambda<Func<double, double>>(
                Expression.Multiply(normalizedPrincipal.Body, factor),
                common),
            nameof(CompoundInterest));
    }

    private static void EnsureRateIsRepresentable<T>(Expression ratePercent, int periods)
        where T : INumber<T>
    {
        if (periods == 0 || !IsKnownIntegralDomain(typeof(T)))
        {
            return;
        }

        if (TryGetStaticRate(ratePercent, out T rate) &&
            rate % T.CreateChecked(100) == T.Zero)
        {
            return;
        }

        throw new NotSupportedException(
            $"CompoundInterest<{typeof(T).Name}> не может точно представить процентную дробь r/100 для данной отложенной ставки. " +
            "Используйте decimal, double, Half, custom rational scalar либо целочисленную ставку, кратную 100%. ");
    }

    private static bool TryGetStaticRate<T>(Expression expression, out T rate)
        where T : INumber<T>
    {
        if (expression is ConstantExpression { Value: T constant })
        {
            rate = constant;
            return true;
        }

        // A C# lambda such as `_ => new BigInteger(100)` is represented as a
        // NewExpression rather than a ConstantExpression. Inspecting its literal
        // constructor argument keeps the operation structural and invokes no
        // user delegate.
        if (typeof(T) == typeof(BigInteger) &&
            expression is NewExpression { Constructor.DeclaringType: var declaringType, Arguments.Count: 1 } created &&
            declaringType == typeof(BigInteger) &&
            created.Arguments[0] is ConstantExpression { Value: not null } argument)
        {
            try
            {
                rate = (T)(object)new BigInteger(Convert.ToInt64(argument.Value));
                return true;
            }
            catch (Exception)
            {
                // A non-integral or unsupported literal remains explicitly
                // rejected by the caller rather than being approximated.
            }
        }

        rate = T.Zero;
        return false;
    }

    private static bool IsKnownIntegralDomain(Type type) =>
        type == typeof(byte) || type == typeof(sbyte) ||
        type == typeof(short) || type == typeof(ushort) ||
        type == typeof(int) || type == typeof(uint) ||
        type == typeof(long) || type == typeof(ulong) ||
        type == typeof(nint) || type == typeof(nuint) ||
        type == typeof(Int128) || type == typeof(UInt128) ||
        type == typeof(BigInteger);

    private static (Expression<Func<T, T>> Principal, Expression<Func<T, T>> Rate) NormalizePair<T>(
        Expression<Func<T, T>> principal,
        Expression<Func<T, T>> rate,
        string operation)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(rate);
        EnsureUnary(principal, operation);
        EnsureUnary(rate, operation);
        NumericConstants.Register<T>();
        return (Normalize(principal, operation), Normalize(rate, operation));
    }

    private static (Expression<Func<double, double>> Principal, Expression<Func<double, double>> Rate,
                    Expression<Func<double, double>> Periods) NormalizeTriple(
        Expression<Func<double, double>> principal,
        Expression<Func<double, double>> rate,
        Expression<Func<double, double>> periods,
        string operation)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(rate);
        ArgumentNullException.ThrowIfNull(periods);
        EnsureUnary(principal, operation);
        EnsureUnary(rate, operation);
        EnsureUnary(periods, operation);

        var normalizedPrincipal = Normalize(principal, operation);
        var normalizedRate = Normalize(rate, operation);
        var normalizedPeriods = Normalize(periods, operation);
        var common = normalizedPrincipal.Parameters[0];
        return (
            normalizedPrincipal,
            Expression.Lambda<Func<double, double>>(Rebind(normalizedRate, common, operation), common),
            Expression.Lambda<Func<double, double>>(Rebind(normalizedPeriods, common, operation), common));
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
            throw new ArgumentException($"{operation} требует лямбду ровно с одним параметром.");
        }
    }

    private static Expression Rebind<T>(
        Expression<Func<T, T>> expression,
        ParameterExpression target,
        string operation)
        where T : INumber<T> =>
        new ParameterRebindVisitor(expression.Parameters[0], target).Visit(expression.Body)
        ?? throw new InvalidOperationException($"Не удалось связать параметр для {operation}.");

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
