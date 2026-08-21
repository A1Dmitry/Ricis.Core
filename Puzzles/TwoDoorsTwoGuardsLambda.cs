using System.ComponentModel;

namespace Ricis.Core.Puzzles;

/// <summary>
/// A Church-encoded boolean: chooses one of two values without using the CLR Boolean type.
/// </summary>
/// <typeparam name="T">The type of either encoded door result.</typeparam>
[Description("Лямбда-терм булева Чёрча, выбирающий один из двух результатов без CLR Boolean.")]
public delegate T ChurchBoolean<T>(T whenTrue, T whenFalse);

/// <summary>
/// A guard maps a proposed Church-encoded answer to the answer that the guard would state.
/// </summary>
/// <typeparam name="T">The type carried by the Church-encoded answer.</typeparam>
[Description("Лямбда-охранник, преобразующий предложенный Church-ответ в произносимый ответ.")]
public delegate ChurchBoolean<T> DoorGuard<T>(ChurchBoolean<T> proposedAnswer);

/// <summary>
/// Closed, typed lambda terms for the two doors and two guards puzzle.
/// The production model contains lambda values only: it uses no conditional branch,
/// CLR Boolean, mutation, loop, exception or imperative proof path.
/// </summary>
/// <typeparam name="T">The type selected by the resulting Church boolean.</typeparam>
[Description("Набор чистых типизированных лямбда-термов для задачи о двух дверях и двух охранниках.")]
public static class TwoDoorsTwoGuardsLambda<T>
{
    /// <summary>
    /// Church TRUE: select the first candidate, representing the left safe door.
    /// </summary>
    [Description("Лямбда Church TRUE: выбирает первый результат, соответствующий левой безопасной двери.")]
    public static readonly ChurchBoolean<T> True = (whenTrue, _) => whenTrue;

    /// <summary>
    /// Church FALSE: select the second candidate, representing the right safe door.
    /// </summary>
    [Description("Лямбда Church FALSE: выбирает второй результат, соответствующий правой безопасной двери.")]
    public static readonly ChurchBoolean<T> False = (_, whenFalse) => whenFalse;

    /// <summary>
    /// Church negation swaps the two candidates selected by its input.
    /// </summary>
    [Description("Лямбда отрицания Чёрча, меняющая местами два возможных результата.")]
    public static readonly Func<ChurchBoolean<T>, ChurchBoolean<T>> Not = value =>
        (whenTrue, whenFalse) => value(whenFalse, whenTrue);

    /// <summary>
    /// The truthful guard returns the answer supplied to the question.
    /// </summary>
    [Description("Лямбда правдивого охранника, возвращающая предложенный ответ без изменения.")]
    public static readonly DoorGuard<T> Truth = answer => answer;

    /// <summary>
    /// The lying guard returns the Church negation of the answer supplied to the question.
    /// </summary>
    [Description("Лямбда лгущего охранника, возвращающая отрицание предложенного ответа.")]
    public static readonly DoorGuard<T> Liar = answer => Not(answer);

    /// <summary>
    /// Ask the first guard what the second guard would say, then negate that statement.
    /// For either ordering of exactly one truthful and one lying guard, the closed term reduces to safeDoor.
    /// </summary>
    [Description("Замкнутая лямбда выхода: отрицание ответа первого охранника о том, что сказал бы второй.")]
    public static readonly Func<DoorGuard<T>, DoorGuard<T>, ChurchBoolean<T>, ChurchBoolean<T>> Escape =
        (firstGuard, secondGuard, safeDoor) => Not(firstGuard(secondGuard(safeDoor)));
}
