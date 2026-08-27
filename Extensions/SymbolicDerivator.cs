using System.Linq.Expressions;

using Ricis.Core.Resources;

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
                RicisLegacyTextResources.Get("report.legacy.232484f420ef") +
                RicisLegacyTextResources.Get("report.legacy.412e0c64f5fe"));
        }

        var function = Expression.Lambda<Func<double, double>>(expr, param);
        return function.DxDt().Body;
    }
}
