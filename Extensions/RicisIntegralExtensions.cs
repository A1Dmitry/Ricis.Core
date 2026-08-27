using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;
using Ricis.Core.Resources;

namespace Ricis.Core.Extensions;

/// <summary>
/// Geometric RICIS integrals. The operation is the API form of A6:
/// 0_F * infinity_L -> F * L. It never constructs a Riemann sum,
/// a classical limit, a quadrature point, or an antiderivative.
/// </summary>
public static class RicisIntegralExtensions
{
    /// <summary>
    /// Builds an independent deferred sum F + G. The two source lambdas are
    /// normalized separately, rebound to one common parameter, and only then
    /// processed by the ordinary RICIS pipeline.
    /// </summary>
    public static Expression<Func<T, T>> Sum<T>(
        this Expression<Func<T, T>> function,
        Expression<Func<T, T>> other)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(other);
        if (function.Parameters.Count != 1 || other.Parameters.Count != 1)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("report.legacy.6b478c3c5d23"));
        }

        NumericConstants.Register<T>();
        var left = RicisPhasePipeline.Simplify(function) as Expression<Func<T, T>>
            ?? throw new InvalidOperationException(RicisLegacyTextResources.Get("report.legacy.f428e8a726f3"));
        var right = RicisPhasePipeline.Simplify(other) as Expression<Func<T, T>>
            ?? throw new InvalidOperationException(RicisLegacyTextResources.Get("report.legacy.d7e8951c331d"));
        var reboundRight = new ParameterRebindVisitor(right.Parameters[0], left.Parameters[0]).Visit(right.Body)
            ?? throw new InvalidOperationException(RicisLegacyTextResources.Get("report.legacy.e33617d4ce58"));

        var raw = Expression.Lambda<Func<T, T>>(
            Expression.Add(left.Body, reboundRight),
            left.Parameters[0]);
        var transformed = RicisPhasePipeline.Simplify(raw);
        return transformed as Expression<Func<T, T>>
            ?? throw new InvalidOperationException(RicisLegacyTextResources.Get("report.legacy.96664848bdcd"));
    }

    /// <summary>
    /// Builds a geometric RICIS integral for a deferred function F and a
    /// finite range width L. The independent result is the symbolic lambda
    /// F * L, obtained by the geometry of A6.
    /// </summary>
    public static Expression<Func<T, T>> Integral<T>(
        this Expression<Func<T, T>> function,
        T width)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(function);
        return BuildIntegral(function, Expression.Constant(width, typeof(T)));
    }

    /// <summary>
    /// Builds a geometric RICIS integral for deferred function F and a
    /// deferred width L. Both lambdas are rebound to one common parameter;
    /// no operand is numerically evaluated during construction.
    /// </summary>
    public static Expression<Func<T, T>> Integral<T>(
        this Expression<Func<T, T>> function,
        Expression<Func<T, T>> width)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(function);
        ArgumentNullException.ThrowIfNull(width);

        if (function.Parameters.Count != 1 || width.Parameters.Count != 1)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("report.legacy.64a8d95a4a2d"));
        }

        var commonParameter = function.Parameters[0];
        var reboundWidth = new ParameterRebindVisitor(width.Parameters[0], commonParameter).Visit(width.Body)
            ?? throw new InvalidOperationException(RicisLegacyTextResources.Get("report.legacy.3e658e1d7e8e"));

        return BuildIntegral(function, reboundWidth);
    }

    private static Expression<Func<T, T>> BuildIntegral<T>(
        Expression<Func<T, T>> function,
        Expression width)
        where T : INumber<T>
    {
        if (function.Parameters.Count != 1)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("report.legacy.135159f302e4"), nameof(function));
        }

        if (width.Type != typeof(T))
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentUICulture, RicisLegacyTextResources.Get("report.legacy.defac919f5a8"), typeof(T).FullName), nameof(width));
        }

        NumericConstants.Register<T>();

        // Normalize F before it becomes the index of the geometric strip.
        // This preserves the established L1 -> SP2 priority contract.
        var normalized = RicisPhasePipeline.Simplify(function) as Expression<Func<T, T>>
            ?? throw new InvalidOperationException(
                string.Concat(
                    string.Format(CultureInfo.CurrentUICulture, RicisLegacyTextResources.Get("report.legacy.8c78f17a7aee"), typeof(T).Name),
                    RicisLegacyTextResources.Get("report.legacy.31e8a935d63d")));

        // A6 geometry, written directly as its normative derived tree:
        //   0_F * infinity_L -> F * L.
        // InfinityExpression is currently double-domain for root metadata, so
        // the generic public API materializes the A6 result itself instead of
        // coercing T into a fake double singularity.
        var area = Expression.Multiply(normalized.Body, width);
        var raw = Expression.Lambda<Func<T, T>>(area, normalized.Parameters[0]);
        var transformed = RicisPhasePipeline.Simplify(raw);

        return transformed as Expression<Func<T, T>>
            ?? throw new InvalidOperationException(
                string.Concat(
                    string.Format(CultureInfo.CurrentUICulture, RicisLegacyTextResources.Get("report.legacy.8c78f17a7aee"), typeof(T).Name),
                    RicisLegacyTextResources.Get("report.legacy.a5cccbc72b15")));
    }

    private sealed class ParameterRebindVisitor : ParameterRebindingVisitorBase
    {
        public ParameterRebindVisitor(ParameterExpression from, ParameterExpression to)
            : base(from, to)
        {
        }
    }
}
