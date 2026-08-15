using System.Linq.Expressions;
using System.Text;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;

/// <summary>
/// Regression scenarios inspired by the symmetry of non-trivial zeta zeros.
/// These tests deliberately prove only finite consequences of their explicit
/// hypotheses; they do not assert or establish the Riemann hypothesis itself.
/// </summary>
internal static class RiemannHypothesisProofSuite
{
    /// <summary>Returns the Riemann-related proof regression cases.</summary>
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("RIEMANN01: симметричная пара на критической прямой выводит σ=1/2", CriticalLinePairProof),
        ("RIEMANN02: Riemann-связанный протокол отвергает ложное следствие", FalseCriticalLineClaimIsRejected),
    ];

    private static void CriticalLinePairProof()
    {
        // sigma and mirrorSigma represent real parts of a finite formal pair.
        // The first equality is symmetry with respect to Re(s)=1/2. The second
        // equality is an explicit finite hypothesis that the pair has equal
        // real parts. Only their linear consequence is proved here.
        Expression<Func<double, double, bool>>[] equations =
        [
            (sigma, mirrorSigma) => sigma + mirrorSigma == 1.0,
            (sigma, mirrorSigma) => sigma - mirrorSigma == 0.0,
        ];
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
        var protocol = new StringBuilder();

        var derived = equations.Prove(constraints, claim, protocol);
        var derivedPredicate = derived.Compile();
        var text = protocol.ToString();

        Require(derivedPredicate(0.5, 0.5) && !derivedPredicate(0.4, 0.6),
            $"Из заданной системы должно следовать sigma=1/2; получено {derived}.");
        Require(derived.Body is BinaryExpression { NodeType: ExpressionType.Equal, Right: var derivedHalf } &&
                derivedHalf.AreEqual(exactHalf.Body),
            "Производное доказательное дерево должно сравнивать sigma с эталонным expression tree () => 1.0 / 2.0.");
        Require(text.Contains("((2 * sigma) == 1)", StringComparison.Ordinal) &&
                text.Contains("(sigma == (1 / 2))", StringComparison.Ordinal) &&
                text.Contains("(mirrorSigma == (1 / 2))", StringComparison.Ordinal) &&
                text.Contains("система выводит sigma=(1 / 2) и mirrorSigma=(1 / 2)", StringComparison.Ordinal),
            "Riemann-связанный протокол должен сохранить несократимую дробь 1/2, реальные имена параметров и все четыре шага линейного вывода.");
        Require(!text.Contains("доказывает гипотезу Римана", StringComparison.OrdinalIgnoreCase),
            "Конечный proof-сценарий не должен ошибочно объявляться доказательством гипотезы Римана.");
    }

    private static void FalseCriticalLineClaimIsRejected()
    {
        Expression<Func<double, double, bool>>[] equations =
        [
            (sigma, mirrorSigma) => sigma + mirrorSigma == 1.0,
            (sigma, mirrorSigma) => sigma - mirrorSigma == 0.0,
        ];
        Expression<Func<double, double, bool>>[] constraints =
        [
            (sigma, mirrorSigma) => sigma > 0.0 && sigma < 1.0,
        ];
        Expression<Func<double, double, bool>> falseClaim =
            (sigma, mirrorSigma) => sigma == 0.4;

        RequireArgumentException(
            () => _ = equations.Prove(constraints, falseClaim, new StringBuilder()),
            "Протокол не должен принимать ложное следствие sigma=0.4 для симметричной пары.");
    }

    private static void RequireArgumentException(Action action, string message)
    {
        try
        {
            action();
            throw new InvalidOperationException(message);
        }
        catch (ArgumentException)
        {
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
