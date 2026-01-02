using Ricis.Core.Expressions;
using System.Linq.Expressions;

namespace Ricis.Core.Extensions;

public static class ExpressionComparableExtensions
{
    public static ExpressionComparable<T> AsComparable<T>(this T expr)
        where T : Expression
        => new(expr);

}