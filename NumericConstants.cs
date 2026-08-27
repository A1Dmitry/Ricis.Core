using System.Collections.Concurrent;
using Ricis.Core.Resources;
using System.Linq.Expressions;
using System.Numerics;

namespace Ricis.Core;

/// <summary>
/// Typed additive and multiplicative identities for scalar types used in RICIS
/// expressions. Built-in numeric types are registered automatically. A custom
/// <c>INumber&lt;TSelf&gt;</c> type is registered only when it is passed to a
/// legacy non-generic API; universal generic pipeline entry points use an
/// explicit immutable scalar policy and do not register the type.
/// </summary>
public static class NumericConstants
{
    private sealed record NumericInfo(
        object Zero,
        object One,
        Func<object, bool> IsZero,
        Func<object, bool> IsOne);

    private static readonly ConcurrentDictionary<Type, NumericInfo> Registered = new();

    static NumericConstants()
    {
        Register<byte>();
        Register<sbyte>();
        Register<short>();
        Register<ushort>();
        Register<int>();
        Register<uint>();
        Register<long>();
        Register<ulong>();
        Register<nint>();
        Register<nuint>();
        Register<Int128>();
        Register<UInt128>();
        Register<Half>();
        Register<float>();
        Register<double>();
        Register<decimal>();
        Register<BigInteger>();
    }

    /// <summary>
    /// Registers a scalar implementing .NET generic math. Call this once before
/// passing a custom scalar directly into the non-generic phase pipeline.
/// Generic entry points do not use this registry.
    /// </summary>
    public static void Register<T>() where T : INumber<T>
    {
        Registered.TryAdd(typeof(T), new NumericInfo(
            T.Zero,
            T.One,
            value => value is T typed && typed.Equals(T.Zero),
            value => value is T typed && typed.Equals(T.One)));
    }

    /// <summary>Returns <c>T.Zero</c> as a typed expression constant.</summary>
    public static ConstantExpression ZeroOf(Type type)
    {
        return Expression.Constant(Get(type).Zero, type);
    }

    /// <summary>Returns <c>T.One</c> as a typed expression constant.</summary>
    public static ConstantExpression OneOf(Type type)
    {
        return Expression.Constant(Get(type).One, type);
    }

    /// <summary>
    /// Attempts to obtain the typed multiplicative identity for a scalar. This
    /// lets the highest-priority F/F rule fall back safely when an arbitrary
    /// user type has not supplied a numeric identity to RICIS.
    /// </summary>
    public static bool TryOneOf(Type type, out ConstantExpression one)
    {
        if (Registered.TryGetValue(type, out var info))
        {
            one = Expression.Constant(info.One, type);
            return true;
        }

        one = null;
        return false;
    }

    /// <summary>
    /// Indicates whether a type belongs to the intrinsic .NET numeric domain
    /// whose arithmetic is covered by RICIS scalar rules. User-defined
    /// overloaded operators remain classical unless a separate RICIS rule
    /// explicitly supports their semantic domain.
    /// </summary>
    public static bool IsIntrinsicNumeric(Type type) =>
        type == typeof(byte) || type == typeof(sbyte) ||
        type == typeof(short) || type == typeof(ushort) ||
        type == typeof(int) || type == typeof(uint) ||
        type == typeof(long) || type == typeof(ulong) ||
        type == typeof(nint) || type == typeof(nuint) ||
        type == typeof(Int128) || type == typeof(UInt128) ||
        type == typeof(Half) || type == typeof(float) ||
        type == typeof(double) || type == typeof(decimal) ||
        type == typeof(BigInteger);

    /// <summary>Returns whether the supplied registered scalar is <c>T.Zero</c>.</summary>
    public static bool IsZero(object value)
    {
        return Registered.TryGetValue(value.GetType(), out var info) && info.IsZero(value);
    }

    /// <summary>Returns whether the supplied registered scalar is <c>T.One</c>.</summary>
    public static bool IsOne(object value)
    {
        return Registered.TryGetValue(value.GetType(), out var info) && info.IsOne(value);
    }

    private static NumericInfo Get(Type type)
    {
        return Registered.TryGetValue(type, out var info)
            ? info
            : throw new NotSupportedException(
                RicisLegacyTextResources.Format("runtime.legacy.a1fda29896f1", ("type.FullName", type.FullName), ("type.Name", type.Name)) +
                RicisLegacyTextResources.Get("runtime.legacy.1de16102d47f"));
    }
}
