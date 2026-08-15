using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;

namespace Ricis.Core.Extensions;

/// <summary>
/// Formal vector-calculus operations over four-variable deferred RICIS fields.
/// All operations construct and transform expression trees only; no component
/// or field is compiled or numerically sampled.
/// </summary>
public static class RicisVectorCalculusExtensions
{
    /// <summary>
    /// Builds the exact symbolic partial derivative of a scalar field with respect
    /// to one coordinate of <c>(x,y,z,t)</c>.
    /// </summary>
    /// <param name="field">The deferred scalar field.</param>
    /// <param name="coordinate">The selected coordinate.</param>
    /// <returns>A normalized deferred scalar field for the partial derivative.</returns>
    public static Expression<Func<double, double, double, double, double>> PartialDerivative(
        this Expression<Func<double, double, double, double, double>> field,
        RicisFieldCoordinate coordinate)
    {
        ArgumentNullException.ThrowIfNull(field);
        if (!Enum.IsDefined(coordinate))
        {
            throw new ArgumentOutOfRangeException(nameof(coordinate), coordinate, "Неизвестная координата поля.");
        }

        var parameters = CreateParameters(field.Parameters);
        var body = Rebind(field, parameters);
        var derivative = new FourVariableDerivativeBuilder(parameters[(int)coordinate]).Build(body);
        return Normalize(derivative, parameters);
    }

    /// <summary>
    /// Builds the spatial gradient <c>(∂x F,∂y F,∂z F)</c> of a scalar field.
    /// </summary>
    /// <param name="field">The deferred scalar field.</param>
    /// <returns>The exact symbolic gradient field.</returns>
    public static RicisVectorField3 Gradient(
        this Expression<Func<double, double, double, double, double>> field) =>
        new(
            field.PartialDerivative(RicisFieldCoordinate.X),
            field.PartialDerivative(RicisFieldCoordinate.Y),
            field.PartialDerivative(RicisFieldCoordinate.Z));

    /// <summary>
    /// Builds the spatial divergence <c>∂x U+∂y V+∂z W</c> of a vector field.
    /// </summary>
    /// <param name="field">The deferred vector field.</param>
    /// <returns>The exact deferred divergence scalar field.</returns>
    public static Expression<Func<double, double, double, double, double>> Divergence(this RicisVectorField3 field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return Add(
            Add(field.U.PartialDerivative(RicisFieldCoordinate.X), field.V.PartialDerivative(RicisFieldCoordinate.Y)),
            field.W.PartialDerivative(RicisFieldCoordinate.Z));
    }

    /// <summary>
    /// Builds the componentwise spatial Laplacian of a vector field.
    /// </summary>
    /// <param name="field">The deferred vector field.</param>
    /// <returns>The exact vector field <c>Δfield</c>.</returns>
    public static RicisVectorField3 Laplacian(this RicisVectorField3 field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return new(Laplacian(field.U), Laplacian(field.V), Laplacian(field.W));
    }

    /// <summary>
    /// Builds the componentwise exact time derivative of a vector field.
    /// </summary>
    /// <param name="field">The deferred vector field.</param>
    /// <returns>The field <c>∂t field</c>.</returns>
    public static RicisVectorField3 TimeDerivative(this RicisVectorField3 field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return new(
            field.U.PartialDerivative(RicisFieldCoordinate.T),
            field.V.PartialDerivative(RicisFieldCoordinate.T),
            field.W.PartialDerivative(RicisFieldCoordinate.T));
    }

