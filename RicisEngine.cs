using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

/// <summary>
/// Collects independently simplified indexed-infinity terms for consumers that
/// explicitly operate on resolved RICIS poles.
/// </summary>
public class RicisEngine
{
    private readonly List<InfinityExpression> _terms = [];

    /// <summary>
    /// Gets an immutable snapshot of indexed-infinity terms accepted by this collector.
    /// </summary>
    public IReadOnlyList<InfinityExpression> Terms => _terms.ToArray();

    /// <summary>
    /// Simplifies <paramref name="expr"/> and adds its body only when it is an
    /// indexed infinity. Finite expressions are rejected explicitly because
    /// this collector has no finite-term storage or combination contract.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when the simplified body is finite, an indexed zero, or another
    /// RICIS expression that is not a true infinity.
    /// </exception>
    public RicisEngine Add(Expression<Func<double, double>> expr)
    {
        ArgumentNullException.ThrowIfNull(expr);
        var simplified = RicisPhasePipeline.Simplify(expr);
        if (simplified is not Expression<Func<double, double>> { Body: InfinityExpression infinity } ||
            infinity is ZeroInfinityExpression)
        {
            throw new ArgumentException(
                "RicisEngine.Add принимает только лямбду, производное RICIS которой является индексированной бесконечностью ∞_F.",
                nameof(expr));
        }

        _terms.Add(infinity);
        return this;
    }
}
