using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Proofs;

internal static class RicisLeanSingularitySuite
{
    internal static IReadOnlyList<(string Name, Action Body)> Tests { get; } =
    [
        ("SQA01: сложная multi-key singularity сохраняет RICIS A6 payload", ComplexSingularityPreservesA6Payload),
        ("SQA02: классическая оценка singularity не определена, RICIS result структурен", ClassicalBoundaryIsUndefined),
        ("SQA03: сложная singularity генерирует structured A6 LeanDoc", SingularityGeneratesLeanDoc),
    ];

    private static void ComplexSingularityPreservesA6Payload()
    {
        var state = CreateComplexState();
        Require(state.IsStructuralSingular,
            "Сложный determinant должен быть распознан как структурный indexed-zero candidate.");
        Require(state.Roots.Count == 2 &&
                state.Roots.Any(root => root.Param.Name == "x" && root.Value == 1.0) &&
                state.Roots.Any(root => root.Param.Name == "y" && root.Value == 2.0),
            "Состояние обязано сохранить оба certified singularity key.");

        var bridged = state.ApplyA6GeometricBridge();
        Require(bridged.Count == 2,
            "Каждый inverse payload entry должен пройти общий A6 bridge.");
        Require(bridged.All(entry => entry.Body is BinaryExpression { NodeType: ExpressionType.Multiply }),
            "A6 должен вернуть структурные payload products, а не вычисленный NaN/Infinity.");
        Require(bridged[0].Body.ToString()!.Contains("(x + 1)", StringComparison.Ordinal) &&
                bridged[0].Body.ToString()!.Contains("(y + 2)", StringComparison.Ordinal),
            "Первый payload должен сохраниться в производном произведении.");
        Require(bridged[1].Body.ToString()!.Contains("(x * x)", StringComparison.Ordinal) &&
                bridged[1].Body.ToString()!.Contains("(y - 2)", StringComparison.Ordinal),
            "Второй payload должен сохранить nested singular expression.");
    }

    private static void ClassicalBoundaryIsUndefined()
    {
        var state = CreateComplexState();
        var determinant = (Expression<Func<double, double, double>>)state.Determinant;
        var payload = (Expression<Func<double, double, double>>)state.InversePayload[0];
        var determinantValue = determinant.Compile()(1.0, 2.0);
        var payloadValue = payload.Compile()(1.0, 2.0);

        Require(double.IsNaN(determinantValue) || determinantValue == 0.0,
            "Классический determinant boundary должен быть нулём или неопределённым.");
        Require(double.IsNaN(payloadValue) || double.IsInfinity(payloadValue),
            "Классический inverse payload boundary должен быть NaN/Infinity.");
        Require(state.ApplyA6GeometricBridge()[0].Body is BinaryExpression,
            "RICIS обязан вернуть structural product вместо классического NaN/Infinity.");
    }

    private static void SingularityGeneratesLeanDoc()
    {
        var rows = new RicisLeanRequestedRows([RicisLeanProofRow.A6IndexedZeroInfinityBridge]);
        var document = RicisLeanTemplate.Render(new RicisLeanStructuredData(), rows);
        var source = document.Source;

        Require(source.Contains("structure A6Payloads", StringComparison.Ordinal) &&
                source.Contains("theorem a6_indexed_zero_infinity_bridge", StringComparison.Ordinal) &&
                source.Contains("theorem a6_payload_product_commutative", StringComparison.Ordinal) &&
                source.Contains("zeroPayload key * A.infinityPayload key", StringComparison.Ordinal) &&
                !source.Contains("sorry", StringComparison.OrdinalIgnoreCase) &&
                !source.Contains("ToString", StringComparison.Ordinal),
            "Сложная singularity должна порождать typed A6 Lean source, а не текстовую сериализацию expression tree.");
    }

    private static RicisJacobianSingularityExpression<double> CreateComplexState()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        // The Jacobian state receives the already-certified structural zero;
        // the high-complexity singular payload remains in the inverse entries.
        var determinantBody = Expression.Constant(0.0);
        var determinant = Expression.Lambda<Func<double, double, double>>(determinantBody, x, y);

        var payload1 = Expression.Divide(
            Expression.Multiply(Expression.Add(x, Expression.Constant(1.0)), Expression.Add(y, Expression.Constant(2.0))),
            Expression.Subtract(y, Expression.Constant(2.0)));
        var payload2 = Expression.Divide(
            Expression.Add(Expression.Multiply(x, x), Expression.Constant(3.0)),
            Expression.Subtract(y, Expression.Constant(2.0)));
        var inversePayload = new LambdaExpression[]
        {
            Expression.Lambda<Func<double, double, double>>(payload1, x, y),
            Expression.Lambda<Func<double, double, double>>(payload2, x, y),
        };

        return new RicisJacobianSingularityExpression<double>(
            determinant,
            inversePayload,
            [(x, 1.0), (y, 2.0)]);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
