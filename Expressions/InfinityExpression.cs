using System.Linq.Expressions;
using System.Text;
using Ricis.Core.Phases;

namespace Ricis.Core.Expressions;

/// <summary>
/// Represents the RICIS public type <c>InfinityExpression</c>.
/// </summary>
public abstract class InfinityExpression : RicisExpression
{
    /// <summary>
    /// Initializes a new instance of <c>InfinityExpression</c>.
    /// </summary>
    protected InfinityExpression(List<(ParameterExpression, double)> roots = null)
    {
        Roots = roots ?? new List<(ParameterExpression, double)>();
    }

    /// <summary>
    /// Gets the <c>Roots</c> value of <c>InfinityExpression</c>.
    /// </summary>
    public List<(ParameterExpression Param, double Value)> Roots { get; }
    /// <inheritdoc />
    public override ExpressionType NodeType => ExpressionType.Extension;
    /// <inheritdoc />
    public override Type Type => typeof(double);

    /// <summary>
    /// Gets the <c>Variable</c> value of <c>InfinityExpression</c>.
    /// </summary>
    public new ParameterExpression Variable => Roots.Count > 0 ? Roots[0].Param : null;
    /// <summary>
    /// Gets the <c>SingularityValue</c> value of <c>InfinityExpression</c>.
    /// </summary>
    public double SingularityValue => Roots.Count > 0 ? Roots[0].Value : double.NaN;

    /// <summary>
    /// Executes <c>CreateLazy</c> for the RICIS expression model.
    /// </summary>
    public static InfinityExpression CreateLazy(Expression numerator, List<(ParameterExpression, double)> roots)
    {
        return new LazyInfinityExpression(numerator, roots);
    }

    // FIX: Фабрика 2: Удобная перегрузка (для одиночного корня)
    // Это исправит ошибку CS1501
    /// <summary>
    /// Executes <c>CreateLazy</c> for the RICIS expression model.
    /// </summary>
    public static InfinityExpression CreateLazy(Expression numerator, ParameterExpression param, double value)
    {
        return new LazyInfinityExpression(numerator, [(param, value)]);
    }

    /// <summary>
    /// Executes <c>FormatInfinity</c> for the RICIS expression model.
    /// </summary>
    protected static string FormatInfinity(string index, List<(ParameterExpression Param, double Value)> roots) =>
        FormatIndexedSymbol("∞", index, roots);

    /// <summary>
    /// Executes <c>FormatZero</c> for the RICIS expression model.
    /// </summary>
    protected static string FormatZero(string index, List<(ParameterExpression Param, double Value)> roots) =>
        FormatIndexedSymbol("0", index, roots);

    private static string FormatIndexedSymbol(string symbol, string index, List<(ParameterExpression Param, double Value)> roots)
    {
        var sb = new StringBuilder();
        sb.Append($"{symbol}_{{{index.Replace("\"", "")}}}");

        if (roots.Count == 1)
        {
            sb.Append($" when {roots[0].Param?.Name ?? "?"}={roots[0].Value:G17}");
        }
        else if (roots.Count > 1)
        {
            sb.Append(" at {");
            sb.Append(string.Join(", ", roots.Select(r => $"{r.Param?.Name ?? "?"}={r.Value:G17}")));
            sb.Append("}");
        }

        return sb.ToString();
    }

    
}