using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

public static class RicisTypeConsistencySuite
{
    public static IEnumerable<(string Name, Action Body)> Tests =>
    [
        ("SP3: TypeConsistency сохраняет indexed-zero payload и тот же узел", TypeConsistencyPreservesZeroNode),
        ("SP3: TypeConsistency сохраняет indexed-infinity payload и ключ", TypeConsistencyPreservesInfinityNode),
        ("SP3: TypeConsistency отклоняет несертифицированный бесконечный ключ", TypeConsistencyRejectsNonFiniteKey),
    ];

    private static void TypeConsistencyPreservesZeroNode()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var zero = new ZeroInfinityExpression(x, []);
        var result = TypeConsistencyPhase.Apply(zero);
        Assert(ReferenceEquals(result, zero), "SP3 не должен редуцировать indexed zero или терять его payload.");
        Assert(ReferenceEquals(((ZeroInfinityExpression)result).Numerator, x), "SP3 должен сохранить исходный индекс F.");
    }

    private static void TypeConsistencyPreservesInfinityNode()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var infinity = InfinityExpression.CreateLazy(x, [(x, 2.0)]);
        var result = TypeConsistencyPhase.Apply(infinity);
        Assert(ReferenceEquals(result, infinity), "SP3 не должен редуцировать indexed infinity.");
        Assert(((InfinityExpression)result).Roots.Count == 1, "SP3 должен сохранить certified key.");
    }

    private static void TypeConsistencyRejectsNonFiniteKey()
    {
        var x = Expression.Parameter(typeof(double), "x");
        var zero = new ZeroInfinityExpression(x, [(x, double.NaN)]);
        try
        {
            _ = TypeConsistencyPhase.Apply(zero);
            throw new InvalidOperationException("SP3 должен отклонять NaN certified key.");
        }
        catch (ArgumentException)
        {
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
