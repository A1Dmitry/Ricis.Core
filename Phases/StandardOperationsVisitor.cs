using Ricis.Core.Expressions;
using Ricis.Core.Extensions;
using Ricis.Core.Simplifiers;
using System.Linq.Expressions;

namespace Ricis.Core.Phases;

/// <summary>
/// Applies the standard RICIS operations A4–A7 after bridge and transform
/// phases. Indexed zeros and indexed infinities are distinct forms and are
/// deliberately handled by separate rules.
/// </summary>
public class StandardOperationsVisitor : ExpressionVisitor, IExpressionVisitor
{
    /// <inheritdoc />
    protected override Expression VisitExtension(Expression node)
    {
        if (node is InfinityExpression or DeferredDerivativeExpression)
        {
            return node;
        }

        return base.VisitExtension(node);
    }

    /// <inheritdoc />
    protected override Expression VisitBinary(BinaryExpression node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        // RICIS standard operations redefine only intrinsic .NET arithmetic.
        // User-defined operator semantics stay strictly classical.
        if (node.Method is not null && !NumericConstants.IsIntrinsicNumeric(node.Type))
        {
            return Rebuild(node, left, right);
        }

        // O(1): F/∞_G -> 0_F. This must precede the generic keyed-pole guard;
        // KeyedInfinity is still a true infinity, and its complete root set is
        // retained on the resulting indexed zero.
        if (node.NodeType == ExpressionType.Divide &&
            right is InfinityExpression denominatorInfinity &&
            IsTrueInfinity(denominatorInfinity) &&
            left is not InfinityExpression)
        {
            return new ZeroInfinityExpression(left, denominatorInfinity.Roots);
        }

        // A keyed pole contains different F(a) values for different keys. It
        // must remain branch-aware for operations without a normative branchwise rule.
        if (left is KeyedInfinityExpression || right is KeyedInfinityExpression)
        {
            return Rebuild(node, left, right);
        }

        // Indexed-zero algebra. These forms are not infinities and therefore
        // must be handled before A5–A7.
        if (left is ZeroInfinityExpression zeroLeft && right is ZeroInfinityExpression zeroRight &&
            AreRootsCompatible(zeroLeft, zeroRight))
        {
            return node.NodeType switch
            {
                ExpressionType.Add => BuildZeroOperation(zeroLeft, zeroRight, ExpressionType.Add),
                ExpressionType.Subtract => BuildZeroOperation(zeroLeft, zeroRight, ExpressionType.Subtract),
                ExpressionType.Multiply => BuildZeroOperation(zeroLeft, zeroRight, ExpressionType.Multiply),
                // A4: 0_F / 0_G -> F/G; equal identities preserve L1.
                ExpressionType.Divide when zeroLeft.Numerator.AreEqual(zeroRight.Numerator) =>
                    NumericConstants.OneOf(zeroLeft.Numerator.Type),
                ExpressionType.Divide => Expression.Divide(zeroLeft.Numerator, zeroRight.Numerator),
                _ => Rebuild(node, left, right)
            };
        }

        // An indexed zero retains the ordinary additive identity property when
        // paired with a finite term. The index has already been recorded by
        // O(1), while Z-01 above retains both indices for 0_F + 0_G.
        if (node.NodeType == ExpressionType.Add)
        {
            if (left is ZeroInfinityExpression)
            {
                return right;
            }

            if (right is ZeroInfinityExpression)
            {
                return left;
            }
        }

        if (node.NodeType == ExpressionType.Subtract)
        {
            if (left is ZeroInfinityExpression subtractZeroLeft && right is ZeroInfinityExpression subtractZeroRight &&
                AreRootsCompatible(subtractZeroLeft, subtractZeroRight))
            {
                return BuildZeroOperation(subtractZeroLeft, subtractZeroRight, ExpressionType.Subtract);
            }

            if (left is ZeroInfinityExpression)
            {
                return Expression.Negate(right);
            }

            if (right is ZeroInfinityExpression)
            {
                return left;
            }
        }

        // A6: 0_F * ∞_G -> F*G. A zero is never accepted as the ∞ operand.
        if (node.NodeType == ExpressionType.Multiply)
        {
            if (left is ZeroInfinityExpression indexedZeroLeft && IsTrueInfinity(right))
            {
                return Expression.Multiply(indexedZeroLeft.Numerator, ((InfinityExpression)right).Numerator);
            }

            if (right is ZeroInfinityExpression indexedZeroRight && IsTrueInfinity(left))
            {
                return Expression.Multiply(((InfinityExpression)left).Numerator, indexedZeroRight.Numerator);
            }

            // Associative O(1) consequence: 0_F·G = (F·0)·G -> 0_{F·G}.
            // This preserves the zero index in derivative products.
            if (left is ZeroInfinityExpression zeroFactorLeft && IsScalar(right))
            {
                return BuildIndexedZeroProduct(zeroFactorLeft, right);
            }

            if (right is ZeroInfinityExpression zeroFactorRight && IsScalar(left))
            {
                return BuildIndexedZeroProduct(zeroFactorRight, left);
            }

            // O(1): F·0 -> 0_F. The deferred parent index is retained.
            if (left.IsZero())
            {
                return new ZeroInfinityExpression(right, []);
            }

            if (right.IsZero())
            {
                return new ZeroInfinityExpression(left, []);
            }
        }

        // 0_F / G -> 0_F/G for finite non-zero G. The index records the
        // finite division instead of being collapsed to an unindexed constant.
        if (node.NodeType == ExpressionType.Divide &&
            left is ZeroInfinityExpression indexedDividend &&
            IsScalar(right) &&
            !right.IsZero())
        {
            var rawIndex = Expression.Divide(indexedDividend.Numerator, right);
            var index = SimplifyIndexedPayload(rawIndex);
            return new ZeroInfinityExpression(index, indexedDividend.Roots);
        }

        // A5/A7 operate only on true indexed infinities, never on 0_F.
        if (IsTrueInfinity(left) && IsTrueInfinity(right))
        {
            var infinityLeft = (InfinityExpression)left;
            var infinityRight = (InfinityExpression)right;
            if (AreRootsCompatible(infinityLeft, infinityRight))
            {
                switch (node.NodeType)
                {
                    case ExpressionType.Add:
                        return MergeInfinities(infinityLeft, infinityRight, ExpressionType.Add);
                    case ExpressionType.Subtract:
                        return MergeInfinities(infinityLeft, infinityRight, ExpressionType.Subtract);
                    case ExpressionType.Multiply:
                        return MergeInfinities(infinityLeft, infinityRight, ExpressionType.Multiply);
                    case ExpressionType.Divide:
                        return infinityLeft.Numerator.AreEqual(infinityRight.Numerator)
                            ? NumericConstants.OneOf(infinityLeft.Numerator.Type)
                            : Expression.Divide(infinityLeft.Numerator, infinityRight.Numerator);
                }
            }
        }

        // Scalar multiplication/division preserves an infinity index.
        if (node.NodeType == ExpressionType.Multiply)
        {
            if (IsTrueInfinity(left) && IsScalar(right))
            {
                var infinity = (InfinityExpression)left;
                return InfinityExpression.CreateLazy(Expression.Multiply(infinity.Numerator, right), infinity.Roots);
            }

            if (IsTrueInfinity(right) && IsScalar(left))
            {
                var infinity = (InfinityExpression)right;
                return InfinityExpression.CreateLazy(Expression.Multiply(left, infinity.Numerator), infinity.Roots);
            }
        }

        if (node.NodeType == ExpressionType.Divide && IsTrueInfinity(left) && IsScalar(right))
        {
            var infinity = (InfinityExpression)left;
            return InfinityExpression.CreateLazy(Expression.Divide(infinity.Numerator, right), infinity.Roots);
        }

        if (node.NodeType == ExpressionType.Multiply)
        {
            if (left.IsOne()) return right;
            if (right.IsOne()) return left;
            if (left.IsZero()) return left;
            if (right.IsZero()) return right;
        }

        if (node.NodeType == ExpressionType.Add)
        {
            if (left.IsZero()) return right;
            if (right.IsZero()) return left;
        }

        return Rebuild(node, left, right);
    }

