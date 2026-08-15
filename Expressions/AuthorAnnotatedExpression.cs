using System.Linq.Expressions;
using Ricis.Core.Metadata;

namespace Ricis.Core.Expressions;

/// <summary>
/// Keeps an already derived RICIS expression unchanged while extending its
/// textual representation with an opt-in author SEO block.
/// </summary>
public sealed class AuthorAnnotatedExpression : Expression
{
    public Expression Body { get; }
    public AuthorSeoProfile Profile { get; }

    public AuthorAnnotatedExpression(Expression body, AuthorSeoProfile profile)
    {
        Body = body ?? throw new ArgumentNullException(nameof(body));
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public override ExpressionType NodeType => ExpressionType.Extension;
    public override Type Type => Body.Type;
    public override bool CanReduce => true;
    public override Expression Reduce() => Body;

    public override string ToString() => $"{Body}{Environment.NewLine}{Profile.ToDisplayBlock()}";
}
