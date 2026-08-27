using System.Linq.Expressions;

namespace Ricis.Core.Metadata;

/// <summary>
/// Detects the opt-in variable or lambda parameter named <c>about</c>.
/// Closure capture and parameter-based opt-in are both supported.
/// </summary>
public static class AboutCaptureDetector
{
    /// <summary>
    /// Determines whether the supplied expression captures an outer variable named <c>about</c>.
    /// </summary>
    public static bool IsCaptured(Expression expression)
    {
        var visitor = new CaptureVisitor();
        visitor.Visit(expression);
        return visitor.Found;
    }

    /// <summary>
    /// Determines whether the supplied expression opts into author metadata by
    /// either capturing an outer <c>about</c> variable or naming a lambda parameter <c>about</c>.
    /// </summary>
    public static bool IsAboutOptIn(Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var visitor = new CaptureVisitor();
        visitor.Visit(expression);
        return visitor.Found || visitor.AboutParameterFound;
    }

    private sealed class CaptureVisitor : ExpressionVisitor
    {
        public bool Found { get; private set; }
        public bool AboutParameterFound { get; private set; }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (node.Parameters.Any(parameter =>
                    string.Equals(parameter.Name, "about", StringComparison.OrdinalIgnoreCase)))
            {
                AboutParameterFound = true;
            }

            return base.VisitLambda(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Member.Name == "about" && node.Expression is ConstantExpression)
            {
                Found = true;
            }

            return base.VisitMember(node);
        }

        // RICIS has intentionally non-reducible symbolic nodes. Detection must
        // inspect ordinary closure members without attempting to reduce them.
        protected override Expression VisitExtension(Expression node) => node;
    }
}
