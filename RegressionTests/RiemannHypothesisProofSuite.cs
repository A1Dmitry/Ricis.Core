using System.Linq.Expressions;
using System.Text;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Proofs;

/// <summary>
/// Regression scenarios for the complete normative RICIS type-identity chain.
/// They verify that ID-01 through ID-06 are emitted as named steps before the
/// exact finite expression-tree derivation.
/// </summary>
internal static class RiemannHypothesisProofSuite
{
    /// <summary>Returns the type-identity proof regression cases.</summary>
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("RIEMANN01: ID-01–ID-06 выводят σ=1/2 как точную дробь", TypeIdentityChainProof),
        ("RIEMANN02: ID-цепочка отвергает ложное следствие", FalseCriticalLineClaimIsRejected),
        ("RIEMANN03: ID-цепочка сохраняет однопроходные ограничения", SinglePassConstraintsArePreserved),
        ("RIEMANN04: специализированный case отделяет аналитику от RICIS-алгебры", SpecializedCaseSeparatesResponsibilities),
    ];

    private static void TypeIdentityChainProof()
    {
        Expression<Func<double, double, bool>>[] constraints =
        [
            (sigma, mirrorSigma) => sigma > 0.0 && sigma < 1.0,
            (sigma, mirrorSigma) => mirrorSigma > 0.0 && mirrorSigma < 1.0,
        ];
        var exactHalf = Expression.Lambda<Func<double>>(
            Expression.Divide(Expression.Constant(1.0), Expression.Constant(2.0)));
        var sigma = Expression.Parameter(typeof(double), "sigma");
        var mirrorSigma = Expression.Parameter(typeof(double), "mirrorSigma");
        var claim = Expression.Lambda<Func<double, double, bool>>(
            Expression.Equal(sigma, exactHalf.Body),
            sigma,
            mirrorSigma);
        var document = new StringBuilder();

        var derived = constraints.ProveTypeIdentityCriticalLine(claim, document);
        var derivedPredicate = derived.Compile();
        var text = document.ToString();

        Require(derivedPredicate(0.5, 0.5) && !derivedPredicate(0.4, 0.6),
            $"ID-цепочка должна вывести sigma=1/2; получено {derived}.");
        Require(derived.Body is BinaryExpression { NodeType: ExpressionType.Equal, Right: var derivedHalf } &&
                derivedHalf.AreEqual(exactHalf.Body),
            "Производное дерево ID-цепочки должно сравнивать sigma с эталонным Divide(1,2).");
        Require(text.Contains("# Нормативный вывод RICIS: тождество типа отражённой пары", StringComparison.Ordinal) &&
                text.Contains("## Нормативная цепочка RICIS", StringComparison.Ordinal) &&
                text.Contains("ID-01", StringComparison.Ordinal) &&
                text.Contains("ID-02", StringComparison.Ordinal) &&
                text.Contains("ID-03", StringComparison.Ordinal) &&
                text.Contains("ID-04", StringComparison.Ordinal) &&
                text.Contains("ID-05", StringComparison.Ordinal) &&
                text.Contains("ID-06", StringComparison.Ordinal) &&
                text.Contains("((2 * sigma) == 1)", StringComparison.Ordinal) &&
                text.Contains("(sigma == (1 / 2))", StringComparison.Ordinal) &&
                text.Contains("(mirrorSigma == (1 / 2))", StringComparison.Ordinal),
            "Документ обязан содержать полный именованный путь ID-01–ID-06 и все промежуточные expression tree.");
    }

    private static void FalseCriticalLineClaimIsRejected()
    {
        Expression<Func<double, double, bool>>[] constraints =
        [
            (sigma, mirrorSigma) => sigma > 0.0 && sigma < 1.0,
        ];
        Expression<Func<double, double, bool>> falseClaim =
            (sigma, mirrorSigma) => sigma == 0.4;

        RegressionAssertions.Expect<ArgumentException>(
            () => _ = constraints.ProveTypeIdentityCriticalLine(falseClaim, new StringBuilder()),
            "ID-цепочка не должна принимать ложное следствие sigma=0.4.");
    }

    private static void SinglePassConstraintsArePreserved()
    {
        var enumerationCount = 0;
        var sigma = Expression.Parameter(typeof(double), "sigma");
        var mirrorSigma = Expression.Parameter(typeof(double), "mirrorSigma");
        var exactHalf = Expression.Divide(Expression.Constant(1.0), Expression.Constant(2.0));
        var claim = Expression.Lambda<Func<double, double, bool>>(
            Expression.Equal(sigma, exactHalf), sigma, mirrorSigma);

        _ = SinglePassConstraints(() => enumerationCount++)
            .ProveTypeIdentityCriticalLine(claim, new StringBuilder());

        Require(enumerationCount == 1,
            "ID-цепочка обязана материализовать однопроходные ограничения ровно один раз без их потери.");
    }

    private static void SpecializedCaseSeparatesResponsibilities()
    {
        Expression<Func<double, double, bool>>[] constraints =
        [
            (sigma, mirrorSigma) => sigma > 0.0 && sigma < 1.0,
            (sigma, mirrorSigma) => mirrorSigma > 0.0 && mirrorSigma < 1.0,
        ];
        Expression<Func<double, double, bool>> claim = (sigma, mirrorSigma) => sigma == 0.5;
        var specializedCase = new RiemannHypothesisProofCase(constraints, claim);
        RicisProofCase proofCase = specializedCase;

        var result = proofCase.Run();
        Require(result.Status == "ConditionalTheorem",
            "RH proof case не должен объявлять аналитически незамкнутый результат конечной деривацией.");
        Require(specializedCase.DerivedClaim is not null &&
                specializedCase.DerivedClaim.Compile()(0.5, 0.5) &&
                !specializedCase.DerivedClaim.Compile()(0.4, 0.6),
            "RH наследник обязан получить тот же ID-01–ID-06 результат из общего engine.");
        Require(proofCase.UnresolvedObligations.Count >= 6 &&
                proofCase.Monitor.Any(entry => entry.Stage == "ANALYTIC" && entry.Status == "OPEN") &&
                proofCase.Monitor.Any(entry => entry.Stage == "ID-01..ID-06" && entry.Status == "PASS"),
            "RH case обязан мониторить открытые аналитические obligations и завершённую алгебраическую фазу отдельно.");
        Require(result.Document.Contains("ID-06", StringComparison.Ordinal),
            "RH case обязан сохранить документ ID-01–ID-06 общего доказательного engine.");
    }

    private static IEnumerable<Expression<Func<double, double, bool>>> SinglePassConstraints(Action onEnumeration)
    {
        onEnumeration();
        yield return (sigma, mirrorSigma) => sigma > 0.0 && sigma < 1.0;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
