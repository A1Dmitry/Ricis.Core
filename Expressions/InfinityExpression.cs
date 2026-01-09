using System.Linq.Expressions;
using System.Text;
using Ricis.Core.Phases;

namespace Ricis.Core.Expressions;

public abstract class InfinityExpression : RicisExpression
{
    protected InfinityExpression(List<(ParameterExpression, double)> roots = null)
    {
        Roots = roots ?? new List<(ParameterExpression, double)>();
    }

    public List<(ParameterExpression Param, double Value)> Roots { get; }
    public override ExpressionType NodeType => ExpressionType.Extension;
    public override Type Type => typeof(double);

    public new ParameterExpression Variable => Roots.Count > 0 ? Roots[0].Param : null;
    public double SingularityValue => Roots.Count > 0 ? Roots[0].Value : double.NaN;

    public static InfinityExpression CreateLazy(Expression numerator, List<(ParameterExpression, double)> roots)
    {
        return new LazyInfinityExpression(numerator, roots);
    }

    // FIX: Фабрика 2: Удобная перегрузка (для одиночного корня)
    // Это исправит ошибку CS1501
    public static InfinityExpression CreateLazy(Expression numerator, ParameterExpression param, double value)
    {
        return new LazyInfinityExpression(numerator, [(param, value)]);
    }

    protected static string FormatInfinity(string index, List<(ParameterExpression Param, double Value)> roots)
    {
        var sb = new StringBuilder();
        sb.Append($"∞_{{{index.Replace("\"", "")}}}");

        if (roots.Count == 1)
        {
            sb.Append($" when {roots[0].Param?.Name ?? "?"}={roots[0].Value:F4}");
        }
        else if (roots.Count > 1)
        {
            sb.Append(" at {");
            sb.Append(string.Join(", ", roots.Select(r => $"{r.Param?.Name ?? "?"}={r.Value:F4}")));
            sb.Append("}");
        }

        return sb.ToString();
    }

    
}