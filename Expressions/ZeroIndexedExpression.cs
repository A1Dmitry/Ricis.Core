using System.Linq.Expressions;
using System.Text;

namespace Ricis.Core.Expressions
{
    /// <summary>
    /// Represents a typed zero 0_F — an indexed zero used instead of taking a limit.
    /// Index is stored as an AST Expression and optional roots for localization.
    /// </summary>
    public sealed class ZeroIndexedExpression : RicisExpression
    {
        private readonly Expression _indexExpression;
        public Expression IndexExpression => _indexExpression;
        public System.Collections.Generic.List<(ParameterExpression Param, double Value)> Roots { get; }

        public override ExpressionType NodeType => ExpressionType.Extension;
        public override System.Type Type => typeof(double);

        public ZeroIndexedExpression(Expression indexExpression, System.Collections.Generic.List<(ParameterExpression, double)> roots = null)
        {
            _indexExpression = indexExpression ?? Expression.Constant(0.0);
            Roots = roots ?? new System.Collections.Generic.List<(ParameterExpression, double)>();
        }

        public override string ToString()
        {
            var idx = _indexExpression?.ToString() ?? "?";
            var sb = new StringBuilder();
            sb.Append($"0_{{{idx.Replace('"', '\\"')}}}");
            if (Roots.Count == 1)
            {
                sb.Append($" when {Roots[0].Param?.Name ?? "?"}={Roots[0].Value:F4}");
            }
            else if (Roots.Count > 1)
            {
                sb.Append(" at {");
                sb.Append(string.Join(", ", Roots.Select(r => $"{r.Param?.Name ?? "?"}={r.Value:F4}")));
                sb.Append("}");
            }
            return sb.ToString();
        }
    }
}
