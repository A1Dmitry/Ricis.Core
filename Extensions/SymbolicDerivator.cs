using System.Linq.Expressions;

namespace Ricis.Core.Extensions;

/// <summary>
/// Compatibility entry point for formal symbolic differentiation.
/// It delegates to <see cref="RicisDerivativeExtensions.DxDt{T}"/> and never
/// uses limits, L'Hopital's rule, numerical differences, or approximation.
/// </summary>
public static class SymbolicDerivator
{
    /// <summary>
    /// Builds dF/dt for an existing double expression. New generic callers
    /// should prefer <c>Expression&lt;Func&lt;T,T&gt;&gt;.DxDt()</c>.
    /// </summary>
    public static Expression Derive(this Expression expr, ParameterExpression param)
    {
        ArgumentNullException.ThrowIfNull(expr);
        ArgumentNullException.ThrowIfNull(param);

        if (expr.Type != typeof(double) || param.Type != typeof(double))
        {
            throw new ArgumentException(
                "Совместимый Derive поддерживает только Expression и параметр типа double. " +
                "Для generic-math используйте DxDt() у Expression<Func<T,T>>.");
        }

        var function = Expression.Lambda<Func<double, double>>(expr, param);
        return function.DxDt().Body;
    }
}