    private static ZeroInfinityExpression BuildIndexedZeroProduct(
        ZeroInfinityExpression indexedZero,
        Expression finiteFactor)
    {
        var index = Expression.Multiply(indexedZero.Numerator, finiteFactor);
        return new ZeroInfinityExpression(index, indexedZero.Roots);
    }

    private static Expression BuildZeroOperation(
        ZeroInfinityExpression left,
        ZeroInfinityExpression right,
        ExpressionType operation)
    {
        var rawIndex = Expression.MakeBinary(operation, left.Numerator, right.Numerator);
        var index = SimplifyIndexedPayload(rawIndex);
        return new ZeroInfinityExpression(index, left.Roots);
    }

    private static Expression SimplifyIndexedPayload(Expression rawIndex)
    {
        var simplified = new ExpressionSimplifierVisitor().Visit(rawIndex);
        return simplified is not null && simplified.Type == rawIndex.Type
            ? simplified
            : rawIndex;
    }

    private static Expression MergeInfinities(InfinityExpression left, InfinityExpression right, ExpressionType operation)
    {
        var newNumerator = Expression.MakeBinary(operation, left.Numerator, right.Numerator);
        return InfinityExpression.CreateLazy(newNumerator, left.Roots);
    }

    private static bool IsTrueInfinity(Expression expression) =>
        expression is InfinityExpression && expression is not ZeroInfinityExpression;

    private static bool AreRootsCompatible(InfinityExpression left, InfinityExpression right)
    {
        var leftRoots = left.Roots;
        var rightRoots = right.Roots;
        if (leftRoots.Count != rightRoots.Count)
        {
            return false;
        }

        // Empty root sets represent the same deferred O(1) context. For
        // concrete keys compare the complete root multiset injectively; no key
        // may be reused to hide a different certified root.
        var used = new bool[rightRoots.Count];
        foreach (var rootLeft in leftRoots)
        {
            var match = -1;
            for (var index = 0; index < rightRoots.Count; index++)
            {
                var rootRight = rightRoots[index];
                if (!used[index] && rootLeft.Param == rootRight.Param &&
                    Math.Abs(rootLeft.Value - rootRight.Value) < 1e-9)
                {
                    match = index;
                    break;
                }
            }

            if (match < 0)
            {
                return false;
            }

            used[match] = true;
        }

        return true;
    }

    private static bool IsScalar(Expression expression) => !IsTrueInfinity(expression) && expression is not ZeroInfinityExpression;

    private static Expression Rebuild(BinaryExpression node, Expression left, Expression right) =>
        left == node.Left && right == node.Right
            ? node
            : Expression.MakeBinary(node.NodeType, left, right, node.IsLiftedToNull, node.Method);
}
