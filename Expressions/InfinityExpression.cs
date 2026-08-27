using System.Linq.Expressions;
using System.Text;

namespace Ricis.Core.Expressions;

/// <summary>
/// Base class for RICIS indexed singularity expressions. A singularity keeps
/// the scalar type of its symbolic index, so replacing a subexpression never
/// changes the return type of its enclosing lambda.
/// </summary>
public abstract class InfinityExpression : RicisExpression
{
    private readonly List<(ParameterExpression Param, double Value)> _roots;

    /// <summary>
    /// Initializes a singularity with a defensive copy of its certified keys.
    /// </summary>
    protected InfinityExpression(List<(ParameterExpression, double)> roots = null)
    {
        _roots = roots?.ToList() ?? [];
    }

    /// <summary>
    /// Gets a defensive copy of the certified singularity keys. Mutating the
    /// returned list cannot alter the already-built RICIS expression.
    /// </summary>
    public List<(ParameterExpression Param, double Value)> Roots => _roots.ToList();

    /// <inheritdoc />
    public override ExpressionType NodeType => ExpressionType.Extension;

    /// <summary>
    /// Gets the scalar type of the singularity index. This is the type exposed
    /// to an enclosing expression tree and therefore matches the original
    /// numerator or deferred function rather than being forced to <see cref="double"/>.
    /// </summary>
    public override Type Type => Numerator.Type;

    /// <summary>
    /// Gets the first key parameter when a certified key exists.
    /// </summary>
    public new ParameterExpression Variable => _roots.Count > 0 ? _roots[0].Param : null;

    /// <summary>
    /// Gets the first certified singularity value, or <see cref="double.NaN"/>
    /// when the singularity is not bound to one concrete key.
    /// </summary>
    public double SingularityValue => _roots.Count > 0 ? _roots[0].Value : double.NaN;

    /// <summary>
    /// Creates a lazy indexed infinity while preserving the type of
    /// <paramref name="numerator"/> and defensively copying its keys.
    /// </summary>
    public static InfinityExpression CreateLazy(Expression numerator, List<(ParameterExpression, double)> roots)
    {
        ArgumentNullException.ThrowIfNull(numerator);
        return new LazyInfinityExpression(numerator, roots);
    }

    /// <summary>
    /// Creates a lazy indexed infinity with one certified key.
    /// </summary>
    public static InfinityExpression CreateLazy(Expression numerator, ParameterExpression param, double value)
    {
        ArgumentNullException.ThrowIfNull(numerator);
        ArgumentNullException.ThrowIfNull(param);
        return new LazyInfinityExpression(numerator, [(param, value)]);
    }

    /// <summary>
    /// Formats an indexed infinity and its certified keys.
    /// </summary>
    protected static string FormatInfinity(string index, IReadOnlyList<(ParameterExpression Param, double Value)> roots) =>
        FormatIndexedSymbol("∞", index, roots);

    /// <summary>
    /// Formats an indexed zero and its certified keys.
    /// </summary>
    protected static string FormatZero(string index, IReadOnlyList<(ParameterExpression Param, double Value)> roots) =>
        FormatIndexedSymbol("0", index, roots);

    private static string FormatIndexedSymbol(
        string symbol,
        string index,
        IReadOnlyList<(ParameterExpression Param, double Value)> roots)
    {
        var sb = new StringBuilder();
        sb.Append(symbol);
        sb.Append("_{");
        sb.Append(index.Replace("\"", ""));
        sb.Append('}');

        if (roots.Count == 1)
        {
            sb.Append($" when {roots[0].Param?.Name ?? "?"}={roots[0].Value:G17}");
        }
        else if (roots.Count > 1)
        {
            sb.Append(" at {");
            sb.Append(string.Join(", ", roots.Select(r => $"{r.Param?.Name ?? "?"}={r.Value:G17}")));
            sb.Append('}');
        }

        return sb.ToString();
    }
}
