using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;
using Ricis.Core.Resources;

namespace Ricis.Core.Extensions;

/// <summary>
/// Formal RICIS derivatives for generic-math scalar lambdas.
/// The implementation performs symbolic tree rearrangement only: it never
/// uses limits, L'Hopital's rule, numerical differences, or approximation.
/// Any singularity created by the new tree is subsequently handled only by
/// the ordinary RICIS phase pipeline and its O(1) bridges.
/// </summary>
public static class RicisDerivativeExtensions
{
    /// <summary>
    /// Builds the formal derivative dF/dt for a single-variable lambda.
    /// </summary>
    public static Expression<Func<T, T>> DxDt<T>(this Expression<Func<T, T>> function)
        where T : INumber<T> => BuildDerivative(function);

    /// <summary>
    /// Alias for <see cref="DxDt{T}(Expression{Func{T, T}})"/>.
    /// </summary>
    public static Expression<Func<T, T>> Derivative<T>(this Expression<Func<T, T>> function)
        where T : INumber<T> => BuildDerivative(function);

    private static Expression<Func<T, T>> BuildDerivative<T>(Expression<Func<T, T>> function)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(function);
        if (function.Parameters.Count != 1)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("report.legacy.019ca3b2ab39"), nameof(function));
        }

        NumericConstants.Register<T>();

        // L1/SP2 are prior to every subsequent RICIS operation, including
        // symbolic differentiation: d(t/t)/dt must begin from d(1)/dt.
        var normalized = RicisPhasePipeline.Simplify(function) as Expression<Func<T, T>>
            ?? throw new InvalidOperationException(
                RicisLegacyTextResources.Get("report.legacy.8c78f17a7aee") +
                typeof(T).Name + ", " + typeof(T).Name + ">> " +
                RicisLegacyTextResources.Get("report.legacy.2d06e611273a"));

        var time = normalized.Parameters[0];
        var derivativeBody = new FormalDerivativeBuilder<T>(time).Build(normalized.Body);
        var raw = Expression.Lambda<Func<T, T>>(derivativeBody, time);
        var transformed = RicisPhasePipeline.Simplify(raw);

        return transformed as Expression<Func<T, T>>
            ?? throw new InvalidOperationException(
                RicisLegacyTextResources.Get("report.legacy.8c78f17a7aee") +
                typeof(T).Name + ", " + typeof(T).Name + ">> " +
                RicisLegacyTextResources.Get("report.legacy.adfc67b0c234"));
    }

    private sealed class FormalDerivativeBuilder<T> where T : INumber<T>
    {
        private readonly ParameterExpression _time;

        public FormalDerivativeBuilder(ParameterExpression time)
        {
            _time = time;
        }

        public Expression Build(Expression expression)
        {
            switch (expression)
            {
                case ConstantExpression:
                    return Zero();

                case ParameterExpression parameter:
                    return ReferenceEquals(parameter, _time) ? One() : Zero();

                case UnaryExpression { NodeType: ExpressionType.Negate } unary:
                    return Expression.Negate(Build(unary.Operand));

                case UnaryExpression { NodeType: ExpressionType.Convert } unary:
                    return unary.Type == typeof(T)
                        ? Build(unary.Operand)
                        : Deferred(expression);

                case BinaryExpression binary:
                    return BuildBinary(binary);

                case MethodCallExpression call:
                    return BuildMethod(call);

                default:
                    return Deferred(expression);
            }
        }

        private Expression BuildMethod(MethodCallExpression call)
        {
            // Transcendental functions are intentionally a double-only layer:
            // they are absent from the base INumber<T> contract. The rules
            // below are exact symbolic rewrites plus the chain rule, never
            // numerical slopes or limit evaluation.
            if (typeof(T) != typeof(double) || call.Method.DeclaringType != typeof(Math))
            {
                return Deferred(call);
            }

            if (call.Method.Name == nameof(Math.Pow) && call.Arguments.Count == 2 &&
                call.Arguments[1] is ConstantExpression { Value: double exponent })
            {
                var powerArgument = call.Arguments[0];
                var powerLocal = Expression.Multiply(
                    Expression.Constant(exponent),
                    Expression.Call(typeof(Math), nameof(Math.Pow), Type.EmptyTypes,
                        powerArgument, Expression.Constant(exponent - 1.0)));
                return Expression.Multiply(powerLocal, Build(powerArgument));
            }

            if (call.Arguments.Count != 1)
            {
                return Deferred(call);
            }

            var argument = call.Arguments[0];
            var dArgument = Build(argument);
            Expression local;
            switch (call.Method.Name)
            {
                case nameof(Math.Sin):
                    local = Expression.Call(typeof(Math), nameof(Math.Cos), Type.EmptyTypes, argument);
                    break;
                case nameof(Math.Cos):
                    local = Expression.Negate(Expression.Call(typeof(Math), nameof(Math.Sin), Type.EmptyTypes, argument));
                    break;
                case nameof(Math.Tan):
                    var cosine = Expression.Call(typeof(Math), nameof(Math.Cos), Type.EmptyTypes, argument);
                    local = Expression.Divide(Expression.Constant(1.0), Expression.Multiply(cosine, cosine));
                    break;
                case nameof(Math.Exp):
                    local = call;
                    break;
                case nameof(Math.Log):
                    local = Expression.Divide(Expression.Constant(1.0), argument);
                    break;
                case nameof(Math.Log10):
                    local = Expression.Divide(
                        Expression.Constant(1.0),
                        Expression.Multiply(argument, Expression.Call(typeof(Math), nameof(Math.Log), Type.EmptyTypes, Expression.Constant(10.0))));
                    break;
                case nameof(Math.Sqrt):
                    local = Expression.Divide(Expression.Constant(1.0), Expression.Multiply(Expression.Constant(2.0), call));
                    break;
                case nameof(Math.Sinh):
                    local = Expression.Call(typeof(Math), nameof(Math.Cosh), Type.EmptyTypes, argument);
                    break;
                case nameof(Math.Cosh):
                    local = Expression.Call(typeof(Math), nameof(Math.Sinh), Type.EmptyTypes, argument);
                    break;
                case nameof(Math.Tanh):
                    var hyperbolicCosine = Expression.Call(typeof(Math), nameof(Math.Cosh), Type.EmptyTypes, argument);
                    local = Expression.Divide(Expression.Constant(1.0), Expression.Multiply(hyperbolicCosine, hyperbolicCosine));
                    break;
                default:
                    return Deferred(call);
            }

            return Expression.Multiply(local, dArgument);
        }

        private Expression BuildBinary(BinaryExpression binary)
        {
            var left = binary.Left;
            var right = binary.Right;

            return binary.NodeType switch
            {
                ExpressionType.Add => Expression.Add(Build(left), Build(right)),
                ExpressionType.Subtract => Expression.Subtract(Build(left), Build(right)),
                ExpressionType.Multiply => Expression.Add(
                    Expression.Multiply(Build(left), right),
                    Expression.Multiply(left, Build(right))),
                ExpressionType.Divide => BuildQuotient(left, right),
                ExpressionType.Power => BuildPower(binary),
                _ => Deferred(binary)
            };
        }

        private Expression BuildQuotient(Expression numerator, Expression denominator)
        {
            // d(F/G) is constructed structurally as
            // (G·dF − F·dG) / G². Any zero G is left for SP2/O(1)/A1/A4.
            var top = Expression.Subtract(
                Expression.Multiply(denominator, Build(numerator)),
                Expression.Multiply(numerator, Build(denominator)));
            var bottom = Expression.Multiply(denominator, denominator);
            return Expression.Divide(top, bottom);
        }

        private Expression BuildPower(BinaryExpression power)
        {
            if (!TryGetIntegralExponent(power.Right, out var exponent))
            {
                return Deferred(power);
            }

            // d(F^n) = n·F^(n−1)·dF. This is a finite symbolic rewrite;
            // it does not differentiate a limit or evaluate a numerical slope.
            var coefficient = Expression.Constant(T.CreateChecked(exponent), typeof(T));
            var previousPower = Expression.Constant(T.CreateChecked(exponent - 1), typeof(T));
            var reducedPower = Expression.MakeBinary(
                ExpressionType.Power,
                power.Left,
                previousPower,
                false,
                power.Method);

            return Expression.Multiply(
                Expression.Multiply(coefficient, reducedPower),
                Build(power.Left));
        }

        private bool TryGetIntegralExponent(Expression expression, out long exponent)
        {
            exponent = 0;
            if (expression is not ConstantExpression { Value: T value })
            {
                return false;
            }

            try
            {
                exponent = long.CreateChecked(value);
                return true;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        private Expression Zero() => Expression.Constant(T.Zero, typeof(T));
        private Expression One() => Expression.Constant(T.One, typeof(T));
        private Expression Deferred(Expression expression) => new DeferredDerivativeExpression(expression, _time);
    }
}
