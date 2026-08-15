using System.Linq.Expressions;
using System.Text;
using Ricis.Core.Expressions;
using Ricis.Core.Proofs;

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
            throw new ArgumentOutOfRangeException(nameof(viscosity), viscosity, "Вязкость должна быть конечной и неотрицательной.");
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
            throw new InvalidOperationException($"NS-02 не выполнен: ∇·u не сократился до нуля, получено {divergence}.");
        }

        if (!residual.IsStructuralZero())
        {
            throw new InvalidOperationException(
                $"NS-07 не выполнен: остаток Навье—Стокса не сократился до нулевого поля, получено {FormatField(residual)}.");
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
        proof.AppendLine("# Нормативный вывод RICIS: тождество Навье—Стокса");
        proof.AppendLine("## Доказательный статус");
        proof.AppendLine("**Конечное символическое выведение.** Все поля остаются expression tree; численное моделирование, пределы и Лопиталь не применяются.");
        proof.AppendLine("## Исходные отложенные поля");
        proof.AppendLine($"- Скорость: `{FormatField(velocity)}`.");
        proof.AppendLine($"- Давление: `{pressure}`.");
        proof.AppendLine($"- Вязкость: `ν={viscosity:G17}`.");
        proof.AppendLine("## Нормативная цепочка RICIS");
        AppendStep(proof, "NS-01", "типовая структура поля", "Три компоненты скорости сохранены как независимые deferred expression tree.", FormatField(velocity));
        AppendStep(proof, "NS-02", "несжимаемость", "∂x U+∂y V+∂z W должно сократиться до нуля.", result.Divergence.ToString());
        AppendStep(proof, "NS-03", "стационарность", "Компонентная ∂t-операция над полем скорости.", FormatField(result.TimeDerivative));
        AppendStep(proof, "NS-04", "конвективный перенос", "Структурное поле (u·∇)u.", FormatField(result.Convection));
        AppendStep(proof, "NS-05", "градиент давления", "Структурное поле ∇p.", FormatField(result.PressureGradient));
        AppendStep(proof, "NS-06", "вязкий член", "Компонентный лапласиан Δu.", FormatField(result.Laplacian));
        AppendStep(proof, "NS-07", "нулевой остаток", "∂t u+(u·∇)u+∇p−νΔu нормализуется RICIS до нулевого поля.", FormatField(result.Residual));
        proof.AppendLine("## Заключение");
        proof.AppendLine("Поля прошли точную проверку несжимаемости и имеют нулевой нормализованный остаток Навье—Стокса.");
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
                    $"{_parameterName} содержит non-finite double constant {value}; " +
                    "proof допускает только конечные real-valued expression trees.",
                    _parameterName);
            }

            return node;
        }
    }
}
