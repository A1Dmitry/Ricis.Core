using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Phases;
using Ricis.Core.Simplifiers;

namespace Ricis.Core.Proofs;

/// <summary>
/// Proves the RICIS identity for one delayed function evaluated at x and x−1
/// when the two responses are structurally equal.
/// </summary>
/// <remarks>
/// The case constructs <c>(F(x)/F(x−1))-1</c>. The second parameter is the
/// exact previous parameter expression <c>x−1</c>; no independent y parameter
/// and no periodicity claim are introduced.
/// </remarks>
public sealed class RicisPreviousParameterIdentityProofCase : RicisProofCase
{
    private readonly Expression<Func<double, double>> function;

    /// <summary>
    /// Initializes a previous-parameter identity proof case.
    /// </summary>
    /// <param name="function">The delayed unary function F(u).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="function"/> is null.</exception>
    public RicisPreviousParameterIdentityProofCase(Expression<Func<double, double>> function)
    {
        ArgumentNullException.ThrowIfNull(function);
        this.function = function;
    }

    /// <inheritdoc />
    public override string Name => "RICIS previous-parameter identity";

    /// <inheritdoc />
    public override IReadOnlyList<string> UnresolvedObligations => [];

    /// <inheritdoc />
    protected override RicisProofCaseResult Execute(ICollection<RicisProofMonitorEntry> events)
    {
        var x = Expression.Parameter(typeof(double), "x");
        var previous = Expression.Subtract(x, Expression.Constant(1.0));
        var responseAtX = Rebind(function, x);
        var responseAtPrevious = Rebind(function, previous);
        var responseX = Expression.Lambda<Func<double, double>>(responseAtX, x);
        var responsePrevious = Expression.Lambda<Func<double, double>>(responseAtPrevious, x);
        var simplifiedX = Extract(RicisPhasePipeline.Simplify(responseX));
        var simplifiedPrevious = Extract(RicisPhasePipeline.Simplify(responsePrevious));

        AddMonitor("PARAMETERS", "PASS", $"Constructed F(x)={simplifiedX} and F(x−1)={simplifiedPrevious}.");
        if (!AreResponseBodiesEqual(simplifiedX.Body, simplifiedPrevious.Body))
        {
            AddMonitor("IDENTITY", "BLOCKED", "The responses F(x) and F(x−1) are different; their equality is not certified.");
            throw new ArgumentException(
                "The previous-parameter identity requires structurally equal responses F(x) and F(x−1).",
                nameof(function));
        }

        AddMonitor("IDENTITY", "PASS", "Certified F(x)=F(x−1) by response identity.");
        var ratioMinusOne = Expression.Lambda<Func<double, double>>(
            Expression.Subtract(
                Expression.Divide(simplifiedX.Body, simplifiedPrevious.Body),
                Expression.Constant(1.0)),
            x);
        AddMonitor("RATIO", "START", $"Built (F(x)/F(x−1))-1={ratioMinusOne}.");
        var phaseDerived = Extract(RicisPhasePipeline.Simplify(ratioMinusOne));
        var derivedBody = phaseDerived.Body;
        if (derivedBody is BinaryExpression
            {
                NodeType: ExpressionType.Subtract,
                Left: var left,
                Right: var right
            } && IsUnit(left) && IsUnit(right))
        {
            derivedBody = NumericConstants.ZeroOf(derivedBody.Type);
        }
        else
        {
            derivedBody = new ExpressionSimplifierVisitor().Visit(derivedBody);
        }

        var derived = Expression.Lambda<Func<double, double>>(derivedBody, x);
        AddMonitor("RATIO", "PASS", $"RICIS identity reduction applied 1−1→0 and produced {derived}.");
        return new RicisProofCaseResult(
            "FiniteDerivation",
            derived.ToString(),
            $"Certified premise: F(x) = {simplifiedX.Body}; F(x−1) = {simplifiedPrevious.Body}{Environment.NewLine}Derived identity: {ratioMinusOne} → {derived}");
    }

    private static Expression Rebind(Expression<Func<double, double>> source, Expression target) =>
        new ParameterReplaceVisitor(source.Parameters[0], target).Visit(source.Body)!;

    private static bool AreResponseBodiesEqual(Expression left, Expression right) =>
        left.AreEqual(right) ||
        left is ConstantExpression leftConstant &&
        right is ConstantExpression rightConstant &&
        Equals(leftConstant.Value, rightConstant.Value);

    private static bool IsUnit(Expression expression) =>
        expression is ConstantExpression { Value: double value } && value == 1.0;

    private static Expression<Func<double, double>> Extract(Expression expression) =>
        expression as Expression<Func<double, double>>
        ?? throw new InvalidOperationException($"Expected unary scalar lambda, got {expression}.");

    private sealed class ParameterReplaceVisitor(ParameterExpression source, Expression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == source ? target : base.VisitParameter(node);
    }
}
