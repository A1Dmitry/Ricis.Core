using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Solvers;

namespace Ricis.Core.Simplifiers;

/// <summary>
/// RICIS Phase 2 — pure singularity transform.
/// NO limits. NO L'Hôpital. NO ε-δ.
///
/// Theory v7.7:
///   SP2 already applied upstream (AlgebraicReductionVisitor).
///   A1:  F(a)/0  → ∞_{F(a)} at every certified key a (F(a) ≠ 0)
///   A4:  0_F/0_G = F/G  (structural ratio of deferred indices; 1 iff F≡G via SP2)
///   SP4: preserve every certified key while applying the index in that key.
/// </summary>
public class RicisTransformVisitor : ExpressionVisitor, IExpressionVisitor
{
    /// <inheritdoc />
    protected override Expression VisitBinary(BinaryExpression node)
    {
        // A1/A4 derive singularities only from finite denominators. An already
        // indexed infinity is a completed special form and must reach Phase 5,
        // where the normative F/∞_G -> 0_F rule preserves its roots.
        if (node.NodeType == ExpressionType.Divide && node.Right is InfinityExpression)
        {
            return node;
        }

        // A1/A4 redefine only built-in division. Overloaded operators belong to
        // their scalar type and therefore follow classical evaluation exactly.
        if (node.NodeType == ExpressionType.Divide &&
            (node.Method is null || NumericConstants.IsIntrinsicNumeric(node.Type)))
        {
            return SimplifyDivision(node.Left, node.Right);
        }
        return base.VisitBinary(node);
    }

    /// <inheritdoc />
    protected override Expression VisitExtension(Expression node) => node;

    private Expression SimplifyDivision(Expression numerator, Expression denominator)
    {
        // Limit form: F/0 → ∞_F. The index F stays deferred; an explicit
        // limit point is not required for this symbolic representation.
        if (denominator.IsZero())
        {
            return InfinityExpression.CreateLazy(numerator, []);
        }

        // SP2: (F/A) / (G/A) → F/G. The common factor A is syntactic and
        // deferred, so it is cancelled before A1/A5 even when A is zero.
        if (numerator is BinaryExpression { NodeType: ExpressionType.Divide } leftRatio &&
            denominator is BinaryExpression { NodeType: ExpressionType.Divide } rightRatio &&
            leftRatio.Right.AreEqual(rightRatio.Right))
        {
            return SimplifyDivision(leftRatio.Left, rightRatio.Left);
        }

        // Algebraic cleanup of a fully constant ratio produced by SP2.
        if (numerator is ConstantExpression { Value: double leftConstant } &&
            denominator is ConstantExpression { Value: double rightConstant } &&
            rightConstant != 0.0)
        {
            var quotient = leftConstant / rightConstant;
            // Keep a non-integral rational form as an expression for later
            // RICIS phases. Only a completed integral result becomes a scalar.
            if (double.IsFinite(quotient) && Math.Abs(quotient - Math.Round(quotient)) < 1e-12)
            {
                return Expression.Constant(quotient);
            }
        }

        // SP2 / L1: identical expressions → 1 (already mostly done upstream;
        // keep as safety net).
        if (numerator.AreEqual(denominator))
        {
            return NumericConstants.OneOf(numerator.Type);
        }

        var tempSingularities = new List<InfinityExpression>();

        // 1. Polynomial roots of denominator
        foreach (var root in denominator.SolveRoots())
        {
            if (double.IsNaN(root.value)) continue;

            var numVal = numerator.EvaluateAtPoint(root.value, root.expr.Name);
            if (double.IsNaN(numVal)) continue;

            if (Math.Abs(numVal) < 1e-10)
            {
                // A4: 0_F / 0_G = F/G
                // Indices are the parent expressions (SP4). No limit, no derivatives.
                // If SP2 canceled identical factors, we would not reach here with 0/0.
                // Remaining case: different identities → keep structural ratio F/G.
                return Expression.Divide(numerator, denominator);
            }

            // A1: F(a) ≠ 0 and den(a) = 0 → ∞_{F(a)}.
            numerator.AddSingularityIfValid(root.expr, root.value, tempSingularities);
        }

        // 2. Trigonometric roots
        var trigRoot = TrigSolver.Solve(denominator);
        if (trigRoot.HasValue)
        {
            var (param, value) = trigRoot.Value;
            if (!double.IsNaN(value))
            {
                var numVal = numerator.EvaluateAtPoint(value, param.Name);

                if (Math.Abs(numVal) < 1e-10)
                {
                    // A4 again: 0_F/0_G = F/G (e.g. sin(x)/x keeps structural ratio;
                    // SP2/series are NOT applied here — pure index law).
                    return Expression.Divide(numerator, denominator);
                }

                numerator.AddSingularityIfValid(param, value, tempSingularities);
            }
        }

        if (tempSingularities.Count == 0)
        {
            return Expression.Divide(numerator, denominator);
        }

        if (tempSingularities.Count == 1)
        {
            return tempSingularities[0];
        }

        // Multiple A1 poles are grouped only when their reduced indices F(a)
        // are identical. Selecting the first index for every root would lose
        // the key-to-index correspondence required by A1.
        var branches = tempSingularities
            .OfType<PoleInfinityExpression>()
            .GroupBy(pole => pole.Numerator.ToString(), StringComparer.Ordinal)
            .Select(group => new PoleInfinityExpression(
                group.First().Numerator,
                DistinctSortedRoots(group.SelectMany(pole => pole.Roots)),
                []))
            .Where(branch => branch.Roots.Count > 0)
            .ToList();

        if (branches.Count == 0)
        {
            return Expression.Divide(numerator, denominator);
        }

        return branches.Count == 1
            ? branches[0]
            : new KeyedInfinityExpression(branches);
    }

    private static List<(ParameterExpression Param, double Value)> DistinctSortedRoots(
        IEnumerable<(ParameterExpression Param, double Value)> roots) =>
        roots
            .Where(root => double.IsFinite(root.Value))
            .OrderBy(root => root.Value)
            .Aggregate(new List<(ParameterExpression Param, double Value)>(), (distinct, root) =>
            {
                // SP4 preserves every certified key. Only numerically identical
                // discovery duplicates are merged; close roots remain distinct.
                if (!distinct.Any(existing => existing.Param == root.Param &&
                                             Math.Abs(existing.Value - root.Value) <= 1e-8))
                {
                    distinct.Add(root);
                }

                return distinct;
            });
}
