using System.Linq.Expressions;
using System.Numerics;

namespace Ricis.Core.Extensions;

internal static class ExpressionValidation
{
    public static void EnsureUnary<T>(Expression<Func<T, T>> expression, string operation)
        where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentException.ThrowIfNullOrEmpty(operation);
        if (expression.Parameters.Count != 1)
        {
            throw new ArgumentException($"{operation} требует лямбду ровно с одним параметром.");
        }
    }
}
