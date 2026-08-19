# DRY refactoring roadmap

## Правило работы

DRY-рефакторинг выполняется атомарно. Каждый шаг должен иметь ограниченную область, пройти regression/quality gate и быть зафиксирован отдельным commit до перехода к следующему шагу. Production behavior изменять нельзя без отдельного contract test.

## Аудит дублирования

Первичный audit выявил три класса повторений:

| Область | Наблюдение | Приоритет |
|---|---|---:|
| Regression suites | Одинаковый `Require(bool, string)` с `InvalidOperationException` повторялся во множестве suite | P1 |
| Regression suites | Повторяются `Assert`, `Expect<TException>`, `X()` и `Rebind` helpers; их нужно объединять только после проверки одинаковой семантики | P2 |
| Console demos | Повторяются compile-and-print loops для sampled points | P2 |
| Production simplifiers | Повторяются numeric extraction/rebinding patterns, но их нельзя объединять механически без сохранения type/generic semantics | P1 после отдельного design review |

## Завершённый шаг DRY-01

Введён общий `RegressionAssertions.Require` и удалены локальные копии из:

- `RegressionTests/RicisVectorSuite.cs`;
- `RegressionTests/RicisVectorExpressionSuite.cs`.

Тестовые сообщения, порядок проверок и exception behavior сохранены. Production code не изменялся.

Проверка DRY-01: **344/344 Core regression tests passed**, включая `VECTOR01–VECTOR08` и `VEX01–VEX06`.

## Завершённый шаг DRY-02

Общий `RegressionAssertions.Expect<TException>` применён к пяти suites:

- `RegressionTests/ExpressionSystemSuite.cs`;
- `RegressionTests/RicisJacobianSingularitySuite.cs`;
- `RegressionTests/RicisMatrixExpressionSuite.cs`;
- `RegressionTests/RicisVectorSuite.cs`;
- `RegressionTests/RicisVectorExpressionSuite.cs`.

Удалены пять идентичных локальных generic exception helpers. Rejection behavior сохранён и проверен тестами `ES04`, `JSG04`, `MEX04`, `VECTOR06`, `VECTOR07` и `VEX06`. DRY-02 quality gate: **344/344 Core regression tests**, Console Release build без warnings/errors, **12/12 Finance tests**, **6/6 Lean artifacts**.

## Следующие шаги

Следующим безопасным кандидатом является перенос одинаковых `Require` из ещё одной однородной группы suites. После этого отдельно проверяется Console helper duplication. Production simplifier refactoring выполняется только после доказательства, что объединяемые helpers имеют одинаковые generic, type-preserving и trust-boundary semantics.
