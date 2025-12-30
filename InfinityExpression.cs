using System.Linq.Expressions;
using Ricis.Core;

public sealed class InfinityExpression : Expression
{
    public InfinityExpression(Expression numerator, ParameterExpression variable, double value)
    {
        Numerator = numerator;
        Variable = variable;
        SingularityValue = value;
    }

    public Expression Numerator { get; } // Индекс бесконечности (0 или C)
    public ParameterExpression Variable { get; }
    public double SingularityValue { get; }

    public override ExpressionType NodeType => ExpressionType.Extension;
    public override Type Type => typeof(double); // В классике это число

    // --- FIX START: Делаем узел редуцируемым ---
    public override bool CanReduce => true;

    public override Expression Reduce()
    {
        // Логика проекции в классику (Classic Projection)

        // Проверяем, является ли индекс нулем (0.0 или 0)
        if (IsZero(Numerator))
        {
            // Аксиома: OO_0 -> 1
            return RicisType.InfinityOne;
        }

        // Аксиома: OO_C -> double.PositiveInfinity
        return Expression.Constant(double.PositiveInfinity);
    }

    private bool IsZero(Expression expr)
    {
        if (expr is ConstantExpression c)
        {
            if (c.Value is int i) return i == 0;
            if (c.Value is double d) return Math.Abs(d) < double.Epsilon;
        }
        return false;
    }
    private bool IsOne(Expression expr)
    {
        if (expr is ConstantExpression c)
        {
            if (c.Value is int i) return i == 1;
            if (c.Value is double d) return Math.Abs(d) == 1.0;
        }
        return false;
    }
    // --- FIX END ---

    public override string ToString()
    {
        return $"∞_{{{Numerator}}} при {Variable.Name} = {SingularityValue:R}";
    }
}