using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

/// <summary>
/// A formal derivative D_variable(operand) retained when no symbolic
/// rearrangement is defined for the operand. It is intentionally not a limit,
/// a numerical approximation, or an implicit zero.
/// </summary>
public sealed class DeferredDerivativeExpression : Expression
{
    public Expression Operand { get; }
    public ParameterExpression DifferentiationVariable { get; }

    public DeferredDerivativeExpression(Expression operand, ParameterExpression variable)
    {
        Operand = operand ?? throw new ArgumentNullException(nameof(operand));
        DifferentiationVariable = variable ?? throw new ArgumentNullException(nameof(variable));
    }

    public override ExpressionType NodeType => ExpressionType.Extension;
    public override Type Type => Operand.Type;
    public override bool CanReduce => false;

    public override string ToString() => $"D_{{{DifferentiationVariable.Name ?? "?"}}}({Operand})";
}
