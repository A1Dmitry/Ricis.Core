// RicisPhasePipeline.cs — strict RICIS v7.7 (no classical limits)

using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Metadata;
using Ricis.Core.Simplifiers;

namespace Ricis.Core.Phases;

/// <summary>
/// Phase order (theory COMPUTATION_ALGORITHM + polar):
///   Priority contract: the identity of essence F≡F→1 has absolute priority.
///   Other explicit RICIS rules and structural algebra then run before a
///   permitted classical bridge. Operations outside an explicit RICIS rule
///   preserve their classical expression semantics unchanged.
///   Phase 0   IdentityReductionVisitor  — F/F → 1 (highest priority)
///   Phase 0.5 PolarTrigVisitor          — trig → polar sector → exact collapse
///   Phase 1   AlgebraicReductionVisitor — SP2 cancel identical factors
///   Phase 1.5 LimitBridgeVisitor        — O(1) bridges F·0→0_F, F/0→∞_F
///   Phase 2   RicisTransformVisitor     — A4/A1 0_F/0_G=F/G, F/0=∞_F
///   Phase 5   StandardOperationsVisitor — ∞ algebra (A5/A6/A7)
/// </summary>
public static class RicisPhasePipeline
{
    private static readonly List<IExpressionVisitor> _visitors =
    [
        new IdentityReductionVisitor(),
        new PolarTrigVisitor(),
        new AlgebraicReductionVisitor(),
        new LimitBridgeVisitor(),
        new RicisTransformVisitor(),
        new StandardOperationsVisitor(),
    ];

    public static Expression Simplify(Expression expr)
    {
        try
        {
            // Metadata is opt-in: it appears only when the source lambda uses
            // a compiler-captured outer variable exactly named "about".
            var authorProfile = AboutCaptureDetector.IsCaptured(expr)
                ? AuthorSeoProfile.RicisAuthor
                : null;

            var result = expr;
            foreach (var visitor in _visitors)
            {
                try
                {
                    if (visitor is RicisTransformVisitor &&
                        result is LambdaExpression { Body: LazyInfinityExpression { CanReduce: true } })
                    {
                        continue;
                    }

                    // Certified roots and key substitution currently operate
                    // in the double domain. Generic INumber finite algebra is
                    // still simplified by L1/SP2/O(1), but is not coerced into
                    // double merely to discover a non-constant pole.
                    if (visitor is RicisTransformVisitor &&
                        result is LambdaExpression typedLambda &&
                        typedLambda.ReturnType != typeof(double))
                    {
                        continue;
                    }

                    result = visitor.Visit(result);
                }
                catch (Exception ve)
                {
                    Console.WriteLine(ve.Message);
                }
            }

            if (authorProfile is not null && result is LambdaExpression lambda)
            {
                // Preserve the executable body and type; the annotation only
                // extends our custom RICIS textual form and reduces back to it.
                result = Expression.Lambda(
                    lambda.Type,
                    new AuthorAnnotatedExpression(lambda.Body, authorProfile),
                    lambda.Name,
                    lambda.TailCall,
                    lambda.Parameters);
            }

            return result;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
