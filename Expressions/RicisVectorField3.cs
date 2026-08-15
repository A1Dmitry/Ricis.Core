using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

/// <summary>
/// Represents a three-component deferred scalar field
/// <c>(U,V,W)(x,y,z,t)</c> for formal RICIS vector calculus.
/// </summary>
public sealed class RicisVectorField3
{
    /// <summary>
    /// Initializes a three-component deferred field.
    /// </summary>
    /// <param name="u">The first field component.</param>
    /// <param name="v">The second field component.</param>
    /// <param name="w">The third field component.</param>
    /// <exception cref="ArgumentNullException">Thrown when a component is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a component does not have exactly four double parameters.</exception>
    public RicisVectorField3(
        Expression<Func<double, double, double, double, double>> u,
        Expression<Func<double, double, double, double, double>> v,
        Expression<Func<double, double, double, double, double>> w)
    {
        U = Validate(u, nameof(u));
        V = Validate(v, nameof(v));
        W = Validate(w, nameof(w));
    }

    /// <summary>Gets the first component <c>U(x,y,z,t)</c>.</summary>
    public Expression<Func<double, double, double, double, double>> U { get; }

    /// <summary>Gets the second component <c>V(x,y,z,t)</c>.</summary>
    public Expression<Func<double, double, double, double, double>> V { get; }

    /// <summary>Gets the third component <c>W(x,y,z,t)</c>.</summary>
    public Expression<Func<double, double, double, double, double>> W { get; }

    private static Expression<Func<double, double, double, double, double>> Validate(
        Expression<Func<double, double, double, double, double>> component,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(component, parameterName);
        if (component.Parameters.Count != 4 || component.Parameters.Any(parameter => parameter.Type != typeof(double)))
        {
            throw new ArgumentException(
                "Компонента векторного поля должна иметь сигнатуру (double x, double y, double z, double t) => double.",
                parameterName);
        }

        return component;
    }
}
