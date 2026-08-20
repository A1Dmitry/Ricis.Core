// RicisPhasePipeline.cs — strict RICIS v7.7 (no classical limits)

using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Logging;
using Ricis.Core.Metadata;
using Ricis.Core.Simplifiers;

namespace Ricis.Core.Phases;

/// <summary>
/// Normative RICIS phase order: identity of essence F≡F→1; polar reduction;
/// SP2 algebra; O(1) bridges; A1/A4 singular transforms; and standard
/// operations A5–A7 plus indexed-zero rules. Operations outside an explicit
/// RICIS rule retain classical semantics.
/// </summary>
public static class RicisPhasePipeline
{
    private static IReadOnlyList<IRicisPipelineStage> CreateStages(IRicisScalarPolicy scalarPolicy) =>
    [
        new RicisPipelineStage<IdentityReductionVisitor>(new IdentityReductionVisitor(scalarPolicy)),
        new RicisPipelineStage<PolarTrigVisitor>(new PolarTrigVisitor()),
        new RicisPipelineStage<AlgebraicReductionVisitor>(new AlgebraicReductionVisitor(scalarPolicy)),
        new RicisPipelineStage<LogicalReductionVisitor>(new LogicalReductionVisitor()),
        new RicisPipelineStage<LimitBridgeVisitor>(new LimitBridgeVisitor(scalarPolicy)),
        new RicisPipelineStage<RicisTransformVisitor>(new RicisTransformVisitor(scalarPolicy)),
        new RicisPipelineStage<TypeConsistencyVisitor>(new TypeConsistencyVisitor()),
        new RicisPipelineStage<StandardOperationsVisitor>(new StandardOperationsVisitor(scalarPolicy)),
    ];

    /// <summary>Simplifies an expression through the complete normative RICIS pipeline.</summary>
    public static Expression Simplify(Expression expr) =>
        SimplifyCore<object>(expr, null, null, RicisScalarPolicies.Legacy);

    /// <summary>
    /// Simplifies an expression through the normative RICIS pipeline and appends
    /// one immutable trace record for every phase attempt. The supplied trace is
    /// output-only: existing records are preserved and the input expression is
    /// never executed during simplification.
    /// </summary>
    public static Expression SimplifyWithTrace(Expression expr, ICollection<RicisPhaseTraceStep> trace)
    {
        ArgumentNullException.ThrowIfNull(trace);
        return SimplifyCore<object>(expr, trace, null, RicisScalarPolicies.Legacy);
    }

    /// <summary>
    /// Simplifies an expression and records a typed, renderer-independent audit
    /// event sequence. The source type of orchestration events is
    /// <typeparamref name="TLogStage"/>; individual visitor events use typed
    /// child logs backed by the same canonical journal.
    /// </summary>
    public static Expression SimplifyWithLog<TLogStage>(Expression expr, ILog<TLogStage> log)
    {
        ArgumentNullException.ThrowIfNull(log);
        return SimplifyCore(expr, null, log, RicisScalarPolicies.Legacy);
    }

    /// <summary>
    /// Simplifies an expression while preserving the existing RICIS phase trace
    /// and publishing the same phase attempts to a typed proof-log journal.
    /// </summary>
    public static Expression SimplifyWithTraceAndLog<TLogStage>(
        Expression expr,
        ICollection<RicisPhaseTraceStep> trace,
        ILog<TLogStage> log)
    {
        ArgumentNullException.ThrowIfNull(trace);
        ArgumentNullException.ThrowIfNull(log);
        return SimplifyCore(expr, trace, log, RicisScalarPolicies.Legacy);
    }

    /// <summary>
    /// Simplifies a unary expression through the universal generic numeric route.
    /// The scalar type remains explicit for every phase and no registration is required.
    /// </summary>
    public static Expression<Func<T, T>> Simplify<T>(Expression<Func<T, T>> expression)
        where T : INumber<T> =>
        SimplifyGenericCore<T, object>(expression, null, null);

