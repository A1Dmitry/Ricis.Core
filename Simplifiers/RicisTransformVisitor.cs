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
///   A1:  F/0  → ∞_F     (F ≠ 0)
///   A4:  0_F/0_G = F/G  (structural ratio of indices; 1 iff F≡G via SP2)
///   SP4: index by expression, not numerical value.
/// </summary>
public class RicisTransformVisitor : ExpressionVisitor, IExpressionVisitor
{
    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.Divide)
        {
            return SimplifyDivision(node.Left, node.Right);
        }
        return base.VisitBinary(node);
    }

    private Expression SimplifyDivision(Expression numerator, Expression denominator)
    {
        // SP2: (F/A) / (G/A) → F/G. The common factor A is syntactic and
        // deferred, so it is cancelled before A1/A5 even when A is zero.
        if (numerator is BinaryExpression { NodeType: ExpressionType.Divide } leftRatio &&
            denominator is BinaryExpression { NodeType: ExpressionType.Divide } rightRatio &&
            leftRatio.Right.AreEqual(rightRatio.Right))
        {
            return Expression.Divide(leftRatio.Left, rightRatio.Left);
        }

        // SP2 / L1: identical expressions → 1 (already mostly done upstream;
        // keep as safety net).
        if (numerator.AreEqual(denominator))
        {
            return RicisType.InfinityOne;
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

            // A1: F ≠ 0, den = 0 → ∞_F (index = numerator expression)
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

        // Multiple poles → monolith
        var primaryIndex = tempSingularities[0].Numerator;
        var allRoots = tempSingularities
            .SelectMany(s => s.Roots)
            .Where(r => !double.IsNaN(r.Value))
            .GroupBy(r => Math.Round(r.Value, 4))
            .Select(g => g.First())
            .OrderBy(r => r.Value)
            .ToList();

        if (allRoots.Count == 0)
        {
            return Expression.Divide(numerator, denominator);
        }

        return InfinityExpression.CreateLazy(primaryIndex, allRoots);
    }
}
