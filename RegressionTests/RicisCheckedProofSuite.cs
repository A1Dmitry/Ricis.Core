using System.Linq.Expressions;
using System.Text;
using Ricis.Core.Extensions;
using Ricis.Core.Proofs;

public static class RicisCheckedProofSuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("CHECKED01: ProveChecked принимает реальные conditions/constraints и проверяет expected", ProveCheckedAcceptsRealLambdas),
        ("CHECKED02: ProveChecked отклоняет структурно неверный expected", ProveCheckedRejectsWrongExpected),
        ("CHECKED03: ProveChecked корректно rebinding-ит параметр expected", ProveCheckedRebindsExpectedParameter),
        ("CHECKED04: ProveChecked не исполняет реальные lambda conditions", ProveCheckedDoesNotExecuteConditions),
        ("CHECKED05: ProveDocumentChecked публикует verification expression", ProveDocumentPublishesVerification),
    ];

    private static void ProveCheckedAcceptsRealLambdas()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var conditions = new[] { Expression.Lambda<Func<double, bool>>(Expression.GreaterThanOrEqual(x, Expression.Constant(0.0)), x) };
        var constraints = new[] { Expression.Lambda<Func<double, bool>>(Expression.NotEqual(x, Expression.Constant(0.0)), x) };
        Expression<Func<double, double>> claim = value => value / value;
        Expression<Func<double, double>> expected = value => 1.0;
        var proof = new StringBuilder();

        var result = conditions.Prove(constraints, claim, expected, proof);

        Assert(result.IsVerified, "Ожидаемое выражение 1 должно совпасть со структурным RICIS-результатом x/x.");
        Assert(result.Conditions.Count == 1 && result.Constraints.Count == 1, "Реальные условия должны сохраниться в результате.");
        Assert(result.Verification.Body is BinaryExpression { NodeType: ExpressionType.Equal }, "Verification должна быть реальным lambda-выражением равенства.");
    }

    private static void ProveCheckedRejectsWrongExpected()
    {
        Expression<Func<double, double>> claim = value => (value / 0.0) / (2.0 / 0.0);
        Expression<Func<double, double>> wrong = value => value / 3.0;
        var result = Array.Empty<Expression<Func<double, bool>>>().Prove(
            Array.Empty<Expression<Func<double, bool>>>(), claim, wrong, new StringBuilder());
        Assert(!result.IsVerified, "Неверный expected не должен считаться доказанным.");
    }

    private static void ProveCheckedRebindsExpectedParameter()
    {
        var claimParameter = Expression.Parameter(typeof(double), "claimX");
        var expectedParameter = Expression.Parameter(typeof(double), "expectedX");
        var claim = Expression.Lambda<Func<double, double>>(Expression.Divide(claimParameter, Expression.Constant(2.0)), claimParameter);
        var expected = Expression.Lambda<Func<double, double>>(Expression.Divide(expectedParameter, Expression.Constant(2.0)), expectedParameter);
        var result = Array.Empty<Expression<Func<double, bool>>>().Prove(
            Array.Empty<Expression<Func<double, bool>>>(), claim, expected, new StringBuilder());
        Assert(result.IsVerified, "Alpha-разные параметры expected должны быть приведены к параметру claim.");
    }

    private static void ProveCheckedDoesNotExecuteConditions()
    {
        ConditionCalls = 0;
        Expression<Func<double, bool>> condition = value => SideEffectCondition(value);
        Expression<Func<double, double>> identity = value => value / value;
        _ = new[] { condition }.Prove(
            Array.Empty<Expression<Func<double, bool>>>(), identity, identity, new StringBuilder());
        Assert(ConditionCalls == 0, "Proof API не должен компилировать или исполнять condition lambda.");
    }

    private static void ProveDocumentPublishesVerification()
    {
        Expression<Func<double, double>> claim = value => value / value;
        var document = new StringBuilder();
        var profile = new RicisProofDocumentProfile(
            "Checked proof",
            RicisProofScope.FiniteDerivation,
            "Real lambda proof",
            "Verification is structural.");
        var result = Array.Empty<Expression<Func<double, bool>>>().ProveDocument(
            Array.Empty<Expression<Func<double, bool>>>(), claim, claim, profile, document);
        Assert(result.IsVerified, "Identity expected должен быть проверен.");
        Assert(document.ToString().Contains("Verification", StringComparison.Ordinal), "Документ должен содержать verification expression.");
    }

    private static int ConditionCalls { get; set; }

    private static bool SideEffectCondition(double value)
    {
        ConditionCalls++;
        return value >= 0.0;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
