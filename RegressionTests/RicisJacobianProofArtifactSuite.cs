using System.Linq.Expressions;
using Ricis.Core.Proofs;

internal static class RicisJacobianProofArtifactSuite
{
    internal static IReadOnlyList<(string Name, Action Body)> Tests { get; } =
    [
        ("JPR01: Jacobian proof принимает независимые lambda-посылки и lambda verification", RetainsRealLambdaProofInputs),
        ("JPR02: Jacobian proof публикует полный typed node-to-root trace", PublishesFullCanonicalTrace),
        ("JPR03: Jacobian proof экспортирует standalone LaTeX и compilable structured Lean", ExportsStandaloneLatexAndLean),
        ("JPR04: Jacobian proof сохраняет A6 payload без классического inverse", PreservesJacobianA6Payload),
    ];

    private static void RetainsRealLambdaProofInputs()
    {
        var scenario = RicisJacobianProofScenario.Create();
        var proof = scenario.ScalarProof.Proof;

        Require(proof.IsVerified,
            "Claim detJ/detJ должен структурно совпасть с expected lambda detJ => 1.");
        Require(proof.Conditions.Count == 2 && proof.Constraints.Count == 1,
            "Сценарий обязан хранить несколько независимых conditions и отдельный domain constraint.");
        Require(proof.Conditions.All(condition => condition is Expression<Func<double, bool>>) &&
                proof.Constraints.All(constraint => constraint is Expression<Func<double, bool>>) &&
                proof.Verification is Expression<Func<double, bool>>,
            "Все посылки, ограничение и proof verification должны остаться реальными lambda expression trees.");
        Require(proof.Verification.Body is BinaryExpression { NodeType: ExpressionType.Equal },
            "Verification должна быть structural equality lambda, а не текстовым статусом.");
    }

    private static void PublishesFullCanonicalTrace()
    {
        var scenario = RicisJacobianProofScenario.Create();
        var trace = scenario.ScalarProof.Trace;
        var latex = scenario.LatexSource;

        Require(trace.Count > 3 && trace.Select(entry => entry.Sequence).SequenceEqual(Enumerable.Range(1, trace.Count).Select(value => (long)value)),
            "Typed journal должен содержать упорядоченный полный trace одного proof run.");
        Require(trace.Count(entry => entry.EventCode == "RICIS_PROOF_START") == 1 &&
                trace.Count(entry => entry.EventCode == "RICIS_PROOF_COMPLETE") == 1 &&
                trace.Count(entry => entry.EventCode == "RICIS_PROOF_VERIFICATION") == 1 &&
                trace.Any(entry => entry.StageType.Contains("RicisProofOrchestrationStage", StringComparison.Ordinal)),
            "Trace обязан содержать ровно один solver run с началом, концом, verification и typed orchestration stage.");
        Require(latex.Contains("Node-to-root маршрут", StringComparison.Ordinal) &&
                latex.Contains("Проверочное выражение", StringComparison.Ordinal) &&
                latex.Contains("Типизированный лог visitor и handler этапов", StringComparison.Ordinal),
            "LaTeX export обязан содержать node-to-root protocol, verification и полный typed audit trace того же run.");
    }

    private static void ExportsStandaloneLatexAndLean()
    {
        var scenario = RicisJacobianProofScenario.Create();
        var latex = scenario.LatexSource;
        var lean = scenario.CombinedLeanSource;
        var audit = scenario.LeanAuditSource;

        Require(latex.StartsWith("\\documentclass", StringComparison.Ordinal) &&
                latex.Contains("\\begin{document}", StringComparison.Ordinal) &&
                latex.Contains("\\begin{verbatim}", StringComparison.Ordinal) &&
                latex.Contains("\\end{verbatim}", StringComparison.Ordinal) &&
                latex.TrimEnd().EndsWith("\\end{document}", StringComparison.Ordinal),
            "LaTeX export должен быть самостоятельным документом с закрытыми document/verbatim окружениями.");
        Require(lean.Contains("a6_indexed_zero_infinity_bridge", StringComparison.Ordinal) &&
                lean.Contains("namespace RicisJacobian", StringComparison.Ordinal) &&
                !lean.Contains("RICIS proof-document export: Lean scaffold", StringComparison.Ordinal) &&
                !lean.Contains("sorry", StringComparison.OrdinalIgnoreCase) &&
                !lean.Contains("sorryAx", StringComparison.OrdinalIgnoreCase),
            "Structured Lean source должен содержать только typed A6 theorem без generic scaffold и sorry markers.");
        Require(audit.Contains("NOT KERNEL VERIFIED", StringComparison.Ordinal) &&
                audit.Contains("RICIS typed proof-log report", StringComparison.Ordinal) &&
                !audit.Contains("theorem ", StringComparison.Ordinal),
            "Typed-log audit должен оставаться отдельным comment-only report и не смешиваться с theorem source.");
    }

    private static void PreservesJacobianA6Payload()
    {
        var scenario = RicisJacobianProofScenario.Create();

        Require(scenario.Jacobian.IsStructuralSingular,
            "Rank-one Jacobian обязан хранить determinant как structural zero.");
        Require(scenario.Jacobian.Roots.Count == 2 && scenario.A6Payload.Count == 1,
            "Jacobian scenario должен сохранить сертифицированные x/y keys и один inverse payload entry.");
        Require(scenario.A6Payload[0].Body is BinaryExpression { NodeType: ExpressionType.Multiply },
            "A6 bridge обязан вернуть structural product, а не вычисленный inverse/NaN.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
