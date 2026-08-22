using System.Linq.Expressions;
using System.Globalization;
using System.Numerics;
using System.Text;
using Ricis.Core.Expressions;
using Ricis.Core.Logging;
using Ricis.Core.Phases;
using Ricis.Core.Resources;
using Ricis.Core.Proofs;
using Ricis.Core.SpecialFunctions;

namespace Ricis.Core.Extensions;

/// <summary>
/// Builds auditable academic proof protocols for deferred RICIS expressions.
/// The proof is a symbolic derivation: supplied conditions and constraints are
/// recorded as hypotheses but are never compiled, evaluated, or treated as an
/// oracle of semantic truth.
/// </summary>
public static class RicisAcademicProofExtensions
{
    /// <summary>
    /// Derives a provable unary scalar expression through the normative RICIS
    /// pipeline and appends an academic protocol containing only effective
    /// RICIS transformations to <paramref name="proof"/>. The returned lambda is an independent derived
    /// expression; all conditions and constraints remain unevaluated expression
    /// trees in the written hypotheses.
    /// </summary>
    /// <typeparam name="T">
    /// The intrinsic or generic-math scalar type of the delayed RICIS function.
    /// </typeparam>
    /// <param name="conditions">
    /// Formal assumptions represented by unary boolean lambdas over the same
    /// scalar parameter as <paramref name="claim"/>.
    /// </param>
    /// <param name="constraints">
    /// Formal domain restrictions represented by unary boolean lambdas. They
    /// are recorded separately from assumptions to preserve academic meaning.
    /// </param>
    /// <param name="claim">
    /// The deferred scalar expression whose RICIS normal form is to be derived.
    /// </param>
    /// <param name="proof">
    /// An output buffer that receives the proof protocol. Its existing contents
    /// are preserved and a complete new proof section is appended.
    /// </param>
    /// <returns>The independently normalized <see cref="Expression{TDelegate}"/> representing the derived RICIS expression.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when a required collection, claim, or proof buffer is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when a hypothesis or the claim is not a unary lambda over T.
    /// </exception>
    public static Expression<Func<T, T>> Prove<T>(
        this IEnumerable<Expression<Func<T, bool>>> conditions,
        IEnumerable<Expression<Func<T, bool>>> constraints,
        Expression<Func<T, T>> claim,
        StringBuilder proof)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(proof);
        var derivation = DeriveUnaryProof(conditions, constraints, claim, log: null);
        AppendAcademicProtocol(
            proof,
            derivation.Conditions,
            derivation.Constraints,
            claim,
            derivation.Trace,
            derivation.Derived);
        return derivation.Derived;
    }

    /// <summary>
    /// Derives a unary scalar expression and publishes a structured event journal
    /// whose typed stage facades identify proof orchestration and every executed
    /// RICIS visitor. Expression inputs remain deferred and are never executed.
    /// </summary>
    public static Expression<Func<T, T>> Prove<T>(
        this IEnumerable<Expression<Func<T, bool>>> conditions,
        IEnumerable<Expression<Func<T, bool>>> constraints,
        Expression<Func<T, T>> claim,
        ILog<RicisProofOrchestrationStage> log)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(log);
        return DeriveUnaryProof(conditions, constraints, claim, log).Derived;
    }

    private static UnaryProofDerivation<T> DeriveUnaryProof<T>(
        IEnumerable<Expression<Func<T, bool>>> conditions,
        IEnumerable<Expression<Func<T, bool>>> constraints,
        Expression<Func<T, T>> claim,
        ILog<RicisProofOrchestrationStage> log)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(constraints);
        ArgumentNullException.ThrowIfNull(claim);

        var conditionList = conditions.ToList();
        var constraintList = constraints.ToList();
        ValidateHypotheses(conditionList, nameof(conditions));
        ValidateHypotheses(constraintList, nameof(constraints));
        ValidateClaim(claim);

        log?.Info(
            "RICIS_PROOF_START",
            RicisLegacyTextResources.Get("report.legacy.9c23e1f43fb1"),
            new Dictionary<string, string>
            {
                ["scalarType"] = typeof(T).FullName ?? typeof(T).Name,
                ["conditionCount"] = conditionList.Count.ToString(),
                ["constraintCount"] = constraintList.Count.ToString(),
                ["claim"] = claim.ToString(),
            });

        NumericConstants.Register<T>();
        var trace = new List<RicisPhaseTraceStep>();
        var derived = log is null
            ? RicisPhasePipeline.SimplifyWithTrace(claim, trace)
            : RicisPhasePipeline.SimplifyWithTraceAndLog(claim, trace, log);
        var typedDerived = derived as Expression<Func<T, T>>
            ?? throw new InvalidOperationException(
                string.Format(CultureInfo.CurrentUICulture, RicisLegacyTextResources.Get("report.legacy.e66a27f6139b"), typeof(T).Name));

        log?.Info(
            "RICIS_PROOF_COMPLETE",
            RicisLegacyTextResources.Get("report.legacy.efbf88fbbff0"),
            new Dictionary<string, string>
            {
                ["derived"] = typedDerived.ToString(),
                ["phaseAttemptCount"] = trace.Count.ToString(),
            });
        return new UnaryProofDerivation<T>(conditionList, constraintList, trace, typedDerived);
    }

    private sealed record UnaryProofDerivation<T>(
        IReadOnlyList<Expression<Func<T, bool>>> Conditions,
        IReadOnlyList<Expression<Func<T, bool>>> Constraints,
        IReadOnlyList<RicisPhaseTraceStep> Trace,
        Expression<Func<T, T>> Derived);

    private static RicisCheckedProofResult<T> CreateCheckedProofResult<T>(
        Expression<Func<T, T>> derived,
        Expression<Func<T, T>> expected,
        IReadOnlyList<Expression<Func<T, bool>>> conditions,
        IReadOnlyList<Expression<Func<T, bool>>> constraints)
        where T : INumber<T>
    {
        var normalizedExpected = RicisPhasePipeline.Simplify(expected) as Expression<Func<T, T>>
            ?? throw new InvalidOperationException(RicisLegacyTextResources.Get("report.legacy.88d28a2f88f2"));
        var reboundExpectedBody = new ParameterSubstitutionVisitor(
            normalizedExpected.Parameters[0], derived.Parameters[0]).Visit(normalizedExpected.Body)
            ?? throw new InvalidOperationException(RicisLegacyTextResources.Get("report.legacy.c11425633c16"));
        var reboundExpected = Expression.Lambda<Func<T, T>>(reboundExpectedBody, derived.Parameters);
        var verification = Expression.Lambda<Func<T, bool>>(
            Expression.Equal(derived.Body, reboundExpected.Body), derived.Parameters);
        return new RicisCheckedProofResult<T>(
            derived,
            reboundExpected,
            verification,
            derived.Body.AreEqual(reboundExpected.Body),
            conditions,
            constraints);
    }

    private static void AppendVerificationProtocol<T>(
        StringBuilder proof,
        RicisCheckedProofResult<T> result)
        where T : INumber<T>
    {
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.dbbc5e77313d"));
        proof.AppendLine($"- Derived: `{result.Derived}`");
        proof.AppendLine($"- Expected: `{result.Expected}`");
        proof.AppendLine($"- Verification: `{result.Verification}`");
        proof.AppendLine($"- Structural status: `{result.IsVerified}`");
    }

    /// <summary>
    /// Proves a real unary lambda claim and structurally checks it against a real
    /// expected lambda expression. Conditions and constraints remain expression
    /// trees and are never compiled or evaluated.
    /// </summary>
    /// <typeparam name="T">The intrinsic or generic-math scalar type.</typeparam>
    /// <param name="conditions">Actual unary lambda assumptions.</param>
    /// <param name="constraints">Actual unary lambda domain constraints.</param>
    /// <param name="claim">The actual deferred claim expression.</param>
    /// <param name="expected">The actual deferred expression expected after RICIS normalization.</param>
    /// <param name="proof">The buffer receiving the derivation and verification record.</param>
    /// <returns>A result containing derived, expected and verification expression trees.</returns>
    public static RicisCheckedProofResult<T> Prove<T>(
        this IEnumerable<Expression<Func<T, bool>>> conditions,
        IEnumerable<Expression<Func<T, bool>>> constraints,
        Expression<Func<T, T>> claim,
        Expression<Func<T, T>> expected,
        StringBuilder proof)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(expected);
        var conditionList = conditions?.ToList() ?? throw new ArgumentNullException(nameof(conditions));
        var constraintList = constraints?.ToList() ?? throw new ArgumentNullException(nameof(constraints));
        var derived = conditionList.Prove(constraintList, claim, proof);
        var result = CreateCheckedProofResult(derived, expected, conditionList, constraintList);
        AppendVerificationProtocol(proof, result);
        return result;
    }

    /// <summary>
    /// Backward-compatible named alias for the extended <see cref="Prove{T}(IEnumerable{Expression{Func{T, Boolean}}}, IEnumerable{Expression{Func{T, Boolean}}}, Expression{Func{T, T}}, Expression{Func{T, T}}, StringBuilder)"/> overload.
    /// </summary>
    public static RicisCheckedProofResult<T> ProveChecked<T>(
        this IEnumerable<Expression<Func<T, bool>>> conditions,
        IEnumerable<Expression<Func<T, bool>>> constraints,
        Expression<Func<T, T>> claim,
        Expression<Func<T, T>> expected,
        StringBuilder proof)
        where T : INumber<T> => conditions.Prove(constraints, claim, expected, proof);

    /// <summary>
    /// Builds an academic proof document from real lambda hypotheses and a real
    /// expected expression, preserving the structural verification expression.
    /// </summary>
    public static RicisCheckedProofResult<T> ProveDocument<T>(
        this IEnumerable<Expression<Func<T, bool>>> conditions,
        IEnumerable<Expression<Func<T, bool>>> constraints,
        Expression<Func<T, T>> claim,
        Expression<Func<T, T>> expected,
        RicisProofDocumentProfile profile,
        StringBuilder document)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(document);
        var derivation = new StringBuilder();
        var result = conditions.Prove(constraints, claim, expected, derivation);
        AppendProofDocument(document, profile, derivation, result.Derived);
        return result;
    }

    /// <summary>
    /// Backward-compatible named alias for the extended <see cref="ProveDocument{T}(IEnumerable{Expression{Func{T, Boolean}}}, IEnumerable{Expression{Func{T, Boolean}}}, Expression{Func{T, T}}, Expression{Func{T, T}}, RicisProofDocumentProfile, StringBuilder)"/> overload.
    /// </summary>
    public static RicisCheckedProofResult<T> ProveDocumentChecked<T>(
        this IEnumerable<Expression<Func<T, bool>>> conditions,
        IEnumerable<Expression<Func<T, bool>>> constraints,
        Expression<Func<T, T>> claim,
        Expression<Func<T, T>> expected,
        RicisProofDocumentProfile profile,
        StringBuilder document)
        where T : INumber<T> => conditions.ProveDocument(constraints, claim, expected, profile, document);

    /// <summary>
    /// Runs a checked unary proof exactly once with an injected typed journal and
    /// renders several formats from the same node-to-root derivation. The real
    /// expected lambda is normalized structurally after the single claim pass;
    /// no hypothesis is evaluated and no second claim derivation is performed.
    /// </summary>
    public static RicisCheckedProofArtifacts<T> ProveDocumentsCheckedWithLog<T>(
        this IEnumerable<Expression<Func<T, bool>>> conditions,
        IEnumerable<Expression<Func<T, bool>>> constraints,
        Expression<Func<T, T>> claim,
        Expression<Func<T, T>> expected,
        RicisProofDocumentProfile profile,
        IEnumerable<RicisProofDocumentFormat> formats,
        ILog<RicisProofOrchestrationStage> log)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(formats);
        ArgumentNullException.ThrowIfNull(log);
        var requestedFormats = formats.Distinct().ToArray();
        if (requestedFormats.Length == 0)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("report.legacy.3d8cd36cf181"), nameof(formats));
        }

        foreach (var format in requestedFormats)
        {
            ValidateDocumentFormat(format);
        }

        var derivationResult = DeriveUnaryProof(conditions, constraints, claim, log);
        var result = CreateCheckedProofResult(
            derivationResult.Derived,
            expected,
            derivationResult.Conditions,
            derivationResult.Constraints);
        log.Info(
            "RICIS_PROOF_VERIFICATION",
            RicisLegacyTextResources.Get("report.legacy.ccfa1fe2b221"),
            new Dictionary<string, string>
            {
                ["verification"] = result.Verification.ToString(),
                ["isVerified"] = result.IsVerified.ToString(),
            });

        var derivation = new StringBuilder();
        AppendAcademicProtocol(
            derivation,
            derivationResult.Conditions,
            derivationResult.Constraints,
            claim,
            derivationResult.Trace,
            derivationResult.Derived);
        AppendVerificationProtocol(derivation, result);
        AppendTypedProofLog(derivation, log.Snapshot());

        var documents = new Dictionary<RicisProofDocumentFormat, string>();
        foreach (var format in requestedFormats)
        {
            var document = new StringBuilder();
            AppendFormattedProofDocument(
                document,
                profile,
                derivation.ToString(),
                result.Derived,
                ResolveDocumentConstructor(format),
                static text => text);
            documents.Add(format, document.ToString());
        }

        return new RicisCheckedProofArtifacts<T>(result, log.Snapshot(), documents);
    }

    /// <summary>
    /// Derives a unary scalar expression and writes an academic proof document
    /// with an explicit finite or conditional proof scope. The document records
    /// the supplied hypotheses without evaluating them and embeds the effective
    /// RICIS derivation produced by <see cref="Prove{T}(IEnumerable{Expression{Func{T, Boolean}}}, IEnumerable{Expression{Func{T, Boolean}}}, Expression{Func{T, T}}, StringBuilder)"/>.
    /// </summary>
    /// <typeparam name="T">The intrinsic or generic-math scalar type of the delayed expression.</typeparam>
    /// <param name="conditions">The formal unary assumptions.</param>
    /// <param name="constraints">The formal unary domain restrictions.</param>
    /// <param name="claim">The delayed scalar expression to derive.</param>
    /// <param name="profile">Academic metadata, stated premises, and proof boundaries.</param>
    /// <param name="document">The buffer receiving the complete Markdown proof document.</param>
    /// <returns>The independent expression tree returned by the underlying symbolic derivation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> or <paramref name="document"/> is null.</exception>
    public static Expression<Func<T, T>> ProveDocument<T>(
        this IEnumerable<Expression<Func<T, bool>>> conditions,
        IEnumerable<Expression<Func<T, bool>>> constraints,
        Expression<Func<T, T>> claim,
        RicisProofDocumentProfile profile,
        StringBuilder document)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(document);

        return conditions.ProveDocument(
            constraints,
            claim,
            profile,
            RicisProofDocumentFormat.Academic,
            static text => text,
            document);
    }

    /// <summary>
    /// Derives a unary scalar claim once through the existing RICIS proof engine
    /// and renders the resulting proof with the selected document template.
    /// The <paramref name="documentTextTransform"/> callback receives the complete
    /// template output and returns the exact text appended to <paramref name="document"/>.
    /// It may add organisation-specific lines or apply a final textual transform,
    /// but it cannot change the proof expression, proof scope, or RICIS trace.
    /// </summary>
    /// <typeparam name="T">The intrinsic or generic-math scalar type of the delayed expression.</typeparam>
    /// <param name="conditions">Formal unary assumptions.</param>
    /// <param name="constraints">Formal unary domain restrictions.</param>
    /// <param name="claim">The delayed scalar expression to derive.</param>
    /// <param name="profile">Proof metadata, stated premises, and limitations.</param>
    /// <param name="format">The requested Log, Academic, Lean scaffold, or Json template.</param>
    /// <param name="documentTextTransform">A non-null final-text transformation callback.</param>
    /// <param name="document">The buffer receiving the rendered proof document.</param>
    /// <returns>The independently derived RICIS expression.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the profile, callback, or document is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="format"/> is unknown.</exception>
    public static Expression<Func<T, T>> ProveDocument<T>(
        this IEnumerable<Expression<Func<T, bool>>> conditions,
        IEnumerable<Expression<Func<T, bool>>> constraints,
        Expression<Func<T, T>> claim,
        RicisProofDocumentProfile profile,
        RicisProofDocumentFormat format,
        Func<string, string> documentTextTransform,
        StringBuilder document)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(documentTextTransform);
        ArgumentNullException.ThrowIfNull(document);
        var documentConstructor = ResolveDocumentConstructor(format);

        var derivation = new StringBuilder();
        var derived = conditions.Prove(constraints, claim, derivation);
        AppendFormattedProofDocument(document, profile, derivation.ToString(), derived, documentConstructor, documentTextTransform);
        return derived;
    }

    /// <summary>
    /// Derives a unary scalar claim once while publishing the complete typed
    /// proof journal to an injected log. The selected document constructor is
    /// resolved before derivation, then receives the same node-to-root protocol
    /// and typed event sequence without a second proof pass.
    /// </summary>
    public static Expression<Func<T, T>> ProveDocumentWithLog<T>(
        this IEnumerable<Expression<Func<T, bool>>> conditions,
        IEnumerable<Expression<Func<T, bool>>> constraints,
        Expression<Func<T, T>> claim,
        RicisProofDocumentProfile profile,
        RicisProofDocumentFormat format,
        ILog<RicisProofOrchestrationStage> log,
        StringBuilder document)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(document);
        var documentConstructor = ResolveDocumentConstructor(format);

        var derivationResult = DeriveUnaryProof(conditions, constraints, claim, log);
        var derivation = new StringBuilder();
        AppendAcademicProtocol(
            derivation,
            derivationResult.Conditions,
            derivationResult.Constraints,
            claim,
            derivationResult.Trace,
            derivationResult.Derived);
        AppendTypedProofLog(derivation, log.Snapshot());
        AppendFormattedProofDocument(
            document,
            profile,
            derivation.ToString(),
            derivationResult.Derived,
            documentConstructor,
            static text => text);
        return derivationResult.Derived;
    }

    /// <summary>
    /// Derives a unary scalar claim and renders it with the selected proof format
    /// without applying a caller-specific text transformation.
    /// </summary>
    public static Expression<Func<T, T>> ProveDocument<T>(
        this IEnumerable<Expression<Func<T, bool>>> conditions,
        IEnumerable<Expression<Func<T, bool>>> constraints,
        Expression<Func<T, T>> claim,
        RicisProofDocumentProfile profile,
        RicisProofDocumentFormat format,
        StringBuilder document)
        where T : INumber<T> => conditions.ProveDocument(
            constraints,
            claim,
            profile,
            format,
            static text => text,
            document);

    /// <summary>
    /// Solves a supported two-variable expression system once with an optional typed
    /// journal and renders the same derivation through the selected document format.
    /// The journal is appended to the derivation before the document factory runs,
    /// so the LaTeX/JSON/Log document contains both system steps and internal events.
    /// </summary>
    public static Expression<Func<double, double, bool>> ProveDocumentWithLog(
        this IEnumerable<Expression<Func<double, double, bool>>> equations,
        IEnumerable<Expression<Func<double, double, bool>>> constraints,
        Expression<Func<double, double, bool>> claim,
        RicisProofDocumentProfile profile,
        RicisProofDocumentFormat format,
        ILog<RicisProofOrchestrationStage> log,
        StringBuilder document)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(log);
        ArgumentNullException.ThrowIfNull(document);
        var documentConstructor = ResolveDocumentConstructor(format);
        var derivation = new StringBuilder();
        var derived = equations.Prove(constraints, claim, derivation, log);
        AppendTypedProofLog(derivation, log.Snapshot());
        AppendFormattedProofDocument(
            document,
            profile,
            derivation.ToString(),
            derived,
            documentConstructor,
            static text => text);
        return derived;
    }

    /// <summary>
    /// Specialized overload: derives a stated coordinate of a supported two-variable
    /// linear system once and renders the existing symbolic protocol through the
    /// selected template. This overload does not restrict the universal generic
    /// RICIS proof/document overloads for deferred F expressions.
    /// </summary>
    /// <param name="equations">The two formal linear equations.</param>
    /// <param name="constraints">The formal binary domain restrictions.</param>
    /// <param name="claim">The coordinate equality to derive.</param>
    /// <param name="profile">Proof metadata, stated premises, and limitations.</param>
    /// <param name="format">The requested output template.</param>
    /// <param name="documentTextTransform">A non-null final-text transformation callback.</param>
    /// <param name="document">The buffer receiving the rendered proof document.</param>
    /// <returns>The independently derived coordinate equality.</returns>
    public static Expression<Func<double, double, bool>> ProveDocument(
        this IEnumerable<Expression<Func<double, double, bool>>> equations,
        IEnumerable<Expression<Func<double, double, bool>>> constraints,
        Expression<Func<double, double, bool>> claim,
        RicisProofDocumentProfile profile,
        RicisProofDocumentFormat format,
        Func<string, string> documentTextTransform,
        StringBuilder document)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(documentTextTransform);
        ArgumentNullException.ThrowIfNull(document);
        var documentConstructor = ResolveDocumentConstructor(format);

        var derivation = new StringBuilder();
        var derived = equations.Prove(constraints, claim, derivation);
        AppendFormattedProofDocument(document, profile, derivation.ToString(), derived, documentConstructor, documentTextTransform);
        return derived;
    }

    /// <summary>
    /// Derives a supported two-variable linear system and renders it with the
    /// selected proof format without a caller-specific text transformation.
    /// </summary>
    public static Expression<Func<double, double, bool>> ProveDocument(
        this IEnumerable<Expression<Func<double, double, bool>>> equations,
        IEnumerable<Expression<Func<double, double, bool>>> constraints,
        Expression<Func<double, double, bool>> claim,
        RicisProofDocumentProfile profile,
        RicisProofDocumentFormat format,
        StringBuilder document) => equations.ProveDocument(
            constraints,
            claim,
            profile,
            format,
            static text => text,
            document);

    /// <summary>
    /// Derives a stated coordinate of a supported two-variable linear system and
    /// writes an academic proof document with an explicit finite or conditional
    /// proof scope. The system and restrictions remain unevaluated expression trees.
    /// </summary>
    /// <param name="equations">The two formal linear equations.</param>
    /// <param name="constraints">The formal binary domain restrictions.</param>
    /// <param name="claim">The coordinate equality to derive.</param>
    /// <param name="profile">Academic metadata, stated premises, and proof boundaries.</param>
    /// <param name="document">The buffer receiving the complete Markdown proof document.</param>
    /// <returns>The independent coordinate equality returned by symbolic elimination.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="profile"/> or <paramref name="document"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the linear system or its claim is unsupported or contradictory.</exception>
    public static Expression<Func<double, double, bool>> ProveDocument(
        this IEnumerable<Expression<Func<double, double, bool>>> equations,
        IEnumerable<Expression<Func<double, double, bool>>> constraints,
        Expression<Func<double, double, bool>> claim,
        RicisProofDocumentProfile profile,
        StringBuilder document)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(document);

        return equations.ProveDocument(
            constraints,
            claim,
            profile,
            RicisProofDocumentFormat.Academic,
            static text => text,
            document);
    }

    /// <summary>
    /// Applies the normative RICIS type-identity chain ID-01 through ID-06 to a
    /// formal reflected pair and writes a fully named academic proof document.
    /// The method constructs the two derived linear consequences
    /// <c>sigma+mirrorSigma=1</c> and <c>sigma-mirrorSigma=0</c> from the
    /// registered identity rules, then delegates exact elimination to the
    /// ordinary two-variable proof engine. No supplied constraint is compiled.
    /// </summary>
    /// <param name="constraints">Optional binary domain restrictions over the formal reflected pair.</param>
    /// <param name="claim">The coordinate equality to derive, normally <c>sigma=1/2</c>.</param>
    /// <param name="document">The buffer receiving the complete named proof document.</param>
    /// <returns>An independent derived equality expression for the claimed coordinate.</returns>
    /// <exception cref="ArgumentException">Thrown when the claim is not a supported exact coordinate consequence of the ID chain.</exception>
    public static Expression<Func<double, double, bool>> ProveTypeIdentityCriticalLine(
        this IEnumerable<Expression<Func<double, double, bool>>> constraints,
        Expression<Func<double, double, bool>> claim,
        StringBuilder document)
    {
        ArgumentNullException.ThrowIfNull(constraints);
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(document);
        var constraintList = constraints.ToList();
        ValidateBinaryHypotheses(constraintList, nameof(constraints));
        if (claim.Parameters.Count != 2)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("report.legacy.58ccb6d5ca04"), nameof(claim));
        }

        var sigma = claim.Parameters[0];
        var mirrorSigma = claim.Parameters[1];
        var equations = new Expression<Func<double, double, bool>>[]
        {
            Expression.Lambda<Func<double, double, bool>>(
                Expression.Equal(Expression.Add(sigma, mirrorSigma), Expression.Constant(1.0)),
                sigma,
                mirrorSigma),
            Expression.Lambda<Func<double, double, bool>>(
                Expression.Equal(Expression.Subtract(sigma, mirrorSigma), Expression.Constant(0.0)),
                sigma,
                mirrorSigma),
        };
        var profile = CreateTypeIdentityProfile(sigma.Name ?? "sigma", mirrorSigma.Name ?? "mirrorSigma", claim);
        return equations.ProveDocument(constraintList, claim, profile, document);
    }

    /// <summary>
    /// Specialized overload: derives a stated coordinate of a two-variable linear
    /// system through symbolic elimination and writes an academic proof protocol to
    /// <paramref name="proof"/>. The supported system contains exactly two
    /// independent equations in the forms <c>x+y=c</c>, <c>x-y=c</c>,
    /// <c>y+x=c</c>, or <c>y-x=c</c>, together with optional two-variable
    /// boolean domain constraints. No equation or constraint is compiled.
    /// </summary>
    /// <param name="equations">The two formal equations of this specialized linear-system overload; generic RICIS proof APIs accept broader deferred F expressions.</param>
    /// <param name="constraints">Optional domain constraints over the same pair of variables.</param>
    /// <param name="claim">A coordinate claim of the form <c>x=c</c> or <c>y=c</c>.</param>
    /// <param name="proof">The output buffer for the academic derivation.</param>
    /// <param name="log">Optional typed journal receiving system solver events.</param>
    /// <returns>An independent derived equality expression for the proved coordinate.</returns>
    /// <exception cref="ArgumentException">Thrown when the system is unsupported, degenerate, non-finite, overflows its finite double derivation, or the claim contradicts its symbolic solution.</exception>
    public static Expression<Func<double, double, bool>> Prove(
        this IEnumerable<Expression<Func<double, double, bool>>> equations,
        IEnumerable<Expression<Func<double, double, bool>>> constraints,
        Expression<Func<double, double, bool>> claim,
        StringBuilder proof,
        ILog<RicisProofOrchestrationStage> log = null)
    {
        ArgumentNullException.ThrowIfNull(equations);
        ArgumentNullException.ThrowIfNull(constraints);
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(proof);

        var equationList = equations.ToList();
        var constraintList = constraints.ToList();
        log?.Info(
            "RICIS_SYSTEM_START",
            RicisLegacyTextResources.Get("report.legacy.38c54bb83889"),
            new Dictionary<string, string>
            {
                ["equationCount"] = equationList.Count.ToString(),
                ["constraintCount"] = constraintList.Count.ToString(),
                ["claim"] = claim.ToString(),
            });
        if (equationList.Count != 2)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("report.legacy.879f604879a7"), nameof(equations));
        }

        ValidateBinaryHypotheses(equationList, nameof(equations));
        ValidateBinaryHypotheses(constraintList, nameof(constraints));
        for (var constraintIndex = 0; constraintIndex < constraintList.Count; constraintIndex++)
        {
            var constraint = constraintList[constraintIndex];
            var normalizedConstraint = log is null
                ? constraint
                : RicisPhasePipeline.Simplify(constraint);
            log?.For<BinarySystemNormalizationStage>().Trace(
                "RICIS_SYSTEM_CONSTRAINT_NORMALIZATION",
                RicisLegacyTextResources.Get("report.legacy.928af9b8a874"),
                constraint.ToString(),
                normalizedConstraint.ToString(),
                new Dictionary<string, string>
                {
                    ["constraintIndex"] = constraintIndex.ToString(),
                    ["ruleFamily"] = "ID-01 / SP2 / A1-A4",
                    ["cancellationRequested"] = bool.TrueString,
                });
        }
        var first = ReadSupportedLinearEquation(equationList[0], nameof(equations));
        var second = ReadSupportedLinearEquation(equationList[1], nameof(equations));
        var determinant = (first.X * second.Y) - (second.X * first.Y);
        log?.Info(
            "RICIS_SYSTEM_COEFFICIENTS",
            RicisLegacyTextResources.Get("report.legacy.05fdb82f3644"),
            new Dictionary<string, string>
            {
                ["first"] = $"{first.X}x + {first.Y}y = {first.Constant}",
                ["second"] = $"{second.X}x + {second.Y}y = {second.Constant}",
                ["determinant"] = determinant.ToString("G17"),
            });
        if (determinant == 0.0)
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("report.legacy.5e5e93b99528"), nameof(equations));
        }

        var solutionX = ((first.Constant * second.Y) - (second.Constant * first.Y)) / determinant;
        var solutionY = ((first.X * second.Constant) - (second.X * first.Constant)) / determinant;
        if (!double.IsFinite(solutionX) || !double.IsFinite(solutionY))
        {
            throw new ArgumentException(
                RicisLegacyTextResources.Get("report.legacy.8439c44ac10e"),
                nameof(equations));
        }

        var (coordinate, claimedValue) = ReadCoordinateClaim(claim);
        if (!double.IsFinite(claimedValue))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("report.legacy.2e3cf2082bef"), nameof(claim));
        }

        var provenValue = coordinate == 0 ? solutionX : solutionY;
        log?.Trace(
            "RICIS_SYSTEM_ELIMINATION",
            RicisLegacyTextResources.Get("report.legacy.02f7e69ab400"),
            claim.ToString(),
            $"x = {solutionX:G17}; y = {solutionY:G17}",
            new Dictionary<string, string>
            {
                ["coordinate"] = coordinate == 0 ? "x" : "y",
                ["claimedValue"] = claimedValue.ToString("G17"),
                ["provenValue"] = provenValue.ToString("G17"),
            });
        if (claimedValue != provenValue)
        {
            throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, RicisLegacyTextResources.Get("report.legacy.dfd6e531edbe"), claim, coordinate == 0 ? "x" : "y", provenValue.ToString("G17", CultureInfo.InvariantCulture)),
                nameof(claim));
        }

        var hasExactIntegralSystem = TrySolveIntegralSystem(first, second, out var exactX, out var exactY);
        var solutionXExpression = hasExactIntegralSystem
            ? BuildExactRationalExpression(exactX)
            : Expression.Constant(solutionX);
        var solutionYExpression = hasExactIntegralSystem
            ? BuildExactRationalExpression(exactY)
            : Expression.Constant(solutionY);
        var parameter = claim.Parameters[coordinate];
        var derived = Expression.Lambda<Func<double, double, bool>>(
            Expression.Equal(parameter, coordinate == 0 ? solutionXExpression : solutionYExpression),
            claim.Parameters);
        AppendLinearSystemProtocol(
            proof,
            equationList,
            constraintList,
            claim,
            first,
            second,
            solutionXExpression,
            solutionYExpression,
            coordinate,
            derived);
        log?.Info(
            "RICIS_SYSTEM_COMPLETE",
            RicisLegacyTextResources.Get("report.legacy.5b1abc61d696"),
            new Dictionary<string, string>
            {
                ["derived"] = derived.ToString(),
                ["coordinate"] = coordinate == 0 ? "x" : "y",
            });
        return derived;
    }

    private static RicisProofDocumentProfile CreateTypeIdentityProfile(
        string sigmaName,
        string mirrorSigmaName,
        Expression<Func<double, double, bool>> claim)
    {
        return new RicisProofDocumentProfile(
            title: RicisLegacyTextResources.Get("report.legacy.ce97b9999b9e"),
            scope: RicisProofScope.FiniteDerivation,
            @abstract: RicisLegacyTextResources.Get("report.legacy.3cfe65ac2002"),
            theorem: string.Format(CultureInfo.InvariantCulture, RicisLegacyTextResources.Get("report.legacy.b43d92d7a142"), sigmaName, mirrorSigmaName, claim),
            definitions:
            [
                string.Format(CultureInfo.InvariantCulture, RicisLegacyTextResources.Get("report.legacy.09800ce17554"), sigmaName, mirrorSigmaName),
                RicisLegacyTextResources.Get("report.legacy.00b58be4fca6"),
            ],
            normativeSteps:
            [
                new RicisProofAxiomStep("ID-01", RicisLegacyTextResources.Get("report.legacy.d5b2d675b4c1"), string.Format(CultureInfo.InvariantCulture, RicisLegacyTextResources.Get("report.legacy.3f6d434b0b7f"), sigmaName, mirrorSigmaName)),
                new RicisProofAxiomStep("ID-02", RicisLegacyTextResources.Get("report.legacy.ce8b068cbd84"), string.Format(CultureInfo.InvariantCulture, RicisLegacyTextResources.Get("report.legacy.4c39a532d29a"), sigmaName, sigmaName, sigmaName, mirrorSigmaName)),
                new RicisProofAxiomStep("ID-03", RicisLegacyTextResources.Get("report.legacy.413aa8a8d84d"), string.Format(CultureInfo.InvariantCulture, RicisLegacyTextResources.Get("report.legacy.f4b04e6353d7"), sigmaName, mirrorSigmaName, sigmaName, mirrorSigmaName)),
                new RicisProofAxiomStep("ID-04", RicisLegacyTextResources.Get("report.legacy.fa95ff4a3c6c"), string.Format(CultureInfo.InvariantCulture, RicisLegacyTextResources.Get("report.legacy.561639ada97d"), sigmaName, mirrorSigmaName, sigmaName, mirrorSigmaName)),
                new RicisProofAxiomStep("ID-05", RicisLegacyTextResources.Get("report.legacy.80ba760f3ffc"), string.Format(CultureInfo.InvariantCulture, RicisLegacyTextResources.Get("report.legacy.6814b74407cd"), sigmaName)),
                new RicisProofAxiomStep("ID-06", RicisLegacyTextResources.Get("report.legacy.1f359d889f7d"), string.Format(CultureInfo.InvariantCulture, RicisLegacyTextResources.Get("report.legacy.6e9ce814d451"), sigmaName, sigmaName)),
            ],
            limitations:
            [
                RicisLegacyTextResources.Get("report.legacy.abcc2d804a4f"),
            ]);
    }

    private static void AppendProofDocument(
        StringBuilder document,
        RicisProofDocumentProfile profile,
        StringBuilder derivation,
        LambdaExpression derived)
    {
        if (document.Length > 0 && document[^1] != '\n')
        {
            document.AppendLine();
        }

        document.Append("# ").AppendLine(profile.Title);
        document.AppendLine();
        document.AppendLine(RicisLegacyTextResources.Get("report.legacy.01fa35f47fb7"));
        document.AppendLine(profile.Abstract);
        document.AppendLine();
        document.AppendLine(RicisLegacyTextResources.Get("report.legacy.bdd154f2ccd2"));
        document.AppendLine(profile.Scope switch
        {
            RicisProofScope.FiniteDerivation =>
                RicisLegacyTextResources.Get("report.legacy.49d4ecb68e39"),
            RicisProofScope.ConditionalTheorem =>
                RicisLegacyTextResources.Get("report.legacy.05b5047d0042"),
            _ => throw new ArgumentOutOfRangeException(nameof(profile)),
        });
        document.AppendLine();
        AppendDocumentSection(document, RicisLegacyTextResources.Get("report.legacy.cf6c6be0e097"), profile.Definitions, RicisLegacyTextResources.Get("report.legacy.12eac73cd080"));
        AppendDocumentSection(document, RicisLegacyTextResources.Get("report.legacy.31e705f71bdb"), profile.Axioms, RicisLegacyTextResources.Get("report.legacy.f798f6c28fc5"));
        AppendNormativeAxiomSteps(document, profile.NormativeSteps);
        document.AppendLine(RicisLegacyTextResources.Get("report.legacy.4576c2f4a7a9"));
        document.AppendLine(profile.Theorem);
        document.AppendLine();
        document.AppendLine(RicisLegacyTextResources.Get("report.legacy.3dd68a120ef7"));
        document.AppendLine(RicisLegacyTextResources.Get("report.legacy.c7923dd99729"));
        document.AppendLine();
        AppendNestedMarkdown(document, derivation);
        document.AppendLine();
        document.AppendLine(RicisLegacyTextResources.Get("report.legacy.4a0292ce58db"));
        document.Append(RicisLegacyTextResources.Get("report.legacy.ee293a489b52")).Append(derived).AppendLine("`.");
        document.AppendLine();
        AppendDocumentSection(
            document,
            RicisLegacyTextResources.Get("report.legacy.64b5128c813a"),
            profile.Limitations,
            RicisLegacyTextResources.Get("report.legacy.8cbb9d46f586"));
    }

    private static void AppendFormattedProofDocument(
        StringBuilder document,
        RicisProofDocumentProfile profile,
        string derivation,
        LambdaExpression derived,
        Func<RicisProofDocumentProfile, string, LambdaExpression, string> documentConstructor,
        Func<string, string> documentTextTransform)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(derivation);
        ArgumentNullException.ThrowIfNull(derived);
        ArgumentNullException.ThrowIfNull(documentConstructor);
        ArgumentNullException.ThrowIfNull(documentTextTransform);

        var rendered = documentConstructor(profile, derivation, derived);
        var transformed = documentTextTransform(rendered)
            ?? throw new InvalidOperationException(RicisLegacyTextResources.Get("report.legacy.bec8cf03e979"));
        if (document.Length > 0 && document[^1] != '\n')
        {
            document.AppendLine();
        }

        document.Append(transformed);
    }

    private static Func<RicisProofDocumentProfile, string, LambdaExpression, string> ResolveDocumentConstructor(
        RicisProofDocumentFormat format)
    {
        ValidateDocumentFormat(format);
        return format == RicisProofDocumentFormat.Academic
            ? static (profile, derivation, derived) =>
            {
                var academic = new StringBuilder();
                AppendProofDocument(academic, profile, new StringBuilder(derivation), derived);
                return academic.ToString();
            }
            : RicisProofDocumentTemplates.ResolveFactory(format);
    }

    private static void ValidateDocumentFormat(RicisProofDocumentFormat format)
    {
        if (!Enum.IsDefined(format))
        {
            throw new ArgumentOutOfRangeException(nameof(format), format, RicisLegacyTextResources.Get("report.legacy.c35280c79f2e"));
        }
    }

    private static void AppendNormativeAxiomSteps(
        StringBuilder document,
        IReadOnlyList<RicisProofAxiomStep> steps)
    {
        document.AppendLine(RicisLegacyTextResources.Get("report.legacy.cc581791e38c"));
        if (steps.Count == 0)
        {
            document.AppendLine(RicisLegacyTextResources.Get("report.legacy.f00f3069f999"));
        }
        else
        {
            for (var index = 0; index < steps.Count; index++)
            {
                var step = steps[index];
                document.Append(RicisLegacyTextResources.Get("report.legacy.d2635f2fd366")).Append(index + 1).Append(": ")
                    .Append(step.RuleId).Append(" — ").AppendLine(step.Title);
                document.Append(RicisLegacyTextResources.Get("report.legacy.db4daaeabee8")).AppendLine(step.Statement);
            }
        }

        document.AppendLine();
    }

    private static void AppendNestedMarkdown(StringBuilder document, StringBuilder derivation)
    {
        var lines = derivation.ToString().Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith('#'))
            {
                document.Append("##");
            }

            document.AppendLine(line);
        }
    }

    private static void AppendDocumentSection(
        StringBuilder document,
        string title,
        IReadOnlyList<string> lines,
        string emptyMessage)
    {
        document.Append("## ").AppendLine(title);
        if (lines.Count == 0)
        {
            document.AppendLine(emptyMessage);
        }
        else
        {
            for (var index = 0; index < lines.Count; index++)
            {
                document.Append(index + 1).Append(". ").AppendLine(lines[index]);
            }
        }

        document.AppendLine();
    }

    private static void ValidateBinaryHypotheses(
        IEnumerable<Expression<Func<double, double, bool>>> hypotheses,
        string parameterName)
    {
        foreach (var hypothesis in hypotheses)
        {
            if (hypothesis is null || hypothesis.Parameters.Count != 2 ||
                hypothesis.Parameters[0].Type != typeof(double) ||
                hypothesis.Parameters[1].Type != typeof(double) ||
                hypothesis.ReturnType != typeof(bool))
            {
                throw new ArgumentException(
                    RicisLegacyTextResources.Get("report.legacy.a072af8d538f"),
                    parameterName);
            }
        }
    }

    private static LinearEquation ReadSupportedLinearEquation(
        Expression<Func<double, double, bool>> equation,
        string parameterName)
    {
        if (equation.Body is not BinaryExpression { NodeType: ExpressionType.Equal, Left: var left, Right: ConstantExpression { Value: double constant } } ||
            left is not BinaryExpression { NodeType: var operation, Left: var firstTerm, Right: var secondTerm } ||
            operation is not ExpressionType.Add and not ExpressionType.Subtract)
        {
            throw new ArgumentException(
                RicisLegacyTextResources.Get("report.legacy.835fe2e58280"),
                parameterName);
        }

        var firstIndex = ParameterIndex(firstTerm, equation.Parameters);
        var secondIndex = ParameterIndex(secondTerm, equation.Parameters);
        if (firstIndex < 0 || secondIndex < 0 || firstIndex == secondIndex)
        {
            throw new ArgumentException(
                RicisLegacyTextResources.Get("report.legacy.901595de1187"),
                parameterName);
        }

        if (!double.IsFinite(constant))
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("report.legacy.d0358209b12e"), parameterName);
        }

        var firstCoefficient = 1.0;
        var secondCoefficient = operation == ExpressionType.Add ? 1.0 : -1.0;
        var xCoefficient = firstIndex == 0 ? firstCoefficient : secondCoefficient;
        var yCoefficient = firstIndex == 1 ? firstCoefficient : secondCoefficient;
        return new LinearEquation(xCoefficient, yCoefficient, constant);
    }

    private static int ParameterIndex(Expression expression, IReadOnlyList<ParameterExpression> parameters) =>
        expression is ParameterExpression parameter && ReferenceEquals(parameter, parameters[0]) ? 0 :
        expression is ParameterExpression second && ReferenceEquals(second, parameters[1]) ? 1 : -1;

    private static (int Coordinate, double Value) ReadCoordinateClaim(Expression<Func<double, double, bool>> claim)
    {
        if (claim.Body is not BinaryExpression { NodeType: ExpressionType.Equal, Left: var left, Right: var right })
        {
            throw new ArgumentException(RicisLegacyTextResources.Get("report.legacy.7ee57d1e6e7f"), nameof(claim));
        }

        var leftIndex = ParameterIndex(left, claim.Parameters);
        if (leftIndex >= 0 && TryReadFiniteDoubleScalar(right, out var rightValue))
        {
            return (leftIndex, rightValue);
        }

        var rightIndex = ParameterIndex(right, claim.Parameters);
        if (rightIndex >= 0 && TryReadFiniteDoubleScalar(left, out var leftValue))
        {
            return (rightIndex, leftValue);
        }

        throw new ArgumentException(
            RicisLegacyTextResources.Get("report.legacy.da1252a7d1c3"),
            nameof(claim));
    }

    private static bool TryReadFiniteDoubleScalar(Expression expression, out double value)
    {
        if (expression is ConstantExpression { Value: double constant } && double.IsFinite(constant))
        {
            value = constant;
            return true;
        }

        if (expression is BinaryExpression { NodeType: ExpressionType.Divide, Left: var numerator, Right: var denominator } &&
            TryReadFiniteDoubleScalar(numerator, out var numeratorValue) &&
            TryReadFiniteDoubleScalar(denominator, out var denominatorValue) &&
            denominatorValue != 0.0)
        {
            value = numeratorValue / denominatorValue;
            return double.IsFinite(value);
        }

        value = 0.0;
        return false;
    }

    private static bool TrySolveIntegralSystem(
        LinearEquation first,
        LinearEquation second,
        out ExactRational x,
        out ExactRational y)
    {
        x = default;
        y = default;
        if (!TryReadIntegralDouble(first.Constant, out var firstConstant) ||
            !TryReadIntegralDouble(second.Constant, out var secondConstant))
        {
            return false;
        }

        var firstX = new BigInteger(first.X);
        var firstY = new BigInteger(first.Y);
        var secondX = new BigInteger(second.X);
        var secondY = new BigInteger(second.Y);
        var determinant = (firstX * secondY) - (secondX * firstY);
        if (determinant.IsZero)
        {
            return false;
        }

        x = ExactRational.Create(
            (firstConstant * secondY) - (secondConstant * firstY),
            determinant);
        y = ExactRational.Create(
            (firstX * secondConstant) - (secondX * firstConstant),
            determinant);
        return true;
    }

    private static bool TryReadIntegralDouble(double value, out BigInteger integer)
    {
        integer = BigInteger.Zero;
        if (!double.IsFinite(value) || value != Math.Truncate(value))
        {
            return false;
        }

        try
        {
            integer = new BigInteger(value);
            return (double)integer == value;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static Expression BuildExactRationalExpression(ExactRational value)
    {
        var numerator = Expression.Constant((double)value.Numerator);
        if (value.Denominator.IsOne)
        {
            return numerator;
        }

        return Expression.Divide(numerator, Expression.Constant((double)value.Denominator));
    }

    private static void AppendLinearSystemProtocol(
        StringBuilder proof,
        IReadOnlyList<Expression<Func<double, double, bool>>> equations,
        IReadOnlyList<Expression<Func<double, double, bool>>> constraints,
        Expression<Func<double, double, bool>> claim,
        LinearEquation first,
        LinearEquation second,
        Expression solutionXExpression,
        Expression solutionYExpression,
        int coordinate,
        Expression<Func<double, double, bool>> derived)
    {
        if (proof.Length > 0 && proof[^1] != '\n')
        {
            proof.AppendLine();
        }

        var x = claim.Parameters[0];
        var y = claim.Parameters[1];
        var xName = x.Name ?? "x";
        var yName = y.Name ?? "y";
        var eliminationCoefficient = (second.X * first.Y) - (first.X * second.Y);
        var eliminationConstant = (second.Constant * first.Y) - (first.Constant * second.Y);
        var combined = Expression.Lambda<Func<double, double, bool>>(
            Expression.Equal(
                Expression.Multiply(Expression.Constant(eliminationCoefficient), x),
                Expression.Constant(eliminationConstant)),
            x,
            y);
        var xResult = Expression.Lambda<Func<double, double, bool>>(
            Expression.Equal(x, solutionXExpression), x, y);
        var firstWithProofParameters = new ParameterSubstitutionVisitor(equations[0].Parameters[0], x)
            .Visit(equations[0].Body);
        firstWithProofParameters = new ParameterSubstitutionVisitor(equations[0].Parameters[1], y)
            .Visit(firstWithProofParameters);
        var substituted = Expression.Lambda<Func<double, double, bool>>(
            new ParameterSubstitutionVisitor(x, solutionXExpression).Visit(firstWithProofParameters),
            x,
            y);
        var yResult = Expression.Lambda<Func<double, double, bool>>(
            Expression.Equal(y, solutionYExpression), x, y);

        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.b2a6b08fb716"));
        proof.AppendLine();
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.b00bd8d2ad67"));
        for (var index = 0; index < equations.Count; index++)
        {
            proof.Append(index + 1).Append(RicisLegacyTextResources.Get("report.legacy.27d65ac1542c")).Append(equations[index]).AppendLine("`.");
        }

        proof.AppendLine();
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.c2d567cd98f3"));
        AppendBinaryHypotheses(proof, constraints);
        proof.AppendLine();
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.86bbede19942"));
        proof.Append(RicisLegacyTextResources.Get("report.legacy.86d27bbedc15")).Append(claim).AppendLine("`.");
        proof.AppendLine();
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.0878e35a8b03"));
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.70415d124b43"));
        proof.AppendLine();
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.5262659204cc"));
        proof.Append(RicisLegacyTextResources.Get("report.legacy.dcf6072b731b"))
            .Append(yName)
            .Append(RicisLegacyTextResources.Get("report.legacy.408dfcbf3047"))
            .Append(yName)
            .AppendLine(RicisLegacyTextResources.Get("report.legacy.8a5545be6873"));
        proof.Append(RicisLegacyTextResources.Get("report.legacy.d524beb3aacc")).Append(equations[0]).Append("`; `").Append(equations[1]).AppendLine("`.");
        proof.Append(RicisLegacyTextResources.Get("report.legacy.42ee707c919d")).Append(combined).AppendLine("`.");
        proof.AppendLine();
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.fcc467b3afa5"));
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.f212371d0307"));
        proof.Append(RicisLegacyTextResources.Get("report.legacy.d524beb3aacc")).Append(combined).AppendLine("`.");
        proof.Append(RicisLegacyTextResources.Get("report.legacy.42ee707c919d")).Append(xResult).AppendLine("`.");
        proof.AppendLine();
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.6f23c5b99ce5"));
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.64cc74660900"));
        proof.Append(RicisLegacyTextResources.Get("report.legacy.d524beb3aacc")).Append(equations[0]).AppendLine("`.");
        proof.Append(RicisLegacyTextResources.Get("report.legacy.42ee707c919d")).Append(substituted).AppendLine("`.");
        proof.AppendLine();
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.c51ce201457c"));
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.e9882dd8a639"));
        proof.Append(RicisLegacyTextResources.Get("report.legacy.d524beb3aacc")).Append(substituted).AppendLine("`.");
        proof.Append(RicisLegacyTextResources.Get("report.legacy.42ee707c919d")).Append(yResult).AppendLine("`.");
        proof.AppendLine();
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.bbf4ba2e6ad8"));
        proof.Append(RicisLegacyTextResources.Get("report.legacy.06f2ae300f06")).Append(xName).Append('=').Append(solutionXExpression)
            .Append(RicisLegacyTextResources.Get("report.legacy.7b721e1a35e3")).Append(yName).Append('=').Append(solutionYExpression)
            .Append(RicisLegacyTextResources.Get("report.legacy.bbd38a15a2f4")).Append(coordinate == 0 ? xName : yName)
            .Append(RicisLegacyTextResources.Get("report.legacy.836805b1ab44")).Append(derived).AppendLine("`.");
    }

    private static void AppendBinaryHypotheses(
        StringBuilder proof,
        IReadOnlyList<Expression<Func<double, double, bool>>> constraints)
    {
        if (constraints.Count == 0)
        {
            proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.36892d843abf"));
            return;
        }

        for (var index = 0; index < constraints.Count; index++)
        {
            proof.Append(index + 1).Append(RicisLegacyTextResources.Get("report.legacy.52ef51fa44c4")).Append(constraints[index]).AppendLine("`.");
        }
    }

    private sealed record LinearEquation(double X, double Y, double Constant);

    private readonly record struct ExactRational(BigInteger Numerator, BigInteger Denominator)
    {
        public static ExactRational Create(BigInteger numerator, BigInteger denominator)
        {
            if (denominator.IsZero)
            {
                throw new DivideByZeroException(RicisLegacyTextResources.Get("report.legacy.512a9ec3ee1b"));
            }

            if (denominator.Sign < 0)
            {
                numerator = BigInteger.Negate(numerator);
                denominator = BigInteger.Negate(denominator);
            }

            var divisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(numerator), denominator);
            return new ExactRational(numerator / divisor, denominator / divisor);
        }
    }

    private sealed class BinarySystemNormalizationStage
    {
        private BinarySystemNormalizationStage()
        {
        }
    }

    private sealed class ParameterSubstitutionVisitor : ParameterRebindingVisitorBase
    {
        public ParameterSubstitutionVisitor(ParameterExpression parameter, Expression replacement)
            : base(parameter, replacement)
        {
        }
    }

    private static void ValidateHypotheses<T>(
        IEnumerable<Expression<Func<T, bool>>> hypotheses,
        string parameterName)
        where T : INumber<T>
    {
        foreach (var hypothesis in hypotheses)
        {
            if (hypothesis is null)
            {
                throw new ArgumentException(RicisLegacyTextResources.Get("report.legacy.8547868348ec"), parameterName);
            }

            if (hypothesis.Parameters.Count != 1 || hypothesis.Parameters[0].Type != typeof(T) ||
                hypothesis.ReturnType != typeof(bool))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentUICulture, RicisLegacyTextResources.Get("report.legacy.590e8b782833"), parameterName, typeof(T).Name),
                    parameterName);
            }
        }
    }

    private static void ValidateClaim<T>(Expression<Func<T, T>> claim)
        where T : INumber<T>
    {
        if (claim.Parameters.Count != 1 || claim.Parameters[0].Type != typeof(T) || claim.ReturnType != typeof(T))
        {
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentUICulture, RicisLegacyTextResources.Get("report.legacy.6bbbd1303a70"), typeof(T).Name),
                nameof(claim));
        }
    }

    private static void AppendAcademicProtocol<T>(
        StringBuilder proof,
        IReadOnlyList<Expression<Func<T, bool>>> conditions,
        IReadOnlyList<Expression<Func<T, bool>>> constraints,
        Expression<Func<T, T>> claim,
        IReadOnlyList<RicisPhaseTraceStep> trace,
        Expression<Func<T, T>> derived)
        where T : INumber<T>
    {
        if (proof.Length > 0 && proof[^1] != '\n')
        {
            proof.AppendLine();
        }

        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.58180e513657"));
        proof.AppendLine();
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.3f105799f77e"));
        AppendHypotheses(proof, RicisLegacyTextResources.Get("report.legacy.1d10d9c55d5c"), conditions);
        proof.AppendLine();
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.c2d567cd98f3"));
        AppendHypotheses(proof, RicisLegacyTextResources.Get("report.legacy.69108a6a41e1"), constraints);
        proof.AppendLine();
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.86bbede19942"));
        proof.Append(RicisLegacyTextResources.Get("report.legacy.cfd73644651c")).Append(claim).AppendLine("`.");
        proof.AppendLine();
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.18a125f8065c"));
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.013809e31b03"));
        proof.AppendLine();

        if (trace.Count == 0)
        {
            proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.a9ee3d52d88b"));
            proof.AppendLine();
        }

        for (var index = 0; index < trace.Count; index++)
        {
            var step = trace[index];
            proof.Append(RicisLegacyTextResources.Get("report.legacy.d2635f2fd366")).Append(index + 1).Append(": ").AppendLine(step.PhaseName);
            proof.Append(RicisLegacyTextResources.Get("report.legacy.33192345bb99")).AppendLine(step.RuleFamily + ".");
            AppendNodeToRootRoutes(proof, RicisLegacyTextResources.Get("report.legacy.49a973eea1d4"), step.BeforeNodeToRoot);
            AppendNodeToRootRoutes(proof, RicisLegacyTextResources.Get("report.legacy.3cb90a6bb85f"), step.AfterNodeToRoot);

            if (step.WasSkipped)
            {
                proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.141f5490f383"));
                proof.AppendLine();
                continue;
            }

            var intermediateSteps = BuildIntermediateSteps(step);
            if (intermediateSteps.Count == 0)
            {
                proof.Append(RicisLegacyTextResources.Get("report.legacy.d524beb3aacc")).Append(step.Before).AppendLine("`.");
                proof.Append(RicisLegacyTextResources.Get("report.legacy.42ee707c919d")).Append(step.After).AppendLine("`.");
                if (!step.Changed)
                {
                    proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.3debb20fe486"));
                }
            }
            else
            {
                proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.693b883781ad"));
                for (var detailIndex = 0; detailIndex < intermediateSteps.Count; detailIndex++)
                {
                    var detail = intermediateSteps[detailIndex];
                    proof.Append(RicisLegacyTextResources.Get("report.legacy.8a44c047d185"))
                        .Append(index + 1)
                        .Append('.')
                        .Append(detailIndex + 1)
                        .Append(": ")
                        .AppendLine(detail.Title);
                    proof.Append(RicisLegacyTextResources.Get("report.legacy.5101234f6e4d")).AppendLine(detail.Rule + ".");
                    proof.Append(RicisLegacyTextResources.Get("report.legacy.d524beb3aacc")).Append(FormatAsLambda(step.Before, detail.Before)).AppendLine("`.");
                    proof.Append(RicisLegacyTextResources.Get("report.legacy.42ee707c919d")).Append(FormatAsLambda(step.Before, detail.After)).AppendLine("`.");
                }
            }

            proof.AppendLine();
        }

        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.bbf4ba2e6ad8"));
        proof.Append(RicisLegacyTextResources.Get("report.legacy.2f4a3d1b6922"))
            .Append(derived)
            .AppendLine("`.");
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.d341d259b277"));
    }

    private static void AppendTypedProofLog(
        StringBuilder proof,
        IReadOnlyList<RicisLogEntry> entries)
    {
        proof.AppendLine();
        proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.15a9b901b551"));
        if (entries.Count == 0)
        {
            proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.006f60e6911d"));
            return;
        }

        foreach (var entry in entries)
        {
            proof.Append('[').Append(entry.Sequence).Append("] ")
                .Append(entry.Severity).Append(" ")
                .Append(entry.StageType).Append(" :: ")
                .Append(entry.EventCode).Append(" — ")
                .AppendLine(entry.Message);
            if (entry.Severity == RicisLogSeverity.Trace)
            {
                proof.Append("  before: ").AppendLine(entry.BeforeExpression ?? string.Empty);
                proof.Append("  after: ").AppendLine(entry.AfterExpression ?? string.Empty);
            }

            if (entry.Severity == RicisLogSeverity.Exception)
            {
                proof.Append("  exception: ").AppendLine(entry.ExceptionType ?? string.Empty);
                proof.Append("  trace: ").AppendLine(entry.ExceptionTrace ?? string.Empty);
            }
        }
    }

    private static void AppendNodeToRootRoutes(
        StringBuilder proof,
        string label,
        IReadOnlyList<string> routes)
    {
        proof.Append(RicisLegacyTextResources.Get("report.legacy.b41bd539e40e")).Append(label).AppendLine("):**");
        foreach (var route in routes)
        {
            proof.Append("- `").Append(route).AppendLine("`.");
        }

        proof.AppendLine();
    }

    private static IReadOnlyList<IntermediateProofStep> BuildIntermediateSteps(RicisPhaseTraceStep step)
    {
        if (!step.RuleFamily.StartsWith("SP2", StringComparison.Ordinal) ||
            step.Before is not LambdaExpression before ||
            step.After is not LambdaExpression after ||
            before.Parameters.Count != 1)
        {
            return [];
        }

        if (TryBuildDifferenceOfSquaresSteps(before.Body, after.Body, out var squares))
        {
            return squares;
        }

        if (TryBuildDifferenceOfCubesSteps(before.Body, after.Body, out var cubes))
        {
            return cubes;
        }

        if (TryBuildSumOfCubesSteps(before.Body, after.Body, out var sumOfCubes))
        {
            return sumOfCubes;
        }

        if (TryBuildNestedRatioSteps(before.Body, after.Body, out var nestedRatio))
        {
            return nestedRatio;
        }

        if (before.Body is not BinaryExpression { NodeType: ExpressionType.Divide, Right: BinaryExpression { NodeType: ExpressionType.Multiply } } &&
            TryBuildCommonFactorSteps(before.Body, after.Body, out var commonFactor))
        {
            return commonFactor;
        }

        if (TryBuildAssociativeFactorSteps(before.Body, after.Body, out var associativeFactors))
        {
            return associativeFactors;
        }

        if (TryBuildAdjacentFactorialSteps(before.Body, after.Body, out var factorials))
        {
            return factorials;
        }

        return [];
    }

    private static bool TryBuildDifferenceOfSquaresSteps(
        Expression before,
        Expression after,
        out IReadOnlyList<IntermediateProofStep> steps)
    {
        steps = [];
        if (before is not BinaryExpression { NodeType: ExpressionType.Divide, Left: BinaryExpression { NodeType: ExpressionType.Subtract } numerator, Right: BinaryExpression denominator } ||
            denominator.NodeType is not ExpressionType.Subtract and not ExpressionType.Add ||
            !TryReadSquare(numerator.Left, out var leftBase) ||
            !TryReadSquare(numerator.Right, out var rightBase) ||
            !denominator.Left.AreEqual(leftBase) ||
            !denominator.Right.AreEqual(rightBase))
        {
            return false;
        }

        var factor = denominator;
        var quotient = denominator.NodeType == ExpressionType.Subtract
            ? Expression.Add(leftBase, rightBase)
            : Expression.Subtract(leftBase, rightBase);
        var factorized = Expression.Divide(Expression.Multiply(factor, quotient), factor);
        if (!quotient.AreEqual(after))
        {
            return false;
        }

        steps =
        [
            new IntermediateProofStep(
                RicisLegacyTextResources.Get("report.legacy.0f11e9888280"),
                "SP2: A²−B² = (A−B)(A+B)",
                before,
                factorized),
            new IntermediateProofStep(
                RicisLegacyTextResources.Get("report.legacy.e258776142b4"),
                "SP2: (F·G)/F = G",
                factorized,
                quotient),
        ];
        return true;
    }

    private static bool TryBuildDifferenceOfCubesSteps(
        Expression before,
        Expression after,
        out IReadOnlyList<IntermediateProofStep> steps)
    {
        steps = [];
        if (before is not BinaryExpression { NodeType: ExpressionType.Divide, Left: BinaryExpression { NodeType: ExpressionType.Subtract } numerator, Right: var denominator } ||
            !TryReadCube(numerator.Left, out var leftBase) ||
            !TryReadCube(numerator.Right, out var rightBase) ||
            denominator is not BinaryExpression { NodeType: ExpressionType.Subtract } difference ||
            !difference.Left.AreEqual(leftBase) ||
            !difference.Right.AreEqual(rightBase))
        {
            return false;
        }

        var leftSquare = Expression.Multiply(leftBase, leftBase);
        // AlgebraicReductionVisitor canonicalizes the mixed term as B·A.
        // Reproduce that exact expression tree so the proof chain terminates
        // at the independently derived SP2 result without a fictitious swap.
        var product = Expression.Multiply(rightBase, leftBase);
        var rightSquare = BuildSquare(rightBase);
        var quotient = Expression.Add(Expression.Add(leftSquare, product), rightSquare);
        var factor = Expression.Subtract(leftBase, rightBase);
        var factorized = Expression.Divide(Expression.Multiply(factor, quotient), factor);
        if (!quotient.AreEqual(after))
        {
            return false;
        }

        steps =
        [
            new IntermediateProofStep(
                RicisLegacyTextResources.Get("report.legacy.85cd78952b8f"),
                "SP2: A³−B³ = (A−B)(A²+A·B+B²)",
                before,
                factorized),
            new IntermediateProofStep(
                RicisLegacyTextResources.Get("report.legacy.e258776142b4"),
                "SP2: (F·G)/F = G",
                factorized,
                quotient),
        ];
        return true;
    }

    private static bool TryBuildSumOfCubesSteps(
        Expression before,
        Expression after,
        out IReadOnlyList<IntermediateProofStep> steps)
    {
        steps = [];
        if (before is not BinaryExpression { NodeType: ExpressionType.Divide, Left: BinaryExpression { NodeType: ExpressionType.Add } numerator, Right: BinaryExpression { NodeType: ExpressionType.Add } denominator } ||
            !TryReadCube(numerator.Left, out var leftBase) ||
            !TryReadCube(numerator.Right, out var rightBase) ||
            !denominator.Left.AreEqual(leftBase) ||
            !denominator.Right.AreEqual(rightBase))
        {
            return false;
        }

        var leftSquare = Expression.Multiply(leftBase, leftBase);
        var negativeProduct = Expression.Multiply(BuildNegated(rightBase), leftBase);
        var rightSquare = BuildSquare(rightBase);
        var quotient = Expression.Add(Expression.Add(leftSquare, negativeProduct), rightSquare);
        var factor = Expression.Add(leftBase, rightBase);
        var factorized = Expression.Divide(Expression.Multiply(factor, quotient), factor);
        if (!quotient.AreEqual(after))
        {
            return false;
        }

        steps =
        [
            new IntermediateProofStep(
                RicisLegacyTextResources.Get("report.legacy.3dd4d0a65eae"),
                "SP2: A³+B³ = (A+B)(A²−A·B+B²)",
                before,
                factorized),
            new IntermediateProofStep(
                RicisLegacyTextResources.Get("report.legacy.e258776142b4"),
                "SP2: (F·G)/F = G",
                factorized,
                quotient),
        ];
        return true;
    }

    private static bool TryBuildNestedRatioSteps(
        Expression before,
        Expression after,
        out IReadOnlyList<IntermediateProofStep> steps)
    {
        steps = [];
        if (before is not BinaryExpression { NodeType: ExpressionType.Divide, Left: var numerator, Right: BinaryExpression { NodeType: ExpressionType.Divide } ratio })
        {
            return false;
        }

        var normalized = Expression.Divide(Expression.Multiply(numerator, ratio.Right), ratio.Left);
        if (normalized.AreEqual(after))
        {
            steps =
            [
                new IntermediateProofStep(
                    RicisLegacyTextResources.Get("report.legacy.a2fd90a5210d"),
                    "SP2: F/(G/H) = (F·H)/G",
                    before,
                    normalized),
            ];
            return true;
        }

        if (TryBuildCommonFactorSteps(normalized, after, out var commonFactor))
        {
            steps =
            [
                new IntermediateProofStep(
                    RicisLegacyTextResources.Get("report.legacy.a2fd90a5210d"),
                    "SP2: F/(G/H) = (F·H)/G",
                    before,
                    normalized),
                .. commonFactor,
            ];
            return true;
        }

        if (TryBuildAssociativeFactorSteps(normalized, after, out var associativeFactors))
        {
            steps =
            [
                new IntermediateProofStep(
                    RicisLegacyTextResources.Get("report.legacy.a2fd90a5210d"),
                    "SP2: F/(G/H) = (F·H)/G",
                    before,
                    normalized),
                .. associativeFactors,
            ];
            return true;
        }

        return false;
    }

    private static bool TryBuildAssociativeFactorSteps(
        Expression before,
        Expression after,
        out IReadOnlyList<IntermediateProofStep> steps)
    {
        steps = [];
        if (before is not BinaryExpression { NodeType: ExpressionType.Divide, Left: var numerator, Right: var denominator })
        {
            return false;
        }

        var numeratorFactors = FlattenBuiltInProduct(numerator);
        var denominatorFactors = FlattenBuiltInProduct(denominator);
        if (numeratorFactors.Count < 2 && denominatorFactors.Count < 2)
        {
            return false;
        }

        var remainingNumerator = numeratorFactors.ToList();
        var remainingDenominator = denominatorFactors.ToList();
        var cancelled = 0;
        for (var denominatorIndex = remainingDenominator.Count - 1; denominatorIndex >= 0; denominatorIndex--)
        {
            var matchIndex = remainingNumerator.FindIndex(factor => factor.AreEqual(remainingDenominator[denominatorIndex]));
            if (matchIndex < 0)
            {
                continue;
            }

            remainingNumerator.RemoveAt(matchIndex);
            remainingDenominator.RemoveAt(denominatorIndex);
            cancelled++;
        }

        if (cancelled == 0)
        {
            return false;
        }

        var reducedNumerator = BuildProduct(remainingNumerator, numerator.Type);
        var reducedDenominator = BuildProduct(remainingDenominator, denominator.Type);
        var reduced = reducedDenominator.IsOne()
            ? reducedNumerator
            : Expression.Divide(reducedNumerator, reducedDenominator);
        if (!reduced.AreEqual(after))
        {
            return false;
        }

        steps =
        [
            new IntermediateProofStep(
                RicisLegacyTextResources.Get("report.legacy.c84003b1378c"),
                RicisLegacyTextResources.Get("report.legacy.8511d53a41e4"),
                before,
                reduced),
        ];
        return true;
    }

    private static List<Expression> FlattenBuiltInProduct(Expression expression)
    {
        var factors = new List<Expression>();
        CollectBuiltInProductFactors(expression, factors);
        return factors;
    }

    private static void CollectBuiltInProductFactors(Expression expression, ICollection<Expression> factors)
    {
        if (expression is BinaryExpression { NodeType: ExpressionType.Multiply } product &&
            (product.Method is null || NumericConstants.IsIntrinsicNumeric(product.Type)))
        {
            CollectBuiltInProductFactors(product.Left, factors);
            CollectBuiltInProductFactors(product.Right, factors);
            return;
        }

        factors.Add(expression);
    }

    private static Expression BuildProduct(IReadOnlyList<Expression> factors, Type scalarType) => factors.Count switch
    {
        0 => NumericConstants.OneOf(scalarType),
        1 => factors[0],
        _ => factors.Aggregate(Expression.Multiply),
    };

    private static bool TryBuildAdjacentFactorialSteps(
        Expression before,
        Expression after,
        out IReadOnlyList<IntermediateProofStep> steps)
    {
        steps = [];
        if (before is not BinaryExpression { NodeType: ExpressionType.Divide, Left: MethodCallExpression numerator, Right: MethodCallExpression denominator } ||
            numerator.Method != typeof(Factorial).GetMethod(nameof(Factorial.Of)) ||
            denominator.Method != typeof(Factorial).GetMethod(nameof(Factorial.Of)) ||
            numerator.Arguments.Count != 1 || denominator.Arguments.Count != 1)
        {
            return false;
        }

        var value = numerator.Arguments[0];
        if (denominator.Arguments[0] is not BinaryExpression { NodeType: ExpressionType.Subtract, Left: var predecessorValue, Right: var decrement } ||
            !predecessorValue.AreEqual(value) ||
            !IsOneOrStaticOne(decrement, value.Type) ||
            !value.AreEqual(after))
        {
            return false;
        }

        steps =
        [
            new IntermediateProofStep(
                RicisLegacyTextResources.Get("report.legacy.007af9a345c6"),
                "SP2: n!/(n−1)! = n",
                before,
                value),
        ];
        return true;
    }

    private static bool IsOneOrStaticOne(Expression expression, Type scalarType)
    {
        if (expression.IsOne())
        {
            return true;
        }

        return expression is MemberExpression { Expression: null, Member: var member } &&
               member.Name == "One" &&
               member.DeclaringType == scalarType;
    }

    private static bool TryBuildCommonFactorSteps(
        Expression before,
        Expression after,
        out IReadOnlyList<IntermediateProofStep> steps)
    {
        steps = [];
        if (before is not BinaryExpression { NodeType: ExpressionType.Divide, Left: BinaryExpression { NodeType: ExpressionType.Multiply } product, Right: var denominator })
        {
            return false;
        }

        var factor = product.Left.AreEqual(denominator)
            ? product.Left
            : product.Right.AreEqual(denominator)
                ? product.Right
                : null;
        var quotient = factor is null
            ? null
            : product.Left.AreEqual(factor)
                ? product.Right
                : product.Left;
        if (factor is null || !quotient.AreEqual(after))
        {
            return false;
        }

        steps =
        [
            new IntermediateProofStep(
                RicisLegacyTextResources.Get("report.legacy.e258776142b4"),
                "SP2: (F·G)/F = G",
                before,
                quotient),
        ];
        return true;
    }

    private static Expression BuildNegated(Expression expression)
    {
        if (expression is ConstantExpression { Type: var type, Value: double value } && type == typeof(double))
        {
            return Expression.Constant(-value, typeof(double));
        }

        return Expression.Negate(expression);
    }

    private static Expression BuildSquare(Expression expression)
    {
        if (expression is ConstantExpression { Type: var type, Value: double value } && type == typeof(double))
        {
            return Expression.Constant(value * value, typeof(double));
        }

        return Expression.Multiply(expression, expression);
    }

    private static bool TryReadSquare(Expression expression, out Expression @base)
    {
        @base = null;
        if (expression is BinaryExpression { NodeType: ExpressionType.Multiply, Left: var left, Right: var right } &&
            left.AreEqual(right))
        {
            @base = left;
            return true;
        }

        if (expression is BinaryExpression { NodeType: ExpressionType.Power, Left: var powerBase, Right: ConstantExpression { Value: double exponent } } &&
            exponent == 2.0)
        {
            @base = powerBase;
            return true;
        }

        return TryReadExactDoublePower(expression, 2, out @base);
    }

    private static bool TryReadCube(Expression expression, out Expression @base)
    {
        @base = null;
        if (expression is BinaryExpression { NodeType: ExpressionType.Multiply, Left: BinaryExpression { NodeType: ExpressionType.Multiply } square, Right: var third } &&
            square.Left.AreEqual(square.Right) &&
            square.Left.AreEqual(third))
        {
            @base = square.Left;
            return true;
        }

        if (expression is BinaryExpression { NodeType: ExpressionType.Power, Left: var powerBase, Right: ConstantExpression { Value: double exponent } } &&
            exponent == 3.0)
        {
            @base = powerBase;
            return true;
        }

        return TryReadExactDoublePower(expression, 3, out @base);
    }

    private static bool TryReadExactDoublePower(Expression expression, int power, out Expression @base)
    {
        @base = null;
        if (expression is not ConstantExpression { Type: var type, Value: double value } || type != typeof(double))
        {
            return false;
        }

        var root = power == 2 ? Math.Sqrt(value) : Math.Cbrt(value);
        var rounded = Math.Round(root);
        if (double.IsNaN(root) || double.IsInfinity(root) || Math.Pow(rounded, power) != value)
        {
            return false;
        }

        @base = Expression.Constant(rounded, typeof(double));
        return true;
    }

    private static Expression FormatAsLambda(Expression sourceLambda, Expression body)
    {
        if (sourceLambda is LambdaExpression lambda)
        {
            return Expression.Lambda(lambda.Type, body, lambda.Parameters);
        }

        return body;
    }

    private sealed record IntermediateProofStep(string Title, string Rule, Expression Before, Expression After);

    private static void AppendHypotheses<T>(
        StringBuilder proof,
        string label,
        IReadOnlyList<Expression<Func<T, bool>>> hypotheses)
        where T : INumber<T>
    {
        if (hypotheses.Count == 0)
        {
            proof.AppendLine(RicisLegacyTextResources.Get("report.legacy.20dc04c11e20"));
            return;
        }

        for (var index = 0; index < hypotheses.Count; index++)
        {
            proof.Append(index + 1)
                .Append(". ")
                .Append(label)
                .Append(": `")
                .Append(hypotheses[index])
                .AppendLine("`.");
        }
    }
}
