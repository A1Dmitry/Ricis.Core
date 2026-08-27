using System.ComponentModel;

using Ricis.Core.Resources;

namespace Ricis.Core.Puzzles;

/// <summary>
/// A Church-encoded boolean: chooses one of two values without using the CLR Boolean type.
/// </summary>
/// <typeparam name="T">The type of either encoded door result.</typeparam>
[LocalizedDescription("runtime.legacy.90b0cbc96541")]
public delegate T ChurchBoolean<T>(T whenTrue, T whenFalse);

/// <summary>
/// A guard maps a proposed Church-encoded answer to the answer that the guard would state.
/// </summary>
/// <typeparam name="T">The type carried by the Church-encoded answer.</typeparam>
[LocalizedDescription("runtime.legacy.cf9de63dc537")]
public delegate ChurchBoolean<T> DoorGuard<T>(ChurchBoolean<T> proposedAnswer);

/// <summary>
/// Closed, typed lambda terms for the two doors and two guards puzzle.
/// The production model contains lambda values only: it uses no conditional branch,
/// CLR Boolean, mutation, loop, exception or imperative proof path.
/// </summary>
/// <typeparam name="T">The type selected by the resulting Church boolean.</typeparam>
[LocalizedDescription("runtime.legacy.d457cdd013eb")]
public static class TwoDoorsTwoGuardsLambda<T>
{
    /// <summary>
    /// Church TRUE: select the first candidate, representing the left safe door.
    /// </summary>
    [LocalizedDescription("runtime.legacy.b2013537888a")]
    public static readonly ChurchBoolean<T> True = (whenTrue, _) => whenTrue;

    /// <summary>
    /// Church FALSE: select the second candidate, representing the right safe door.
    /// </summary>
    [LocalizedDescription("runtime.legacy.653f76442ad0")]
    public static readonly ChurchBoolean<T> False = (_, whenFalse) => whenFalse;

    /// <summary>
    /// Church negation swaps the two candidates selected by its input.
    /// </summary>
    [LocalizedDescription("runtime.legacy.488319e7cc89")]
    public static readonly Func<ChurchBoolean<T>, ChurchBoolean<T>> Not = value =>
        (whenTrue, whenFalse) => value(whenFalse, whenTrue);

    /// <summary>
    /// The truthful guard returns the answer supplied to the question.
    /// </summary>
    [LocalizedDescription("runtime.legacy.48dc6c87a579")]
    public static readonly DoorGuard<T> Truth = answer => answer;

    /// <summary>
    /// The lying guard returns the Church negation of the answer supplied to the question.
    /// </summary>
    [LocalizedDescription("runtime.legacy.83370049d4bb")]
    public static readonly DoorGuard<T> Liar = answer => Not(answer);

    /// <summary>
    /// Ask the first guard what the second guard would say, then negate that statement.
    /// For either ordering of exactly one truthful and one lying guard, the closed term reduces to safeDoor.
    /// </summary>
    [LocalizedDescription("runtime.legacy.a643cae08cf8")]
    public static readonly Func<DoorGuard<T>, DoorGuard<T>, ChurchBoolean<T>, ChurchBoolean<T>> Escape =
        (firstGuard, secondGuard, safeDoor) => Not(firstGuard(secondGuard(safeDoor)));
}
