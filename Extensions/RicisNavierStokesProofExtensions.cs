using System.Linq.Expressions;
using System.Globalization;
using System.Text;
using Ricis.Core.Expressions;
using Ricis.Core.Proofs;
using Ricis.Core.Resources;

namespace Ricis.Core.Extensions;

/// <summary>
/// Academic proof operations for exact RICIS Navier-Stokes field identities.
/// </summary>
public static class RicisNavierStokesProofExtensions
{
    /// <summary>
    /// Constructs and certifies the incompressible Navier-Stokes residual
    /// <c>∂t u+(u·∇)u+∇p−νΔu</c> for a supplied deferred velocity and pressure.
    /// The method is purely symbolic: no field component is compiled, sampled,
    /// discretized, or evaluated numerically.
    /// </summary>
    /// <param name="velocity">The deferred velocity field <c>u(x,y,z,t)</c>.</param>
    /// <param name="pressure">The deferred pressure scalar <c>p(x,y,z,t)</c>.</param>
    /// <param name="viscosity">The finite viscosity coefficient <c>ν</c>.</param>
    /// <param name="proof">The output buffer for the academic proof document.</param>
    /// <returns>All intermediate deferred fields and the certified residual.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when viscosity is negative or non-finite.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the supplied field does not satisfy exact incompressibility or the normalized residual is non-zero.</exception>
    public static RicisNavierStokesProofResult ProveNavierStokesIdentity(
        this RicisVectorField3 velocity,
        Expression<Func<double, double, double, double, double>> pressure,
        double viscosity,
        StringBuilder proof)
    {
        ArgumentNullException.ThrowIfNull(velocity);
        ArgumentNullException.ThrowIfNull(pressure);
        ArgumentNullException.ThrowIfNull(proof);
        if (!double.IsFinite(viscosity) || viscosity < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(viscosity), viscosity, RicisLegacyTextResources.Get("report.legacy.901faa2aff0e"));
        }

        EnsureFiniteConstants(velocity.U, nameof(velocity));
        EnsureFiniteConstants(velocity.V, nameof(velocity));
        EnsureFiniteConstants(velocity.W, nameof(velocity));
        EnsureFiniteConstants(pressure, nameof(pressure));

        var divergence = velocity.Divergence();
        var timeDerivative = velocity.TimeDerivative();
        var convection = velocity.ConvectiveDerivative(velocity);
        var pressureGradient = pressure.Gradient();
        var laplacian = velocity.Laplacian();
        var residual = timeDerivative
            .Add(convection)
            .Add(pressureGradient)
            .Subtract(laplacian.Scale(viscosity));
        var result = new RicisNavierStokesProofResult(
            divergence,
            timeDerivative,
            convection,
            pressureGradient,
            laplacian,
            residual);

        if (!divergence.Body.IsZero())
        {
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentUICulture, RicisLegacyTextResources.Get("report.legacy.e535b04270b1"), divergence));
        }

        if (!residual.IsStructuralZero())
        {
            throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentUICulture, RicisLegacyTextResources.Get("report.legacy.4fdef7e7a9f9"), FormatField(residual)));
        }

        AppendProof(proof, velocity, pressure, viscosity, result);
        return result;
    }

    private static void AppendProof(
        StringBuilder proof,
        RicisVectorField3 velocity,
        Expression<Func<double, double, double, double, double>> pressure,
        double viscosity,
        RicisNavierStokesProofResult result)
    {
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.7176f237529b"));
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.bdd154f2ccd2"));
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.5e88098bad33"));
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.8008afe654cc"));
        proof.AppendLine(string.Format(CultureInfo.CurrentUICulture, RicisLegacyTextResources.Get("report.legacy.00c017819672"), FormatField(velocity)));
        proof.AppendLine(string.Format(CultureInfo.CurrentUICulture, RicisLegacyTextResources.Get("report.legacy.52267a6b9ae8"), pressure));
        proof.AppendLine(string.Format(CultureInfo.CurrentUICulture, RicisLegacyTextResources.Get("report.legacy.4d16cdcb2eda"), viscosity));
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.cc581791e38c"));
        AppendStep(proof, "NS-01", RicisLegacyTextResources.Get("report.legacy.1690ce7e0253"), RicisLegacyTextResources.Get("report.legacy.6be9f532c82f"), FormatField(velocity));
        AppendStep(proof, "NS-02", RicisLegacyTextResources.Get("report.legacy.9c2b960ed8d1"), RicisLegacyTextResources.Get("report.legacy.9c2b960ed8d1"), result.Divergence.ToString());
        AppendStep(proof, "NS-03", RicisLegacyTextResources.Get("report.legacy.0d2d51e692b7"), RicisLegacyTextResources.Get("report.legacy.1fe97bf2118d"), FormatField(result.TimeDerivative));
        AppendStep(proof, "NS-04", RicisLegacyTextResources.Get("report.legacy.9024ba2b8477"), RicisLegacyTextResources.Get("report.legacy.9024ba2b8477"), FormatField(result.Convection));
        AppendStep(proof, "NS-05", RicisLegacyTextResources.Get("report.legacy.5f01a4f4bfe5"), RicisLegacyTextResources.Get("report.legacy.5f01a4f4bfe5"), FormatField(result.PressureGradient));
        AppendStep(proof, "NS-06", RicisLegacyTextResources.Get("report.legacy.2fc26c363933"), RicisLegacyTextResources.Get("report.legacy.2fc26c363933"), FormatField(result.Laplacian));
        AppendStep(proof, "NS-07", RicisLegacyTextResources.Get("report.legacy.e2b1ada5d5f8"), RicisLegacyTextResources.Get("report.legacy.e2b1ada5d5f8"), FormatField(result.Residual));
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.bbf4ba2e6ad8"));
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.8e9340cbe2b9"));
    }

    private static void AppendStep(StringBuilder proof, string ruleId, string title, string basis, string after)
    {
        proof.AppendLine($"### {ruleId}: {title}");
        proof.AppendLine($"**Нормативное основание:** {basis}");
        proof.AppendLine($"**После:** `{after}`.");
    }

    private static string FormatField(RicisVectorField3 field) =>
        $"({field.U.Body}, {field.V.Body}, {field.W.Body})";

    private static void EnsureFiniteConstants(LambdaExpression expression, string parameterName)
    {
        var visitor = new FiniteConstantValidationVisitor(parameterName);
        visitor.Visit(expression);
    }

    private sealed class FiniteConstantValidationVisitor : ExpressionVisitor
    {
        private readonly string _parameterName;

        public FiniteConstantValidationVisitor(string parameterName) => _parameterName = parameterName;

        protected override Expression VisitExtension(Expression node) => node;

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is double value && !double.IsFinite(value))
            {
                throw new ArgumentException(
                    string.Concat(
                        string.Format(CultureInfo.CurrentUICulture, RicisLegacyTextResources.Get("report.legacy.d70c2bf4cc20"), _parameterName, value),
                        RicisLegacyTextResources.Get("report.legacy.147e337db02e")),
                    _parameterName);
            }

            return node;
        }
    }
}
