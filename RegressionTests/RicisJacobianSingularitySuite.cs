using System.Linq.Expressions;
using Ricis.Core.Expressions;

internal static class RicisJacobianSingularitySuite
{
    internal static IReadOnlyList<(string Name, Action Body)> Tests { get; } =
    [
        ("JSG01: сингулярный determinant сохраняется как индексированный zero", PreservesDeterminantIndex),
        ("JSG02: A6 возвращает determinant·inverse payload без NaN", AppliesA6PayloadBridge),
        ("JSG03: несколько inverse payload entries обрабатываются покомпонентно", BridgesAllPayloadEntries),
        ("JSG04: пустой inverse payload отклоняется", RejectsEmptyPayload),
    ];

    private static void PreservesDeterminantIndex()
    {
        var state = CreateState();
        Require(state.IsStructuralSingular, "det(J)=0 должен распознаваться как структурный singularity candidate.");
        Require(state.ToString().Contains("0_"), "Состояние должно сохранять индексированный zero determinant.");
    }

    private static void AppliesA6PayloadBridge()
    {
        var result = CreateState().ApplyA6GeometricBridge();
        Require(result.Count == 1, "Один inverse payload должен дать один bridge result.");
        Require(result[0].Body is BinaryExpression { NodeType: ExpressionType.Multiply } product &&
                product.Left is ConstantExpression { Value: double left } && left == 0.0 &&
                product.Right is ConstantExpression { Value: double right } && right == 7.0,
            $"A6 должен сохранить точный payload 0_det·Inv, получено: {result[0]}.");
    }

    private static void BridgesAllPayloadEntries()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        var determinant = Expression.Lambda<Func<double, double, double>>(Expression.Constant(0.0), x, y);
        var payload1 = Expression.Lambda<Func<double, double, double>>(Expression.Constant(2.0), x, y);
        var payload2 = Expression.Lambda<Func<double, double, double>>(Expression.Add(x, y), x, y);
        var state = new RicisJacobianSingularityExpression<double>(determinant, [payload1, payload2]);
        var result = state.ApplyA6GeometricBridge();
        Require(result.Count == 2, "Каждый inverse payload entry должен пройти A6 отдельно.");
        Require(result.All(entry => entry.Body is BinaryExpression { NodeType: ExpressionType.Multiply }),
            "A6 payload entries должны оставаться структурными произведениями.");
    }

    private static void RejectsEmptyPayload()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        var determinant = Expression.Lambda<Func<double, double, double>>(Expression.Constant(0.0), x, y);
        Expect<ArgumentException>(() => _ = new RicisJacobianSingularityExpression<double>(determinant, []),
            "Пустой inverse payload не имеет смысла для матричного состояния.");
    }

    private static RicisJacobianSingularityExpression<double> CreateState()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var y = Expression.Parameter(typeof(double), "y");
        var determinant = Expression.Lambda<Func<double, double, double>>(Expression.Constant(0.0), x, y);
        var inversePayload = Expression.Lambda<Func<double, double, double>>(Expression.Constant(7.0), x, y);
        return new RicisJacobianSingularityExpression<double>(determinant, [inversePayload]);
    }

    private static void Expect<TException>(Action action, string message)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException(message);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
