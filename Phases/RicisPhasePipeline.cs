// RicisPhasePipeline.cs — strict RICIS v7.7 (no classical limits)

using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Logging;
using Ricis.Core.Metadata;
using Ricis.Core.Resources;
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
    /// Simplifies an expression through the normative RICIS pipeline with an
    /// optional typed audit journal. A null journal preserves the legacy route.
    /// </summary>
    public static Expression Simplify<TLogStage>(Expression expr, ILog<TLogStage> log = null) =>
        SimplifyCore(expr, null, log, RicisScalarPolicies.Legacy);

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
    /// Simplifies an expression with an optional trace collection and optional
    /// typed journal. The existing two-argument trace overload remains unchanged.
    /// </summary>
    public static Expression SimplifyWithTrace<TLogStage>(
        Expression expr,
        ICollection<RicisPhaseTraceStep> trace,
        ILog<TLogStage> log = null)
    {
        ArgumentNullException.ThrowIfNull(trace);
        return SimplifyCore(expr, trace, log, RicisScalarPolicies.Legacy);
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
        return Simplify(expr, log);
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

    /// <summary>
    /// Generic unary simplification with an optional typed journal. A null journal
    /// is intentionally equivalent to the legacy generic simplifier.
    /// </summary>
    public static Expression<Func<T, T>> Simplify<T, TLogStage>(
        Expression<Func<T, T>> expression,
        ILog<TLogStage> log = null)
        where T : INumber<T> => SimplifyGenericCore(expression, null, log);

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
        return Simplify(expression, log);
    }

    /// <summary>
    /// Generic unary simplification with an optional trace collection and typed
    /// journal. Null log disables event publication without changing the result.
    /// </summary>
    public static Expression<Func<T, T>> SimplifyWithTrace<T, TLogStage>(
        Expression<Func<T, T>> expression,
        ICollection<RicisPhaseTraceStep> trace,
        ILog<TLogStage> log = null)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(trace);
        return SimplifyGenericCore(expression, trace, log);
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
            RicisLegacyTextResources.Get("runtime.legacy.b5012a0952c4"),
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
                RicisLegacyTextResources.Get("runtime.legacy.ec350e79b2d7"),
                RicisLegacyTextResources.Get("runtime.legacy.33485f9eae04"),
                before,
                result,
                wasSkipped: false));
            log?.For<AuthorAnnotatedExpression>().Trace(
                "RICIS_AUTHOR_ANNOTATION",
                RicisLegacyTextResources.Get("runtime.legacy.2917bc23233d"),
                before.ToString(),
                result.ToString(),
                new Dictionary<string, string>
                {
                    ["phaseName"] = RicisLegacyTextResources.Get("runtime.legacy.ec350e79b2d7"),
                    ["ruleFamily"] = RicisLegacyTextResources.Get("runtime.legacy.33485f9eae04"),
                    ["wasSkipped"] = bool.FalseString,
                });
        }

        log?.Info(
            "RICIS_PIPELINE_COMPLETE",
            RicisLegacyTextResources.Get("runtime.legacy.3fee62ba3c42"),
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
                    RicisLegacyTextResources.Get("runtime.legacy.57176ff633d9"),
                    attributes);
                stageLog?.Trace(
                    "RICIS_PHASE_TRACE",
                    RicisLegacyTextResources.Get("runtime.legacy.5d068bd9d4b8"),
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
                    string.Format(CultureInfo.CurrentUICulture, RicisLegacyTextResources.Get("runtime.legacy.5f025eed03f0"), typeof(TVisitor).Name, before.Type),
                    attributes);
                throw new InvalidOperationException(
                    string.Format(CultureInfo.CurrentUICulture, RicisLegacyTextResources.Get("runtime.legacy.b27e90881808"), typeof(TVisitor).Name, before.Type),
                    error);
            }

            trace?.Add(new RicisPhaseTraceStep(phaseName, ruleFamily, before, result, wasSkipped: false));
            attributes["wasSkipped"] = bool.FalseString;
            attributes["changed"] = (!before.AreEqual(result)).ToString();
            stageLog?.Info("RICIS_PHASE_COMPLETE", RicisLegacyTextResources.Get("runtime.legacy.1117f66fa728"), attributes);
            stageLog?.Trace(
                "RICIS_PHASE_TRACE",
                RicisLegacyTextResources.Get("runtime.legacy.4a1dc8f154fb"),
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
        IdentityReductionVisitor => (RicisLegacyTextResources.Get("runtime.legacy.2ba1ae598727"), "ID-01 / L1: F/F → 1"),
        PolarTrigVisitor => (RicisLegacyTextResources.Get("runtime.legacy.c4b3cd72e20b"), RicisLegacyTextResources.Get("runtime.legacy.f86329c58d5c")),
        AlgebraicReductionVisitor => (RicisLegacyTextResources.Get("runtime.legacy.2c89204e4fb5"), RicisLegacyTextResources.Get("runtime.legacy.2e4fce67051c")),
        LogicalReductionVisitor => (RicisLegacyTextResources.Get("runtime.legacy.377505616fe6"), RicisLegacyTextResources.Get("runtime.legacy.a80dc9b99e16")),
        LimitBridgeVisitor => (RicisLegacyTextResources.Get("runtime.legacy.acd7614cf864"), "LIM: F·0 → 0_F, F/0 → ∞_F"),
        RicisTransformVisitor => (RicisLegacyTextResources.Get("runtime.legacy.2dee41925e3c"), RicisLegacyTextResources.Get("runtime.legacy.de0dc11786eb")),
        TypeConsistencyVisitor => (RicisLegacyTextResources.Get("runtime.legacy.d8dc6b874a89"), RicisLegacyTextResources.Get("runtime.legacy.c39fdc38ba4a")),
        StandardOperationsVisitor => (RicisLegacyTextResources.Get("runtime.legacy.c69cbfe28c74"), "Z-01/Z-02, A5/A6/A7"),
        _ => (visitor.GetType().Name, RicisLegacyTextResources.Get("runtime.legacy.227cfe2cac84")),
    };
}
