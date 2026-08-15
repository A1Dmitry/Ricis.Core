using System.Linq.Expressions;
using System.Numerics;
using System.Text;
using Ricis.Core.Phases;

namespace Ricis.Core.Extensions;

/// <summary>
/// Builds auditable academic proof protocols for deferred RICIS expressions.
/// The proof is a symbolic derivation: supplied conditions and constraints are
/// recorded as hypotheses but are never compiled, evaluated, or treated as an
/// oracle of semantic truth.
/// </summary>
public static class RicisAcademicProofExtensions
{
    /// <summary>
    /// Derives a provable unary scalar expression through the normative RICIS
    /// pipeline and appends an academic protocol containing only effective
    /// RICIS transformations to <paramref name="proof"/>. The returned lambda is an independent derived
    /// expression; all conditions and constraints remain unevaluated expression
    /// trees in the written hypotheses.
    /// </summary>
    /// <typeparam name="T">
    /// The intrinsic or generic-math scalar type of the delayed RICIS function.
    /// </typeparam>
    /// <param name="conditions">
    /// Formal assumptions represented by unary boolean lambdas over the same
    /// scalar parameter as <paramref name="claim"/>.
    /// </param>
    /// <param name="constraints">
    /// Formal domain restrictions represented by unary boolean lambdas. They
    /// are recorded separately from assumptions to preserve academic meaning.
    /// </param>
    /// <param name="claim">
    /// The deferred scalar expression whose RICIS normal form is to be derived.
    /// </param>
    /// <param name="proof">
    /// An output buffer that receives the proof protocol. Its existing contents
    /// are preserved and a complete new proof section is appended.
    /// </param>
    /// <returns>The independently normalized <see cref="Expression{TDelegate}"/> representing the derived RICIS expression.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required collection, claim, or proof buffer is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when a hypothesis or the claim is not a unary lambda over T.
    /// </exception>
    public static Expression<Func<T, T>> Prove<T>(
        this IEnumerable<Expression<Func<T, bool>>> conditions,
        IEnumerable<Expression<Func<T, bool>>> constraints,
        Expression<Func<T, T>> claim,
        StringBuilder proof)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(constraints);
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(proof);

        var conditionList = conditions.ToList();
        var constraintList = constraints.ToList();
        ValidateHypotheses(conditionList, nameof(conditions));
        ValidateHypotheses(constraintList, nameof(constraints));
        ValidateClaim(claim);

        NumericConstants.Register<T>();
        var trace = new List<RicisPhaseTraceStep>();
        var derived = RicisPhasePipeline.SimplifyWithTrace(claim, trace) as Expression<Func<T, T>>
            ?? throw new InvalidOperationException(
                $"RICIS-конвейер должен сохранить Expression<Func<{typeof(T).Name}, {typeof(T).Name}>> при доказательстве.");

        AppendAcademicProtocol(proof, conditionList, constraintList, claim, trace, derived);
        return derived;
    }

    private static void ValidateHypotheses<T>(
        IEnumerable<Expression<Func<T, bool>>> hypotheses,
        string parameterName)
        where T : INumber<T>
    {
        foreach (var hypothesis in hypotheses)
        {
            if (hypothesis is null)
            {
                throw new ArgumentException("Список гипотез не может содержать null.", parameterName);
            }

            if (hypothesis.Parameters.Count != 1 || hypothesis.Parameters[0].Type != typeof(T) ||
                hypothesis.ReturnType != typeof(bool))
            {
                throw new ArgumentException(
                    $"Каждая гипотеза {parameterName} должна быть лямбдой Func<{typeof(T).Name}, Boolean> с одним параметром.",
                    parameterName);
            }
        }
    }

    private static void ValidateClaim<T>(Expression<Func<T, T>> claim)
        where T : INumber<T>
    {
        if (claim.Parameters.Count != 1 || claim.Parameters[0].Type != typeof(T) || claim.ReturnType != typeof(T))
        {
            throw new ArgumentException(
                $"Тезис должен быть лямбдой Func<{typeof(T).Name}, {typeof(T).Name}> с одним параметром.",
                nameof(claim));
        }
    }

    private static void AppendAcademicProtocol<T>(
        StringBuilder proof,
        IReadOnlyList<Expression<Func<T, bool>>> conditions,
        IReadOnlyList<Expression<Func<T, bool>>> constraints,
        Expression<Func<T, T>> claim,
        IReadOnlyList<RicisPhaseTraceStep> trace,
        Expression<Func<T, T>> derived)
        where T : INumber<T>
    {
        if (proof.Length > 0 && proof[^1] != '\n')
        {
            proof.AppendLine();
        }

        proof.AppendLine("# Формальный вывод RICIS III");
        proof.AppendLine();
        proof.AppendLine("## Предпосылки");
        AppendHypotheses(proof, "Условие", conditions);
        proof.AppendLine();
        proof.AppendLine("## Ограничения области");
        AppendHypotheses(proof, "Ограничение", constraints);
        proof.AppendLine();
        proof.AppendLine("## Тезис");
        proof.Append("Доказуемое отложенное выражение: `").Append(claim).AppendLine("`.");
        proof.AppendLine();
        proof.AppendLine("## Нормативный вывод");
        proof.AppendLine("Ни одна предпосылка не исполнялась численно. Ниже записаны только нормативные фазы, которые действительно изменили expression tree; неизменяющие и неприменённые фазы в текст доказательства не включаются.");
        proof.AppendLine();

        var effectiveSteps = trace.Where(step => step.Changed).ToList();
        if (effectiveSteps.Count == 0)
        {
            proof.AppendLine("Ни одна нормативная фаза не изменила тезис: производное выражение структурно совпадает с исходным.");
            proof.AppendLine();
        }

        for (var index = 0; index < effectiveSteps.Count; index++)
        {
            var step = effectiveSteps[index];
            proof.Append("### Шаг ").Append(index + 1).Append(": ").AppendLine(step.PhaseName);
            proof.Append("**Нормативное основание:** ").AppendLine(step.RuleFamily + ".");
            proof.Append("До: `").Append(step.Before).AppendLine("`.");
            proof.Append("После: `").Append(step.After).AppendLine("`.");
            proof.AppendLine();
        }

        proof.AppendLine("## Заключение");
        proof.Append("Следовательно, в рамках перечисленных формальных условий и ограничений производное RICIS-выражение имеет вид: `")
            .Append(derived)
            .AppendLine("`.");
        proof.AppendLine("Протокол фиксирует символическое выведение и не утверждает истинность внешних предпосылок вне переданных expression tree.");
    }

    private static void AppendHypotheses<T>(
        StringBuilder proof,
        string label,
        IReadOnlyList<Expression<Func<T, bool>>> hypotheses)
        where T : INumber<T>
    {
        if (hypotheses.Count == 0)
        {
            proof.AppendLine("Формальные высказывания не заданы.");
            return;
        }

        for (var index = 0; index < hypotheses.Count; index++)
        {
            proof.Append(index + 1)
                .Append(". ")
                .Append(label)
                .Append(": `")
                .Append(hypotheses[index])
                .AppendLine("`.");
        }
    }
}
