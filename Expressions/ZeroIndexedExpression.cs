using System.Linq.Expressions;
using System.Text;

namespace Ricis.Core.Expressions
{
    /// <summary>
    /// Представляет индексированный ноль 0_F — типизированный ноль, используемый вместо вычисления предела.
    /// Индекс хранится как AST-выражение (Expression), а также может содержать список корней для локализации сингулярности.
    /// </summary>
    public sealed class ZeroIndexedExpression : RicisExpression
    {
        /// <summary>
        /// Индексированное выражение F в записи 0_F. Хранится как AST (Expression) и используется для следования протоколу SP4.
        /// </summary>
        public Expression IndexExpression { get; }

        /// <summary>
        /// Список пар (параметр, значение), указывающий точку/точки локализации нуля (например, x=0).
        /// Может быть пустым, если локализация отсутствует или не определена.
        /// </summary>
        public System.Collections.Generic.List<(ParameterExpression Param, double Value)> Roots { get; }

        /// <summary>
        /// Тип узла выражения (ExpressionType.Extension) — расширяемый узел AST.
        /// </summary>
        public override ExpressionType NodeType => ExpressionType.Extension;

        /// <summary>
        /// Системный тип значения узла — double (вещественное число).
        /// </summary>
        public override System.Type Type => typeof(double);

        /// <summary>
        /// Создаёт новый экземпляр ZeroIndexedExpression.
        /// <param name="indexExpression">AST-выражение, служащее индексом F (может быть null — в таком случае используется 0.)</param>
        /// <param name="roots">Опциональный список корней для локализации сингулярности (параметр, значение).</param>
        /// </summary>
        public ZeroIndexedExpression(Expression indexExpression, System.Collections.Generic.List<(ParameterExpression, double)> roots = null)
        {
            IndexExpression = indexExpression ?? Expression.Constant(0.0);
            Roots = roots ?? new System.Collections.Generic.List<(ParameterExpression, double)>();
        }

        /// <summary>
        /// Строковое представление индексированного нуля в формате 0_{index} [при необходимости с информацией о корнях].
        /// Используется для отладки и логирования.
        /// </summary>
        /// <returns>Читаемая строка, отображающая индекс и корни (если есть).</returns>
        public override string ToString()
        {
            var idx = IndexExpression?.ToString() ?? "?";
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
