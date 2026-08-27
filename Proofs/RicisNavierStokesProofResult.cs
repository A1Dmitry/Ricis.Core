using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;

namespace Ricis.Core.Proofs;

/// <summary>
/// Stores the exact deferred fields produced by a RICIS Navier-Stokes proof scenario.
/// </summary>
public sealed class RicisNavierStokesProofResult
{
    /// <summary>
    /// Initializes a certified proof result.
    /// </summary>
    /// <param name="divergence">The formal incompressibility scalar.</param>
    /// <param name="timeDerivative">The exact field <c>∂t u</c>.</param>
    /// <param name="convection">The exact field <c>(u·∇)u</c>.</param>
    /// <param name="pressureGradient">The exact field <c>∇p</c>.</param>
    /// <param name="laplacian">The exact field <c>Δu</c>.</param>
    /// <param name="residual">The normalized Navier-Stokes residual.</param>
    /// <exception cref="ArgumentNullException">Thrown when an intermediate proof expression is null.</exception>
    public RicisNavierStokesProofResult(
        Expression<Func<double, double, double, double, double>> divergence,
        RicisVectorField3 timeDerivative,
        RicisVectorField3 convection,
        RicisVectorField3 pressureGradient,
        RicisVectorField3 laplacian,
        RicisVectorField3 residual)
    {
        ArgumentNullException.ThrowIfNull(divergence);
        ArgumentNullException.ThrowIfNull(timeDerivative);
        ArgumentNullException.ThrowIfNull(convection);
        ArgumentNullException.ThrowIfNull(pressureGradient);
        ArgumentNullException.ThrowIfNull(laplacian);
        ArgumentNullException.ThrowIfNull(residual);

        Divergence = divergence;
        TimeDerivative = timeDerivative;
        Convection = convection;
        PressureGradient = pressureGradient;
        Laplacian = laplacian;
        Residual = residual;
    }

    /// <summary>Gets the formal divergence <c>∇·u</c>.</summary>
    public Expression<Func<double, double, double, double, double>> Divergence { get; }

    /// <summary>Gets the formal time derivative <c>∂t u</c>.</summary>
    public RicisVectorField3 TimeDerivative { get; }

    /// <summary>Gets the formal convective field <c>(u·∇)u</c>.</summary>
    public RicisVectorField3 Convection { get; }

    /// <summary>Gets the formal pressure gradient <c>∇p</c>.</summary>
    public RicisVectorField3 PressureGradient { get; }

    /// <summary>Gets the formal viscous base field <c>Δu</c>.</summary>
    public RicisVectorField3 Laplacian { get; }

    /// <summary>Gets the normalized Navier-Stokes residual.</summary>
    public RicisVectorField3 Residual { get; }

    /// <summary>Gets whether incompressibility and every residual component are exact structural zeros.</summary>
    public bool IsCertified => Divergence.Body.IsZero() && Residual.IsStructuralZero();
}
