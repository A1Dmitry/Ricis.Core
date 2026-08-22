using System.Linq.Expressions;

namespace Ricis.Core.CachedSolutions;

/// <summary>
/// A verified test-backed solution. The classical and RICIS expectations are
/// stored separately because cancellation may preserve singularity metadata.
/// </summary>
public sealed record CachedSolution(
    string Id,
    LambdaExpression Formula,
    LambdaExpression ClassicalExpectation,
    LambdaExpression RicisExpectation,
    string SourceTest,
    string Notes,
    string DisplayName = "",
    string SourceUrl = "",
    string Explanation = "",
    string ClassicalResult = "",
    string RicisResult = "",
    string Status = "confirmed",
    string[] Tags = null,
    double MapX = 0,
    double MapY = 0,
    double MapZ = 0)
{
    /// <summary>Converts the verified solution into a UI-ready 3D map bubble.</summary>
    public CachedSolutionBubble ToBubble() => new(
        Id,
        string.IsNullOrWhiteSpace(DisplayName) ? Id : DisplayName,
        Formula.ToString(),
        string.IsNullOrWhiteSpace(ClassicalResult) ? ClassicalExpectation.ToString() : ClassicalResult,
        string.IsNullOrWhiteSpace(RicisResult) ? RicisExpectation.ToString() : RicisResult,
        SourceTest,
        SourceUrl,
        Explanation,
        Notes,
        Status,
        Tags ?? Array.Empty<string>(),
        MapX,
        MapY,
        MapZ);
}

/// <summary>Complete seed payload for one confirmed solution bubble in the 3D map.</summary>
public sealed record CachedSolutionBubble(
    string Id,
    string DisplayName,
    string Formula,
    string ClassicalResult,
    string RicisResult,
    string SourceTest,
    string SourceUrl,
    string Explanation,
    string Notes,
    string Status,
    IReadOnlyList<string> Tags,
    double X,
    double Y,
    double Z);

/// <summary>
/// A cached answer is a hypothesis until the deeper RICIS III pass validates it.
/// </summary>
public sealed record CachedSolutionProposal(
    CachedSolution Solution,
    double Similarity,
    LambdaExpression ClassicalHypothesis,
    LambdaExpression RicisCandidate,
    bool RicisValidated,
    string ValidationMessage);

/// <summary>Index of test-backed formulas used by the no-agent fallback.</summary>
public sealed class CachedSolutionIndex
{
    private readonly IReadOnlyList<CachedSolution> _solutions;

    /// <summary>Initializes the index from verified cached solutions.</summary>
    public CachedSolutionIndex(IEnumerable<CachedSolution> solutions)
    {
        _solutions = solutions?.ToArray() ?? throw new ArgumentNullException(nameof(solutions));
    }

    /// <summary>Gets all cached solutions currently available for lookup.</summary>
    public IReadOnlyList<CachedSolution> Solutions => _solutions;

    /// <summary>Gets only confirmed solutions projected as 3D map bubbles.</summary>
    public IReadOnlyList<CachedSolutionBubble> ConfirmedBubbles => _solutions
        .Where(solution => string.Equals(solution.Status, "confirmed", StringComparison.OrdinalIgnoreCase))
        .Select(solution => solution.ToBubble())
        .ToArray();

    /// <summary>Finds cached formulas with a shared structural shape.</summary>
    public IReadOnlyList<(CachedSolution Solution, double Similarity)> FindSimilar(
        LambdaExpression query,
        int maxResults = 5)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (maxResults <= 0) return Array.Empty<(CachedSolution, double)>();

        var queryShape = ExpressionShape.Of(query);
        return _solutions
            .Select(solution => (solution, Similarity: ExpressionShape.Similarity(queryShape, ExpressionShape.Of(solution.Formula))))
            .Where(match => match.Similarity >= 0.5d)
            .OrderByDescending(match => match.Similarity)
            .Take(maxResults)
            .ToArray();
    }

    /// <summary>
    /// Uses the closest test-backed answer only as a hypothesis, then runs the
    /// supplied deep RICIS III reducer and validates its result against the
    /// cached RICIS expectation. No claim is returned as validated otherwise.
    /// </summary>
    public CachedSolutionProposal ResolveWithoutAgent(
        LambdaExpression query,
        Func<LambdaExpression, LambdaExpression> deepRicisReduction)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(deepRicisReduction);

        var match = FindSimilar(query, 1).FirstOrDefault();
        if (match.Solution is null) return null;

        var candidate = deepRicisReduction(query);
        var valid = Expressions.ExpressionStructuralComparer.AreEqual(
            candidate.Body,
            match.Solution.RicisExpectation.Body);
        var message = valid
            ? "RICIS III deep reduction matches the cached expectation."
            : "Cached solution was only a hypothesis; RICIS III result differs and remains unvalidated.";

        return new CachedSolutionProposal(
            match.Solution,
            match.Similarity,
            match.Solution.ClassicalExpectation,
            candidate,
            valid,
            message);
    }
}

internal static class ExpressionShape
{
    public static string Of(Expression expression)
    {
        var tokens = new List<string>();
        new ShapeVisitor(tokens).Visit(expression);
        return string.Join("|", tokens);
    }

    public static double Similarity(string left, string right)
    {
        if (left == right) return 1d;
        var a = left.Split('|', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var b = right.Split('|', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (a.Count == 0 || b.Count == 0) return 0d;
        return (double)a.Intersect(b).Count() / a.Union(b).Count();
    }

    private sealed class ShapeVisitor(List<string> tokens) : ExpressionVisitor
    {
        protected override Expression VisitBinary(BinaryExpression node)
        {
            tokens.Add($"B:{node.NodeType}");
            return base.VisitBinary(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            tokens.Add($"U:{node.NodeType}");
            return base.VisitUnary(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            tokens.Add($"M:{node.Method.DeclaringType?.FullName}:{node.Method.Name}");
            return base.VisitMethodCall(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            tokens.Add($"C:{node.Type.FullName}");
            return node;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            tokens.Add($"P:{node.Type.FullName}");
            return node;
        }
    }
}
