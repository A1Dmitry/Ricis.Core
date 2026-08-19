using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

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
            throw new ArgumentException("Sum требует две лямбды ровно с одним параметром.");
        }

        NumericConstants.Register<T>();
        var left = RicisPhasePipeline.Simplify(function) as Expression<Func<T, T>>
            ?? throw new InvalidOperationException("RICIS-конвейер не сохранил лямбду первого слагаемого.");
        var right = RicisPhasePipeline.Simplify(other) as Expression<Func<T, T>>
            ?? throw new InvalidOperationException("RICIS-конвейер не сохранил лямбду второго слагаемого.");
        var reboundRight = new ParameterRebindVisitor(right.Parameters[0], left.Parameters[0]).Visit(right.Body)
            ?? throw new InvalidOperationException("Не удалось связать параметр второго слагаемого.");

        var raw = Expression.Lambda<Func<T, T>>(
            Expression.Add(left.Body, reboundRight),
            left.Parameters[0]);
        var transformed = RicisPhasePipeline.Simplify(raw);
        return transformed as Expression<Func<T, T>>
            ?? throw new InvalidOperationException("RICIS-конвейер не сохранил лямбду суммы.");
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
            throw new ArgumentException("Integral требует две лямбды ровно с одним параметром.");
        }

        var commonParameter = function.Parameters[0];
        var reboundWidth = new ParameterRebindVisitor(width.Parameters[0], commonParameter).Visit(width.Body)
            ?? throw new InvalidOperationException("Не удалось связать параметр ширины диапазона.");

        return BuildIntegral(function, reboundWidth);
    }

    private static Expression<Func<T, T>> BuildIntegral<T>(
        Expression<Func<T, T>> function,
        Expression width)
        where T : INumber<T>
    {
        if (function.Parameters.Count != 1)
        {
            throw new ArgumentException("Integral требует лямбду F ровно с одним параметром.", nameof(function));
        }

        if (width.Type != typeof(T))
        {
            throw new ArgumentException(
                $"Ширина диапазона должна иметь тип {typeof(T).FullName}.", nameof(width));
        }

        NumericConstants.Register<T>();

        // Normalize F before it becomes the index of the geometric strip.
        // This preserves the established L1 -> SP2 priority contract.
        var normalized = RicisPhasePipeline.Simplify(function) as Expression<Func<T, T>>
            ?? throw new InvalidOperationException(
                $"RICIS-конвейер должен сохранить Expression<Func<{typeof(T).Name}, {typeof(T).Name}>> " +
                "при нормализации F для Integral.");

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
                $"RICIS-конвейер должен сохранить Expression<Func<{typeof(T).Name}, {typeof(T).Name}>> " +
                "для геометрического Integral.");
    }

    private sealed class ParameterRebindVisitor : ParameterRebindingVisitorBase
    {
        public ParameterRebindVisitor(ParameterExpression from, ParameterExpression to)
            : base(from, to)
        {
        }
    }
}
