// RicisPhasePipeline.cs — strict RICIS v7.7 (no classical limits)

using System.Linq.Expressions;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Metadata;
using Ricis.Core.Simplifiers;

namespace Ricis.Core.Phases;

/// <summary>
/// Normative RICIS phase order:
/// identity of essence F≡F→1; polar reduction; SP2 algebra; O(1) bridges;
/// A1/A4 singular transforms; and standard operations A5–A7 plus indexed-zero
/// rules. Operations outside an explicit RICIS rule retain classical semantics.
/// </summary>
public static class RicisPhasePipeline
{
    private static readonly List<IExpressionVisitor> Visitors =
    [
        new IdentityReductionVisitor(),
        new PolarTrigVisitor(),
        new AlgebraicReductionVisitor(),
        new LimitBridgeVisitor(),
        new RicisTransformVisitor(),
        new TypeConsistencyVisitor(),
        new StandardOperationsVisitor(),
    ];

    /// <summary>
    /// Simplifies an expression through the complete normative RICIS pipeline.
    /// </summary>
    public static Expression Simplify(Expression expr) => SimplifyCore(expr, null);

    /// <summary>
    /// Simplifies an expression through the normative RICIS pipeline and appends
    /// one immutable trace record for every phase attempt. The supplied trace is
    /// output-only: existing records are preserved and the input expression is
    /// never executed during simplification.
    /// </summary>
    public static Expression SimplifyWithTrace(Expression expr, ICollection<RicisPhaseTraceStep> trace)
    {
        ArgumentNullException.ThrowIfNull(trace);
        return SimplifyCore(expr, trace);
    }

    private static Expression SimplifyCore(Expression expr, ICollection<RicisPhaseTraceStep> trace)
    {
        ArgumentNullException.ThrowIfNull(expr);

        // Metadata is opt-in: it appears when a source lambda captures an
        // outer variable or uses a parameter exactly named "about".
        var authorProfile = AboutCaptureDetector.IsAboutOptIn(expr)
            ? AuthorSeoProfile.RicisAuthor
            : null;

        var result = expr;
        foreach (var visitor in Visitors)
        {
            var before = result;
            var (phaseName, ruleFamily) = Describe(visitor);
            if (MustSkip(visitor, result))
            {
                trace?.Add(new RicisPhaseTraceStep(phaseName, ruleFamily, before, before, wasSkipped: true));
                continue;
            }

            try
            {
                result = visitor.Visit(result);
            }
            catch (Exception error)
            {
                throw new InvalidOperationException(
                    $"Фаза RICIS {visitor.GetType().Name} не смогла преобразовать выражение типа {result.Type}.",
                    error);
            }

            trace?.Add(new RicisPhaseTraceStep(phaseName, ruleFamily, before, result, wasSkipped: false));
        }

        if (authorProfile is not null && result is LambdaExpression lambda)
        {
            var before = result;
            result = Expression.Lambda(
                lambda.Type,
                new AuthorAnnotatedExpression(lambda.Body, authorProfile),
                lambda.Name,
                lambda.TailCall,
                lambda.Parameters);
            trace?.Add(new RicisPhaseTraceStep(
                "Метафаза автора",
                "META — opt-in SEO-аннотация about",
                before,
                result,
                wasSkipped: false));
        }

        return result;
    }

    private static bool MustSkip(IExpressionVisitor visitor, Expression result)
    {
        if (visitor is ExpressionSimplifierVisitor)
        {
            // A final ordinary-algebra pass must not rewrite an expression tree
            // that still carries RICIS extension payload. A6/L0 results can be
            // ordinary-looking products after materialization, but their
            // structural form is normative and must remain untouched.
            return ContainsRicisExpression(result);
        }

        if (visitor is not RicisTransformVisitor)
        {
            return false;
        }

        if (result is LambdaExpression { Body: LazyInfinityExpression { CanReduce: true } })
        {
            return true;
        }

        // Certified root discovery and key substitution are presently a double
        // facility. Finite generic algebra and O(1) bridges remain typed and do
        // not coerce their scalar domain only to search for numeric roots.
        return result is LambdaExpression lambda && lambda.ReturnType != typeof(double);
    }

    private static bool ContainsRicisExpression(Expression expression)
    {
        var finder = new RicisExpressionFinder();
        finder.Visit(expression);
        return finder.Found;
    }

    private sealed class RicisExpressionFinder : ExpressionVisitor
    {
        public bool Found { get; private set; }

        public override Expression Visit(Expression node)
        {
            if (node is RicisExpression)
            {
                Found = true;
                return node;
            }

            return base.Visit(node);
        }
    }

    private static (string PhaseName, string RuleFamily) Describe(IExpressionVisitor visitor) => visitor switch
    {
        IdentityReductionVisitor => ("Фаза 0 — тождество сущности", "ID-01 / L1: F/F → 1"),
        PolarTrigVisitor => ("Фаза 0.5 — полярная тригонометрия", "POL: точные полярные тождества"),
        AlgebraicReductionVisitor => ("Фаза 1 — структурная алгебра", "SP2: сокращение до сингулярностей"),
        LimitBridgeVisitor => ("Фаза 1.5 — мосты O(1)", "LIM: F·0 → 0_F, F/0 → ∞_F"),
        RicisTransformVisitor => ("Фаза 2 — сингулярное преобразование", "A1/A4: индексирование и отношение нулей"),
        TypeConsistencyVisitor => ("Фаза 4 — согласованность типов", "SP3: сохранение типа и ключей payload"),
        StandardOperationsVisitor => ("Фаза 5 — стандартные операции", "Z-01/Z-02, A5/A6/A7"),
        _ => (visitor.GetType().Name, "Нормативная фаза RICIS")
    };
}
