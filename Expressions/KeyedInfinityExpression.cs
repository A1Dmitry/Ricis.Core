using System.Linq.Expressions;

namespace Ricis.Core.Expressions;

/// <summary>
/// A finite union of A1 poles whose certified keys have different values of
/// F(a). Each branch remains an ordinary indexed infinity; this wrapper keeps
/// the key-to-index correspondence instead of discarding it during monolith
/// construction.
/// </summary>
public sealed class KeyedInfinityExpression : InfinityExpression
{
    public IReadOnlyList<PoleInfinityExpression> Branches { get; }

    public KeyedInfinityExpression(IReadOnlyList<PoleInfinityExpression> branches)
        : base(branches.SelectMany(branch => branch.Roots)
            .OrderBy(root => root.Value)
            .ToList())
    {
        if (branches.Count < 2)
        {
            throw new ArgumentException("A keyed infinity requires at least two index branches.", nameof(branches));
        }

        Branches = branches;
    }

    // A multi-branch object has no single global index. Consumers that need
    // A5–A7 must process branches by compatible root sets rather than use this
    // placeholder; StandardOperationsVisitor preserves such an operation.
    public override Expression Numerator => Expression.Constant(double.NaN);
    public override bool CanReduce => false;

    public override string ToString() => string.Join("; ", Branches.Select(branch => branch.ToString()));
}
