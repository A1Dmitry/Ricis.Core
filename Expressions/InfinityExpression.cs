using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

public sealed class InfinityExpression(Expression numerator, ParameterExpression variable, double value)
    : Expression
{
    public Expression Numerator { get; } = numerator; // Индекс бесконечности (0 или C)
    public new ParameterExpression Variable { get; set; } = variable;
    public double SingularityValue { get; } = value;

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
            if (c.Value is int i)
            {
                return i == 0;
            }

            if (c.Value is double d)
            {
                return Math.Abs(d) < double.Epsilon;
            }
        }
        return false;
    }
    private bool IsOne(Expression expr)
    {
        if (expr is ConstantExpression c)
        {
            if (c.Value is int i)
            {
                return i == 1;
            }

            if (c.Value is double d)
            {
                return Math.Abs(d) == 1.0;
            }
        }
        else
        {
            return expr.AreSelfIdentical(RicisType.InfinityOne);
        }
        return false;
    }
    // --- FIX END ---

    public override string ToString()
    {
        return $"∞_{{{Numerator}}} when {Variable.Name} = {SingularityValue:R}";
    }
}