using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

/// <summary>
/// A formal derivative D_variable(operand) retained when no symbolic
/// rearrangement is defined for the operand. It is intentionally not a limit,
/// a numerical approximation, or an implicit zero.
/// </summary>
public sealed class DeferredDerivativeExpression : Expression
{
    /// <summary>
    /// Gets the <c>Operand</c> value of <c>DeferredDerivativeExpression</c>.
    /// </summary>
    public Expression Operand { get; }
    /// <summary>
    /// Gets the <c>DifferentiationVariable</c> value of <c>DeferredDerivativeExpression</c>.
    /// </summary>
    public ParameterExpression DifferentiationVariable { get; }

    /// <summary>
    /// Initializes a new instance of <c>DeferredDerivativeExpression</c>.
    /// </summary>
    public DeferredDerivativeExpression(Expression operand, ParameterExpression variable)
    {
        Operand = operand ?? throw new ArgumentNullException(nameof(operand));
        DifferentiationVariable = variable ?? throw new ArgumentNullException(nameof(variable));
    }

    /// <inheritdoc />
    public override ExpressionType NodeType => ExpressionType.Extension;
    /// <inheritdoc />
    public override Type Type => Operand.Type;
    /// <inheritdoc />
    public override bool CanReduce => false;

    /// <inheritdoc />
    public override string ToString() => $"D_{{{DifferentiationVariable.Name ?? "?"}}}({Operand})";
}
