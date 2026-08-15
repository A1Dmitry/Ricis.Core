using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

/// <summary>
/// A finite union of A1 poles whose certified keys have different values of
/// F(a). Each branch remains an ordinary indexed infinity; this wrapper keeps
/// the complete key-to-index correspondence instead of collapsing it to one
/// global numerator.
/// </summary>
public sealed class KeyedInfinityExpression : InfinityExpression
{
    private readonly IReadOnlyList<PoleInfinityExpression> _branches;

    /// <summary>
    /// Gets immutable pole branches. Each branch carries its own exact key set
    /// and numerator index.
    /// </summary>
    public IReadOnlyList<PoleInfinityExpression> Branches => _branches;

    /// <summary>
    /// Initializes a keyed infinity from at least two non-null pole branches.
    /// The branch list and roots are copied so later caller mutations cannot
    /// change the established key-to-index relation.
    /// </summary>
    public KeyedInfinityExpression(IReadOnlyList<PoleInfinityExpression> branches)
        : base(CollectRoots(branches))
    {
        _branches = branches.ToArray();
    }

    /// <summary>
    /// A keyed pole has no single global index. This non-semantic placeholder
    /// exists only for legacy APIs; equality and RICIS operations must inspect
    /// <see cref="Branches"/> rather than this value.
    /// </summary>
    public override Expression Numerator => Expression.Default(Type);

    /// <summary>
    /// Gets the scalar type shared by all branches.
    /// </summary>
    public override Type Type => _branches[0].Type;

    /// <inheritdoc />
    public override bool CanReduce => false;

    /// <inheritdoc />
    public override string ToString() => string.Join("; ", _branches.Select(branch => branch.ToString()));

    private static List<(ParameterExpression Param, double Value)> CollectRoots(
        IReadOnlyList<PoleInfinityExpression> branches)
    {
        ArgumentNullException.ThrowIfNull(branches);
        if (branches.Count < 2)
        {
            throw new ArgumentException("A keyed infinity requires at least two index branches.", nameof(branches));
        }

        if (branches.Any(branch => branch is null))
        {
            throw new ArgumentException("A keyed infinity cannot contain a null branch.", nameof(branches));
        }

        var branchType = branches[0].Type;
        if (branches.Any(branch => branch.Type != branchType))
        {
            throw new ArgumentException("All keyed infinity branches must have one scalar type.", nameof(branches));
        }

        return branches
            .SelectMany(branch => branch.Roots)
            .OrderBy(root => root.Value)
            .ToList();
    }
}
