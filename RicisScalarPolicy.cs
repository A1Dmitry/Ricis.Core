using System.Linq.Expressions;
using System.Numerics;

namespace Ricis.Core;

/// <summary>
/// Internal scalar-domain policy supplied explicitly to RICIS reduction stages.
/// It separates the legacy built-in route from the universal generic
/// <c>INumber&lt;T&gt;</c> route without concrete external numeric-type knowledge.
/// </summary>
internal interface IRicisScalarPolicy
{
    /// <summary>Returns whether the supplied expression type is the active scalar domain.</summary>
    bool IsScalarType(Type type);

    /// <summary>Creates the active domain's exact additive identity.</summary>
    ConstantExpression ZeroOf(Type type);

    /// <summary>Creates the active domain's exact multiplicative identity.</summary>
    ConstantExpression OneOf(Type type);

    /// <summary>Creates an exact small integer constant in the active scalar domain.</summary>
    ConstantExpression FromInt32(int value, Type type);

    /// <summary>Returns whether a value is the active domain's exact additive identity.</summary>
    bool IsZeroValue(object value);

    /// <summary>Returns whether a value is the active domain's exact multiplicative identity.</summary>
    bool IsOneValue(object value);

    /// <summary>Returns whether RICIS may apply a safe structural rule to this arithmetic node.</summary>
    bool SupportsRicisArithmetic(BinaryExpression node);

    /// <summary>Returns whether the supplied expression exposes a valid unary-negation operator.</summary>
    bool SupportsUnaryNegation(Expression expression);
}

/// <summary>Creates explicit immutable scalar policies for legacy and generic RICIS routes.</summary>
internal static class RicisScalarPolicies
{
    /// <summary>Gets the compatibility policy for the legacy non-generic pipeline.</summary>
    public static IRicisScalarPolicy Legacy { get; } = new LegacyRicisScalarPolicy();

    /// <summary>Creates the universal policy for one statically known generic numeric scalar.</summary>
    public static IRicisScalarPolicy For<T>() where T : INumber<T> => GenericRicisScalarPolicy<T>.Instance;
}

internal sealed class LegacyRicisScalarPolicy : IRicisScalarPolicy
{
    public bool IsScalarType(Type type) => NumericConstants.IsIntrinsicNumeric(type);

    public ConstantExpression ZeroOf(Type type) => NumericConstants.ZeroOf(type);

    public ConstantExpression OneOf(Type type) => NumericConstants.OneOf(type);

    public ConstantExpression FromInt32(int value, Type type)
    {
        if (type == typeof(double)) return Expression.Constant((double)value);
        if (type == typeof(float)) return Expression.Constant((float)value);
        if (type == typeof(decimal)) return Expression.Constant((decimal)value);
        if (type == typeof(long)) return Expression.Constant((long)value);
        if (type == typeof(BigInteger)) return Expression.Constant(new BigInteger(value));
        return Expression.Constant(value, type);
    }

    public bool IsZeroValue(object value) => value is not null && NumericConstants.IsZero(value);

    public bool IsOneValue(object value) => value is not null && NumericConstants.IsOne(value);

    public bool SupportsRicisArithmetic(BinaryExpression node) =>
        node.Method is null || NumericConstants.IsIntrinsicNumeric(node.Type);

    public bool SupportsUnaryNegation(Expression expression)
    {
        try
        {
            _ = Expression.Negate(expression);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }
}

internal sealed class GenericRicisScalarPolicy<T> : IRicisScalarPolicy
    where T : INumber<T>
{
    public static GenericRicisScalarPolicy<T> Instance { get; } = new();

    private GenericRicisScalarPolicy()
    {
    }

    public bool IsScalarType(Type type) => type == typeof(T);

    public ConstantExpression ZeroOf(Type type) =>
        type == typeof(T)
            ? Expression.Constant(T.Zero, typeof(T))
            : throw new ArgumentException($"Expected scalar type {typeof(T).FullName}, received {type.FullName}.", nameof(type));

    public ConstantExpression OneOf(Type type) =>
        type == typeof(T)
            ? Expression.Constant(T.One, typeof(T))
            : throw new ArgumentException($"Expected scalar type {typeof(T).FullName}, received {type.FullName}.", nameof(type));

    public ConstantExpression FromInt32(int value, Type type) =>
        type == typeof(T)
            ? Expression.Constant(T.CreateChecked(value), typeof(T))
            : throw new ArgumentException($"Expected scalar type {typeof(T).FullName}, received {type.FullName}.", nameof(type));

    public bool IsZeroValue(object value) => value is T typed && typed.Equals(T.Zero);

    public bool IsOneValue(object value) => value is T typed && typed.Equals(T.One);

    public bool SupportsRicisArithmetic(BinaryExpression node) =>
        node.Type == typeof(T) &&
        (node.Method is null || node.Method.ReturnType == typeof(T));

    public bool SupportsUnaryNegation(Expression expression)
    {
        if (expression.Type != typeof(T))
        {
            return false;
        }

        try
        {
            _ = Expression.Negate(expression);
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return false;
        }
    }
}
