using System.Linq.Expressions;
using Ricis.Core.Metadata;

namespace Ricis.Core.Expressions;

/// <summary>
/// Keeps an already derived RICIS expression unchanged while extending its
/// textual representation with an opt-in author SEO block.
/// </summary>
public sealed class AuthorAnnotatedExpression : Expression
{
    /// <summary>
    /// Gets the <c>Body</c> value of <c>AuthorAnnotatedExpression</c>.
    /// </summary>
    public Expression Body { get; }
    /// <summary>
    /// Gets the <c>Profile</c> value of <c>AuthorAnnotatedExpression</c>.
    /// </summary>
    public AuthorSeoProfile Profile { get; }

    /// <summary>
    /// Initializes a new instance of <c>AuthorAnnotatedExpression</c>.
    /// </summary>
    public AuthorAnnotatedExpression(Expression body, AuthorSeoProfile profile)
    {
        Body = body ?? throw new ArgumentNullException(nameof(body));
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    /// <inheritdoc />
    public override ExpressionType NodeType => ExpressionType.Extension;
    /// <inheritdoc />
    public override Type Type => Body.Type;
    /// <inheritdoc />
    public override bool CanReduce => true;
    /// <inheritdoc />
    public override Expression Reduce() => Body;

    /// <inheritdoc />
    public override string ToString() => $"{Body}{Environment.NewLine}{Profile.ToDisplayBlock()}";
}
