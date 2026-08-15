using System.Linq.Expressions;

namespace Ricis.Core.Metadata;

/// <summary>
/// Detects the compiler-generated closure member for an opt-in variable named
/// <c>about</c>. A lambda parameter called about deliberately does not match.
/// </summary>
public static class AboutCaptureDetector
{
    public static bool IsCaptured(Expression expression)
    {
        var visitor = new CaptureVisitor();
        visitor.Visit(expression);
        return visitor.Found;
    }

    private sealed class CaptureVisitor : ExpressionVisitor
    {
        public bool Found { get; private set; }

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
