using System.Linq.Expressions;
using System.Numerics;

namespace Ricis.Core.Expressions;

/// <summary>
///     Представление типового нуля 0_F — ноль с индексом (Expression) и типом индекса (RicisType).
///     Теперь класс обобщённый: TValue задаёт CLR‑тип значения (как <double>, <int> и т.п.).
/// </summary>
public sealed class TypedZeroExpression<TValue>(Expression indexExpression, RicisType indexType) : Expression
where TValue : INumber<TValue>
{
    public Expression IndexExpression { get; } = indexExpression;
    public RicisType IndexType { get; } = indexType ?? RicisType.Scalar;

    public override ExpressionType NodeType => ExpressionType.Extension;
    public override Type Type => typeof(TValue);

    public override string ToString()
    {
        return $"0_{{{IndexExpression}}}:{typeof(TValue).Name}";
    }
}