    /// <summary>
    /// Builds the convective field <c>(u·∇)v</c> without evaluating either field.
    /// </summary>
    /// <param name="transport">The deferred transport velocity <c>u</c>.</param>
    /// <param name="field">The deferred field <c>v</c>.</param>
    /// <returns>The exact symbolic convective derivative.</returns>
    public static RicisVectorField3 ConvectiveDerivative(this RicisVectorField3 transport, RicisVectorField3 field)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(field);
        return new(
            ConvectiveComponent(transport, field.U),
            ConvectiveComponent(transport, field.V),
            ConvectiveComponent(transport, field.W));
    }

    /// <summary>
    /// Adds two deferred vector fields componentwise.
    /// </summary>
    /// <param name="left">The first field.</param>
    /// <param name="right">The second field.</param>
    /// <returns>The exact field <c>left+right</c>.</returns>
    public static RicisVectorField3 Add(this RicisVectorField3 left, RicisVectorField3 right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return new(Add(left.U, right.U), Add(left.V, right.V), Add(left.W, right.W));
    }

    /// <summary>
    /// Subtracts the second deferred vector field from the first componentwise.
    /// </summary>
    /// <param name="left">The minuend field.</param>
    /// <param name="right">The subtrahend field.</param>
    /// <returns>The exact field <c>left-right</c>.</returns>
    public static RicisVectorField3 Subtract(this RicisVectorField3 left, RicisVectorField3 right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return new(Subtract(left.U, right.U), Subtract(left.V, right.V), Subtract(left.W, right.W));
    }

    /// <summary>
    /// Scales every component of a deferred vector field by a finite scalar.
    /// </summary>
    /// <param name="field">The source field.</param>
    /// <param name="scalar">The scalar multiplier.</param>
    /// <returns>The exact scaled field.</returns>
    public static RicisVectorField3 Scale(this RicisVectorField3 field, double scalar)
    {
        ArgumentNullException.ThrowIfNull(field);
        if (!double.IsFinite(scalar))
        {
            throw new ArgumentOutOfRangeException(nameof(scalar), scalar, "Масштаб векторного поля обязан быть конечным double.");
        }

        return new(Scale(field.U, scalar), Scale(field.V, scalar), Scale(field.W, scalar));
    }

    /// <summary>
    /// Determines whether every component of a deferred field is structurally a typed zero after RICIS normalization.
    /// </summary>
    /// <param name="field">The field to inspect.</param>
    /// <returns><see langword="true"/> when all three components are exact zeros.</returns>
    public static bool IsStructuralZero(this RicisVectorField3 field)
    {
        ArgumentNullException.ThrowIfNull(field);
        return field.U.Body.IsZero() && field.V.Body.IsZero() && field.W.Body.IsZero();
    }

    private static Expression<Func<double, double, double, double, double>> Laplacian(
        Expression<Func<double, double, double, double, double>> field) =>
        Add(
            Add(
                field.PartialDerivative(RicisFieldCoordinate.X).PartialDerivative(RicisFieldCoordinate.X),
                field.PartialDerivative(RicisFieldCoordinate.Y).PartialDerivative(RicisFieldCoordinate.Y)),
            field.PartialDerivative(RicisFieldCoordinate.Z).PartialDerivative(RicisFieldCoordinate.Z));

    private static Expression<Func<double, double, double, double, double>> ConvectiveComponent(
        RicisVectorField3 transport,
        Expression<Func<double, double, double, double, double>> component)
    {
        var dx = component.PartialDerivative(RicisFieldCoordinate.X);
        var dy = component.PartialDerivative(RicisFieldCoordinate.Y);
        var dz = component.PartialDerivative(RicisFieldCoordinate.Z);
        return Add(Add(Multiply(transport.U, dx), Multiply(transport.V, dy)), Multiply(transport.W, dz));
    }

    private static Expression<Func<double, double, double, double, double>> Add(
        Expression<Func<double, double, double, double, double>> left,
        Expression<Func<double, double, double, double, double>> right) =>
        Combine(left, right, Expression.Add);

    private static Expression<Func<double, double, double, double, double>> Subtract(
        Expression<Func<double, double, double, double, double>> left,
        Expression<Func<double, double, double, double, double>> right) =>
        Combine(left, right, Expression.Subtract);

    private static Expression<Func<double, double, double, double, double>> Multiply(
        Expression<Func<double, double, double, double, double>> left,
        Expression<Func<double, double, double, double, double>> right) =>
        Combine(left, right, Expression.Multiply);

    private static Expression<Func<double, double, double, double, double>> Scale(
        Expression<Func<double, double, double, double, double>> field,
        double scalar)
    {
        var parameters = CreateParameters(field.Parameters);
        return Normalize(Expression.Multiply(Expression.Constant(scalar), Rebind(field, parameters)), parameters);
    }

    private static Expression<Func<double, double, double, double, double>> Combine(
        Expression<Func<double, double, double, double, double>> left,
        Expression<Func<double, double, double, double, double>> right,
        Func<Expression, Expression, BinaryExpression> operation)
    {
        var parameters = CreateParameters(left.Parameters);
        return Normalize(operation(Rebind(left, parameters), Rebind(right, parameters)), parameters);
    }

    private static Expression<Func<double, double, double, double, double>> Normalize(
        Expression body,
        IReadOnlyList<ParameterExpression> parameters)
    {
        // A derivative rule may create an ordinary exact coefficient such as
        // F·0. It is reduced locally before the RICIS O(1) phase, because this
        // zero is a proven coefficient of the derivative rule rather than a
        // geometric 0_F bridge supplied by the original field.
        var finiteBody = new FiniteCalculusReductionVisitor().Visit(body);
        var lambda = Expression.Lambda<Func<double, double, double, double, double>>(
            finiteBody,
            parameters[0],
            parameters[1],
            parameters[2],
            parameters[3]);
        return RicisPhasePipeline.Simplify(lambda) as Expression<Func<double, double, double, double, double>>
            ?? throw new InvalidOperationException("RICIS-конвейер должен сохранить тип четырёхпеременного scalar-поля.");
    }

    private static ParameterExpression[] CreateParameters(IReadOnlyList<ParameterExpression> source) =>
    [
        Expression.Parameter(typeof(double), source[0].Name ?? "x"),
        Expression.Parameter(typeof(double), source[1].Name ?? "y"),
        Expression.Parameter(typeof(double), source[2].Name ?? "z"),
        Expression.Parameter(typeof(double), source[3].Name ?? "t"),
    ];

    private static Expression Rebind(
        Expression<Func<double, double, double, double, double>> field,
        IReadOnlyList<ParameterExpression> target)
    {
        var substitutions = new Dictionary<ParameterExpression, Expression>
        {
            [field.Parameters[0]] = target[0],
            [field.Parameters[1]] = target[1],
            [field.Parameters[2]] = target[2],
            [field.Parameters[3]] = target[3],
        };
        return new ParameterSubstitutionVisitor(substitutions).Visit(field.Body);
    }

    private sealed class FiniteCalculusReductionVisitor : ExpressionVisitor
    {
        protected override Expression VisitUnary(UnaryExpression node)
        {
            var operand = Visit(node.Operand);
            if (node.NodeType == ExpressionType.Negate && operand is ConstantExpression { Value: double value })
            {
                var negated = -value;
                return Expression.Constant(negated == 0.0 ? 0.0 : negated);
            }

            return operand == node.Operand ? node : Expression.MakeUnary(node.NodeType, operand, node.Type, node.Method);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var left = Visit(node.Left);
            var right = Visit(node.Right);
            switch (node.NodeType)
            {
                case ExpressionType.Add:
                    if (IsLiteralZero(left))
                    {
                        return right;
                    }

                    if (IsLiteralZero(right))
                    {
                        return left;
                    }

                    if (IsNegationOf(left, right) || IsNegationOf(right, left))
                    {
                        return Expression.Constant(0.0);
                    }

                    if (left.AreEqual(right))
                    {
                        return Expression.Multiply(Expression.Constant(2.0), left);
                    }

                    if (TryConstant(left, out var leftAddend) && TryConstant(right, out var rightAddend))
                    {
                        return Expression.Constant(leftAddend + rightAddend);
                    }

                    break;
                case ExpressionType.Subtract:
                    if (IsLiteralZero(right))
                    {
                        return left;
                    }

                    if (IsLiteralZero(left))
                    {
                        return Expression.Negate(right);
                    }

                    if (left.AreEqual(right))
                    {
                        return Expression.Constant(0.0);
                    }

                    if (TryConstant(left, out var leftMinuend) && TryConstant(right, out var rightSubtrahend))
                    {
                        return Expression.Constant(leftMinuend - rightSubtrahend);
                    }

                    break;
                case ExpressionType.Multiply:
                    if (IsLiteralZero(left) || IsLiteralZero(right))
                    {
                        return Expression.Constant(0.0);
                    }

                    if (IsLiteralOne(left))
                    {
                        return right;
                    }

                    if (IsLiteralOne(right))
                    {
                        return left;
                    }

                    if (IsLiteralNegativeOne(left))
                    {
                        return Expression.Negate(right);
                    }

                    if (IsLiteralNegativeOne(right))
                    {
                        return Expression.Negate(left);
                    }

                    if (TryConstant(left, out var leftFactor) && TryConstant(right, out var rightFactor))
                    {
                        return Expression.Constant(leftFactor * rightFactor);
                    }

                    if (TryConstant(left, out leftFactor) && TryExtractScalarFactor(right, out rightFactor, out var rightRemainder))
                    {
                        return ScaleFinite(rightRemainder, leftFactor * rightFactor);
                    }

                    if (TryConstant(right, out rightFactor) && TryExtractScalarFactor(left, out leftFactor, out var leftRemainder))
                    {
                        return ScaleFinite(leftRemainder, leftFactor * rightFactor);
                    }

                    break;
                case ExpressionType.Divide:
                    if (TryConstant(left, out var leftDividend) && TryConstant(right, out var rightDivisor) && rightDivisor != 0.0)
                    {
                        return Expression.Constant(leftDividend / rightDivisor);
                    }

                    if (IsLiteralZero(left) && TryConstant(right, out var finiteDivisor) && finiteDivisor != 0.0)
                    {
                        return Expression.Constant(0.0);
                    }

                    if (right is ConstantExpression { Value: double divisor } && divisor != 0.0 &&
                        TryExtractScalarFactor(left, out var factor, out var remainder))
                    {
                        var quotient = factor / divisor;
                        if (quotient == 1.0)
                        {
                            return remainder;
                        }

                        if (quotient == -1.0)
                        {
                            return Expression.Negate(remainder);
                        }

                        return Expression.Multiply(Expression.Constant(quotient), remainder);
                    }

                    break;
            }

            return left == node.Left && right == node.Right
                ? node
                : Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
        }

        private static Expression ScaleFinite(Expression expression, double scalar) => scalar switch
        {
            0.0 => Expression.Constant(0.0),
            1.0 => expression,
            -1.0 => Expression.Negate(expression),
            _ => Expression.Multiply(Expression.Constant(scalar), expression),
        };

        private static bool TryConstant(Expression expression, out double value)
        {
            if (expression is ConstantExpression { Value: double constant })
            {
                value = constant;
                return true;
            }

            value = 0.0;
            return false;
        }

        private static bool TryExtractScalarFactor(Expression expression, out double factor, out Expression remainder)
        {
            if (expression is BinaryExpression { NodeType: ExpressionType.Multiply } product)
            {
                if (TryConstant(product.Left, out factor))
                {
                    remainder = product.Right;
                    return true;
                }

                if (TryConstant(product.Right, out factor))
                {
                    remainder = product.Left;
                    return true;
                }
            }

            factor = 0.0;
            remainder = expression;
            return false;
        }

        private static bool IsLiteralZero(Expression expression) =>
            expression is ConstantExpression { Value: double value } && value == 0.0;

        private static bool IsLiteralOne(Expression expression) =>
            expression is ConstantExpression { Value: double value } && value == 1.0;

        private static bool IsLiteralNegativeOne(Expression expression) =>
            expression is ConstantExpression { Value: double value } && value == -1.0;

        private static bool IsNegationOf(Expression candidate, Expression source) =>
            candidate is UnaryExpression { NodeType: ExpressionType.Negate } unary && unary.Operand.AreEqual(source);
    }

    private sealed class ParameterSubstitutionVisitor : ExpressionVisitor
    {
        private readonly IReadOnlyDictionary<ParameterExpression, Expression> _substitutions;

        public ParameterSubstitutionVisitor(IReadOnlyDictionary<ParameterExpression, Expression> substitutions)
        {
            _substitutions = substitutions;
        }

        protected override Expression VisitParameter(ParameterExpression node) =>
            _substitutions.TryGetValue(node, out var replacement) ? replacement : node;
    }

    private sealed class FourVariableDerivativeBuilder
    {
        private readonly ParameterExpression _coordinate;

        public FourVariableDerivativeBuilder(ParameterExpression coordinate)
        {
            _coordinate = coordinate;
        }

        public Expression Build(Expression expression) => expression switch
        {
            ConstantExpression => Expression.Constant(0.0),
            ParameterExpression parameter => Expression.Constant(ReferenceEquals(parameter, _coordinate) ? 1.0 : 0.0),
            UnaryExpression { NodeType: ExpressionType.Negate } unary => Expression.Negate(Build(unary.Operand)),
            BinaryExpression { NodeType: ExpressionType.Add } binary => Expression.Add(Build(binary.Left), Build(binary.Right)),
            BinaryExpression { NodeType: ExpressionType.Subtract } binary => Expression.Subtract(Build(binary.Left), Build(binary.Right)),
            BinaryExpression { NodeType: ExpressionType.Multiply } binary => Expression.Add(
                Expression.Multiply(Build(binary.Left), binary.Right),
                Expression.Multiply(binary.Left, Build(binary.Right))),
            BinaryExpression { NodeType: ExpressionType.Divide } binary => Expression.Divide(
                Expression.Subtract(
                    Expression.Multiply(binary.Right, Build(binary.Left)),
                    Expression.Multiply(binary.Left, Build(binary.Right))),
                Expression.Multiply(binary.Right, binary.Right)),
            _ => new DeferredDerivativeExpression(expression, _coordinate),
        };
    }
}
