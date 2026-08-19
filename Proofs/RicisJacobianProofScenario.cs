using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Logging;

namespace Ricis.Core.Proofs;

/// <summary>
/// Produces one auditable symbolic proof scenario for the singular rank-one
/// Jacobian J = ((1, 1), (1, 1)), whose determinant is structurally zero.
/// </summary>
public sealed class RicisJacobianProofScenario
{
    private RicisJacobianProofScenario(
        RicisCheckedProofArtifacts<double> scalarProof,
        RicisJacobianSingularityExpression<double> jacobian,
        IReadOnlyList<LambdaExpression> a6Payload,
        RicisLeanDoc structuredLeanDocument,
        string combinedLeanSource)
    {
        ScalarProof = scalarProof ?? throw new ArgumentNullException(nameof(scalarProof));
        Jacobian = jacobian ?? throw new ArgumentNullException(nameof(jacobian));
        A6Payload = a6Payload?.ToArray() ?? throw new ArgumentNullException(nameof(a6Payload));
        StructuredLeanDocument = structuredLeanDocument ?? throw new ArgumentNullException(nameof(structuredLeanDocument));
        CombinedLeanSource = string.IsNullOrWhiteSpace(combinedLeanSource)
            ? throw new ArgumentException("Combined Lean source не может быть пустым.", nameof(combinedLeanSource))
            : combinedLeanSource;
    }

    /// <summary>Gets the checked scalar determinant proof, typed trace and document exports.</summary>
    public RicisCheckedProofArtifacts<double> ScalarProof { get; }

    /// <summary>Gets the singular structural state of the rank-one Jacobian.</summary>
    public RicisJacobianSingularityExpression<double> Jacobian { get; }

    /// <summary>Gets the entrywise A6 bridge results for the deferred inverse payload.</summary>
    public IReadOnlyList<LambdaExpression> A6Payload { get; }

    /// <summary>Gets the formal structured A6 Lean theorem document.</summary>
    public RicisLeanDoc StructuredLeanDocument { get; }

    /// <summary>
    /// Gets the structured, compiler-checkable Lean source only. The audit report
    /// remains available separately through <see cref="LeanAuditSource"/>.
    /// </summary>
    public string CombinedLeanSource { get; }

    /// <summary>Gets the standalone LaTeX proof document rendered from the one canonical run.</summary>
    public string LatexSource => ScalarProof.GetDocument(RicisProofDocumentFormat.Latex);

    /// <summary>Gets the non-kernel typed-log audit report rendered from the one canonical run.</summary>
    public string LeanAuditSource => RicisProofLogReportRenderer.Render(ScalarProof.Trace, RicisProofLogFormat.Lean);

    /// <summary>
    /// Creates the canonical rank-one Jacobian scenario without compiling or
    /// executing its formal lambda conditions, constraints, claim, or expected result.
    /// </summary>
    public static RicisJacobianProofScenario Create()
    {
        var determinant = Expression.Parameter(typeof(double), "detJ");
        var zero = Expression.Constant(0.0);
        var one = Expression.Constant(1.0);
        Expression<Func<double, bool>>[] conditions =
        [
            Expression.Lambda<Func<double, bool>>(Expression.Equal(determinant, zero), determinant),
            Expression.Lambda<Func<double, bool>>(
                Expression.Equal(Expression.Multiply(determinant, determinant), zero), determinant),
        ];
        Expression<Func<double, bool>>[] constraints =
        [
            Expression.Lambda<Func<double, bool>>(Expression.GreaterThanOrEqual(determinant, zero), determinant),
        ];
        var claim = Expression.Lambda<Func<double, double>>(
            Expression.Divide(determinant, determinant), determinant);
        var expected = Expression.Lambda<Func<double, double>>(one, determinant);
        var profile = new RicisProofDocumentProfile(
            title: "JAC-001: сингулярный rank-one Jacobian",
            scope: RicisProofScope.ConditionalTheorem,
            @abstract: "Каноническая RICIS-проверка structural determinant identity и A6 payload bridge для J=((1,1),(1,1)).",
            theorem: "При переданных формальных посылках det(J)=0 scalar claim detJ/detJ структурно выводится в 1; inverse payload остаётся отложенным A6-объектом.",
            definitions:
            [
                "J = ((1,1),(1,1)); det(J)=1·1−1·1=0.",
                "detJ — scalar-координата determinant, используемая только как expression tree.",
                "0_det(J) и ∞_Inv(J) — индексированные структурные объекты, не классический inverse.",
            ],
            axioms:
            [
                "L1/SP2: одинаковые отложенные выражения F/F структурно сокращаются до 1.",
                "A6: 0_F·∞_G возвращает structural payload F·G.",
            ],
            normativeSteps:
            [
                new RicisProofAxiomStep("JAC-01", "Сертификация determinant", "Rank-one Jacobian передаёт structural zero determinant без вычисления inverse."),
                new RicisProofAxiomStep("JAC-02", "Проверка scalar claim", "L1/SP2 строит verification lambda для detJ/detJ и 1."),
                new RicisProofAxiomStep("JAC-03", "A6 payload bridge", "Каждая inverse payload entry остаётся отложенной и связывается с indexed determinant."),
            ],
            limitations:
            [
                "Формальные lambda-посылки не исполняются и не считаются установленными фактами.",
                "Generic Lean audit trace не является theorem; kernel-checked часть ограничена structured A6 template.",
                "Сценарий не вычисляет классический inverse сингулярной матрицы.",
            ]);
        var log = new RicisProofLog<RicisProofOrchestrationStage>();
        var scalarProof = conditions.ProveDocumentsCheckedWithLog(
            constraints,
            claim,
            expected,
            profile,
            [RicisProofDocumentFormat.Latex, RicisProofDocumentFormat.Json],
            log);

        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        var structuralDeterminant = Expression.Lambda<Func<double, double, double>>(Expression.Constant(0.0), x, y);
        var inversePayload = Expression.Lambda<Func<double, double, double>>(
            Expression.Divide(
                Expression.Add(x, Expression.Constant(1.0)),
                Expression.Subtract(y, Expression.Constant(2.0))),
            x,
            y);
        var jacobian = new RicisJacobianSingularityExpression<double>(
            structuralDeterminant,
            [inversePayload],
            [(x, 1.0), (y, 2.0)]);
        var a6Payload = jacobian.ApplyA6GeometricBridge();
        var structuredLean = RicisLeanTemplate.Render(
            new RicisLeanStructuredData(namespaceName: "RicisJacobian"),
            new RicisLeanRequestedRows([RicisLeanProofRow.A6IndexedZeroInfinityBridge]));
        var combinedLean = structuredLean.Source;

        return new RicisJacobianProofScenario(scalarProof, jacobian, a6Payload, structuredLean, combinedLean);
    }

}
