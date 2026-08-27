using System.Linq.Expressions;
using Ricis.Core.Expressions;

namespace Ricis.Core.Phases;

/// <summary>
/// Represents one deterministic transformation attempt of the normative RICIS
/// pipeline. The step stores complete before/after expression trees, their
/// node-to-root routes, the phase name, and the governing rule family.
/// </summary>
public sealed class RicisPhaseTraceStep
{
    /// <summary>
    /// Initializes one pipeline trace step and captures every expression-node
    /// route from that node upward to the root of both tree snapshots.
    /// </summary>
    public RicisPhaseTraceStep(
        string phaseName,
        string ruleFamily,
        Expression before,
        Expression after,
        bool wasSkipped)
    {
        PhaseName = phaseName ?? throw new ArgumentNullException(nameof(phaseName));
        RuleFamily = ruleFamily ?? throw new ArgumentNullException(nameof(ruleFamily));
        Before = before ?? throw new ArgumentNullException(nameof(before));
        After = after ?? throw new ArgumentNullException(nameof(after));
        WasSkipped = wasSkipped;
        BeforeNodeToRoot = NodeToRootRouteVisitor.Capture(Before);
        AfterNodeToRoot = NodeToRootRouteVisitor.Capture(After);
    }

    /// <summary>Gets the ordered normative phase that attempted a transformation.</summary>
    public string PhaseName { get; }

    /// <summary>Gets the RICIS axiom family or structural rule family governing the phase.</summary>
    public string RuleFamily { get; }

    /// <summary>Gets the expression tree before this phase.</summary>
    public Expression Before { get; }

    /// <summary>Gets the expression tree after this phase.</summary>
    public Expression After { get; }

    /// <summary>
    /// Gets every route in the pre-phase tree, written from the visited node up
    /// to the lambda/root expression.
    /// </summary>
    public IReadOnlyList<string> BeforeNodeToRoot { get; }

    /// <summary>
    /// Gets every route in the post-phase tree, written from the visited node up
    /// to the lambda/root expression.
    /// </summary>
    public IReadOnlyList<string> AfterNodeToRoot { get; }

    /// <summary>
    /// Gets whether the phase was deliberately skipped because its certified
    /// double-domain precondition was unavailable for the current expression.
    /// </summary>
    public bool WasSkipped { get; }

    /// <summary>
    /// Gets whether the phase changed the structural RICIS expression. A
    /// deliberately skipped phase is never reported as a transformation.
    /// </summary>
    public bool Changed => !WasSkipped && !Before.AreEqual(After);

    private sealed class NodeToRootRouteVisitor : ExpressionVisitor
    {
        private readonly Stack<string> _ancestors = new();
        private readonly List<string> _routes = [];

        private NodeToRootRouteVisitor()
        {
        }

        public static IReadOnlyList<string> Capture(Expression root)
        {
            ArgumentNullException.ThrowIfNull(root);
            var visitor = new NodeToRootRouteVisitor();
            visitor.Visit(root);
            return Array.AsReadOnly(visitor._routes.ToArray());
        }

        public override Expression Visit(Expression node)
        {
            if (node is null)
            {
                return null;
            }

            _ancestors.Push(Describe(node));
            try
            {
                _routes.Add(string.Join(" -> ", _ancestors));
                return base.Visit(node);
            }
            finally
            {
                _ancestors.Pop();
            }
        }

        protected override Expression VisitExtension(Expression node)
        {
            return node.CanReduce ? Visit(node.Reduce()) ?? node : node;
        }

        private static string Describe(Expression node) => node switch
        {
            LambdaExpression lambda => $"Lambda<{lambda.Type.Name}>",
            ParameterExpression parameter => $"Parameter({parameter.Name ?? "_"})",
            ConstantExpression constant => $"Constant({constant.Value?.ToString() ?? "null"})",
            BinaryExpression binary => binary.NodeType.ToString(),
            UnaryExpression unary => unary.NodeType.ToString(),
            MethodCallExpression method => $"Call({method.Method.Name})",
            MemberExpression member => $"Member({member.Member.Name})",
            RicisExpression ricis => ricis.GetType().Name,
            _ => node.NodeType.ToString(),
        };
    }
}
