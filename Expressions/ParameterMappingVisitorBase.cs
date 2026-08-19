using System.Linq;
using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

/// <summary>
/// Shared base behavior for identity-based parameter-to-expression mapping.
/// The mapping is immutable for the lifetime of a visitor and preserves RICIS extension nodes.
/// </summary>
internal abstract class ParameterMappingVisitorBase : ExpressionVisitor
{
    private readonly IReadOnlyDictionary<ParameterExpression, Expression> _mapping;

    protected ParameterMappingVisitorBase(
        IReadOnlyList<ParameterExpression> source,
        IReadOnlyList<ParameterExpression> replacements)
        : this(source, replacements.Select(parameter => (Expression)parameter).ToArray())
    {
    }

    protected ParameterMappingVisitorBase(
        IReadOnlyList<ParameterExpression> source,
        IReadOnlyList<Expression> replacements)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(replacements);
        if (source.Count != replacements.Count)
        {
            throw new ArgumentException("Количество исходных параметров и замен должно совпадать.", nameof(replacements));
        }

        var mapping = new Dictionary<ParameterExpression, Expression>(ReferenceEqualityComparer.Instance);
        for (var index = 0; index < source.Count; index++)
        {
            ArgumentNullException.ThrowIfNull(source[index]);
            ArgumentNullException.ThrowIfNull(replacements[index]);
            mapping[source[index]] = replacements[index];
        }

        _mapping = mapping;
    }

    protected override Expression VisitExtension(Expression node) =>
        RicisSpecialExpressionRebinder.Rebind(node, Visit);

    protected override Expression VisitParameter(ParameterExpression node) =>
        _mapping.TryGetValue(node, out var replacement)
            ? replacement
            : base.VisitParameter(node);
}
