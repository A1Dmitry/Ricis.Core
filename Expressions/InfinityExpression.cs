using System.Linq.Expressions;
using System.Text;
using Ricis.Core.Extensions;

namespace Ricis.Core.Expressions;

public sealed class InfinityExpression : RicisExpression
{
    // DRY: Используем Numerator как единственный источник Истины (Индекс)
    public Expression Numerator { get; }
    
    // Основное хранилище точек сингулярности
    public List<(ParameterExpression Param, double Value)> Roots { get; }

    public override ExpressionType NodeType => ExpressionType.Extension;
    public override Type Type => typeof(double);
    public override bool CanReduce => true;

    // --- КОНСТРУКТОР 1: Множественные сингулярности (Вызывается из RicisTransformVisitor) ---
    public InfinityExpression(Expression numerator, List<(ParameterExpression, double)> roots)
    {
        Numerator = numerator;
        Roots = roots ?? new();
    }

    // --- КОНСТРУКТОР 2: Одиночная сингулярность (Legacy / Совместимость) ---
    // Делегирует основному конструктору, чтобы не дублировать логику инициализации
    public InfinityExpression(Expression numerator, ParameterExpression variable, double value)
        : this(numerator, new List<(ParameterExpression, double)> { (variable, value) })
    {
    }

    // --- Computed Properties (Совместимость со старым кодом) ---
    // Берем данные из списка Roots, не храним их отдельно
    public new ParameterExpression Variable => Roots.Count > 0 ? Roots[0].Param : null;
    public double SingularityValue => Roots.Count > 0 ? Roots[0].Value : double.NaN;

    /// <summary>
    /// Проекция RICIS-бесконечности в стандартную арифметику IEEE 754.
    /// Реализует SP1 и SP4.
    /// </summary>
    public override Expression Reduce()
    {
        // 1. CASE: Identity Recovery (0/0 -> 1)
        // Если Индекс ЯВНО равен 0 (константа), то это схлопнувшаяся неопределенность.
        if (Numerator.IsZero())
        {
            return Expression.Constant(1.0);
        }

        // 2. CASE: Pole / Weighted Infinity (C/0 -> C * Inf)
        // Мы возвращаем выражение (Index * Infinity).
        // Это сохраняет знак и масштаб.

        var infinity = Expression.Constant(double.PositiveInfinity);

        // Приводим Индекс к double, чтобы умножение было валидным
        var indexAsDouble = Numerator.Type == typeof(double)
            ? Numerator
            : Expression.Convert(Numerator, typeof(double));

        return Expression.Multiply(indexAsDouble, infinity);
    }

    private static bool IsZero(Expression expr)
    {
        if (expr is ConstantExpression c)
        {
            if (c.Value is int i) return i == 0;
            if (c.Value is double d) return Math.Abs(d) < double.Epsilon;
        }
        return false;
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append($"∞_{{{Numerator}}}"); // Вывод Индекса

        if (Roots.Count == 1)
        {
            if (double.IsNaN(Roots[0].Value))
                sb.Append(" (Implicit)");
            else
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