    /// <summary>Generic unary simplification with an immutable phase trace.</summary>
    public static Expression<Func<T, T>> SimplifyWithTrace<T>(
        Expression<Func<T, T>> expression,
        ICollection<RicisPhaseTraceStep> trace)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(trace);
        return SimplifyGenericCore<T, object>(expression, trace, null);
    }

    /// <summary>Generic unary simplification with a typed proof-log journal.</summary>
    public static Expression<Func<T, T>> SimplifyWithLog<T, TLogStage>(
        Expression<Func<T, T>> expression,
        ILog<TLogStage> log)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(log);
        return SimplifyGenericCore(expression, null, log);
    }

    /// <summary>Generic unary simplification with both trace and typed proof log.</summary>
    public static Expression<Func<T, T>> SimplifyWithTraceAndLog<T, TLogStage>(
        Expression<Func<T, T>> expression,
        ICollection<RicisPhaseTraceStep> trace,
        ILog<TLogStage> log)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(trace);
        ArgumentNullException.ThrowIfNull(log);
        return SimplifyGenericCore(expression, trace, log);
    }

    private static Expression<Func<T, T>> SimplifyGenericCore<T, TLogStage>(
        Expression<Func<T, T>> expression,
        ICollection<RicisPhaseTraceStep> trace,
        ILog<TLogStage> log)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(expression);
        var simplified = SimplifyCore(expression, trace, log, RicisScalarPolicies.For<T>());
        return simplified as Expression<Func<T, T>>
            ?? throw new InvalidOperationException(
                $"Generic RICIS pipeline changed lambda type {typeof(Func<T, T>)} to {simplified.Type}.");
    }

    private static Expression SimplifyCore<TLogStage>(
        Expression expr,
        ICollection<RicisPhaseTraceStep> trace,
        ILog<TLogStage> log,
        IRicisScalarPolicy scalarPolicy)
    {
        ArgumentNullException.ThrowIfNull(expr);
        ArgumentNullException.ThrowIfNull(scalarPolicy);
        log?.Info(
            "RICIS_PIPELINE_START",
            "Запущен нормативный RICIS phase pipeline.",
            new Dictionary<string, string>
            {
                ["inputType"] = expr.Type.FullName ?? expr.Type.Name,
                ["inputExpression"] = expr.ToString(),
            });

        // Metadata is opt-in: it appears when a source lambda captures an
        // outer variable or uses a parameter exactly named "about".
        var authorProfile = AboutCaptureDetector.IsAboutOptIn(expr)
            ? AuthorSeoProfile.RicisAuthor
            : null;

        var result = expr;
        foreach (var stage in CreateStages(scalarPolicy))
        {
            result = stage.Apply(result, trace, log);
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
            log?.For<AuthorAnnotatedExpression>().Trace(
                "RICIS_AUTHOR_ANNOTATION",
                "Применена opt-in SEO-аннотация автора.",
                before.ToString(),
                result.ToString(),
                new Dictionary<string, string>
                {
                    ["phaseName"] = "Метафаза автора",
                    ["ruleFamily"] = "META — opt-in SEO-аннотация about",
                    ["wasSkipped"] = bool.FalseString,
                });
        }

        log?.Info(
            "RICIS_PIPELINE_COMPLETE",
            "Нормативный RICIS phase pipeline завершён.",
            new Dictionary<string, string>
            {
                ["outputType"] = result.Type.FullName ?? result.Type.Name,
                ["outputExpression"] = result.ToString(),
            });
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

    private interface IRicisPipelineStage
    {
        Expression Apply<TLogStage>(
            Expression result,
            ICollection<RicisPhaseTraceStep> trace,
            ILog<TLogStage> log);
    }

    private sealed class RicisPipelineStage<TVisitor> : IRicisPipelineStage
        where TVisitor : IExpressionVisitor
    {
        private readonly TVisitor _visitor;

        public RicisPipelineStage(TVisitor visitor)
        {
            _visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
        }

        public Expression Apply<TLogStage>(
            Expression result,
            ICollection<RicisPhaseTraceStep> trace,
            ILog<TLogStage> log)
        {
            var before = result;
            var (phaseName, ruleFamily) = Describe(_visitor);
            var stageLog = log?.For<TVisitor>();
            var attributes = new Dictionary<string, string>
            {
                ["phaseName"] = phaseName,
                ["ruleFamily"] = ruleFamily,
                ["visitorType"] = typeof(TVisitor).FullName ?? typeof(TVisitor).Name,
            };

            if (MustSkip(_visitor, result))
            {
                trace?.Add(new RicisPhaseTraceStep(phaseName, ruleFamily, before, before, wasSkipped: true));
                attributes["wasSkipped"] = bool.TrueString;
                stageLog?.Warning(
                    "RICIS_PHASE_SKIPPED",
                    "Фаза была пропущена из-за документированного precondition.",
                    attributes);
                stageLog?.Trace(
                    "RICIS_PHASE_TRACE",
                    "Зафиксирована пропущенная фаза без изменения expression tree.",
                    before.ToString(),
                    before.ToString(),
                    attributes);
                return result;
            }

            try
            {
                result = _visitor.Visit(result);
            }
            catch (Exception error)
            {
                attributes["wasSkipped"] = bool.FalseString;
                stageLog?.Exception(
                    "RICIS_PHASE_EXCEPTION",
                    error,
                    $"Фаза {typeof(TVisitor).Name} не смогла преобразовать выражение типа {before.Type}.",
                    attributes);
                throw new InvalidOperationException(
                    $"Фаза RICIS {typeof(TVisitor).Name} не смогла преобразовать выражение типа {before.Type}.",
                    error);
            }

            trace?.Add(new RicisPhaseTraceStep(phaseName, ruleFamily, before, result, wasSkipped: false));
            attributes["wasSkipped"] = bool.FalseString;
            attributes["changed"] = (!before.AreEqual(result)).ToString();
            stageLog?.Info("RICIS_PHASE_COMPLETE", "Нормативная фаза завершена.", attributes);
            stageLog?.Trace(
                "RICIS_PHASE_TRACE",
                "Зафиксирована попытка нормативного преобразования.",
                before.ToString(),
                result.ToString(),
                attributes);
            return result;
        }
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
        LogicalReductionVisitor => ("Фаза 1.25 — логическая редукция", "LOG: безопасная минимизация Boolean expression tree"),
        LimitBridgeVisitor => ("Фаза 1.5 — мосты O(1)", "LIM: F·0 → 0_F, F/0 → ∞_F"),
        RicisTransformVisitor => ("Фаза 2 — сингулярное преобразование", "A1/A4: индексирование и отношение нулей"),
        TypeConsistencyVisitor => ("Фаза 4 — согласованность типов", "SP3: сохранение типа и ключей payload"),
        StandardOperationsVisitor => ("Фаза 5 — стандартные операции", "Z-01/Z-02, A5/A6/A7"),
        _ => (visitor.GetType().Name, "Нормативная фаза RICIS"),
    };
}
