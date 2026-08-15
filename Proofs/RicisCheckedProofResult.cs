using System.Linq.Expressions;
using System.Numerics;

namespace Ricis.Core.Proofs;

/// <summary>
/// Stores the result of a proof driven by real lambda hypotheses and a real
/// expected expression. The verification expression is retained as an
/// expression tree and is never compiled by the proof engine.
/// </summary>
/// <typeparam name="T">The scalar type of the deferred expression.</typeparam>
public sealed class RicisCheckedProofResult<T> where T : INumber<T>
{
    /// <summary>
    /// Initializes a checked proof result.
    /// </summary>
    public RicisCheckedProofResult(
        Expression<Func<T, T>> derived,
        Expression<Func<T, T>> expected,
        Expression<Func<T, bool>> verification,
        bool isVerified,
        IReadOnlyList<Expression<Func<T, bool>>> conditions,
        IReadOnlyList<Expression<Func<T, bool>>> constraints)
    {
        Derived = derived ?? throw new ArgumentNullException(nameof(derived));
        Expected = expected ?? throw new ArgumentNullException(nameof(expected));
        Verification = verification ?? throw new ArgumentNullException(nameof(verification));
        Conditions = conditions ?? throw new ArgumentNullException(nameof(conditions));
        Constraints = constraints ?? throw new ArgumentNullException(nameof(constraints));
        IsVerified = isVerified;
    }

    /// <summary>Gets the independently derived RICIS expression.</summary>
    public Expression<Func<T, T>> Derived { get; }

    /// <summary>Gets the real expected expression supplied by the caller.</summary>
    public Expression<Func<T, T>> Expected { get; }

    /// <summary>
    /// Gets the real structural verification expression <c>derived == expected</c>.
    /// </summary>
    public Expression<Func<T, bool>> Verification { get; }

    /// <summary>Gets whether the normalized derived and expected trees are structurally equal.</summary>
    public bool IsVerified { get; }

    /// <summary>Gets the actual lambda conditions supplied to the proof engine.</summary>
    public IReadOnlyList<Expression<Func<T, bool>>> Conditions { get; }

    /// <summary>Gets the actual lambda constraints supplied to the proof engine.</summary>
    public IReadOnlyList<Expression<Func<T, bool>>> Constraints { get; }

    /// <inheritdoc />
    public override string ToString() =>
        $"Verified={IsVerified}; Derived={Derived}; Expected={Expected}; Verification={Verification}";
}
