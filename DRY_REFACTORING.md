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

## Следующие шаги

Следующим безопасным кандидатом является перенос одинаковых `Require` из ещё одной однородной группы suites. После этого отдельно проверяется Console helper duplication. Production simplifier refactoring выполняется только после доказательства, что объединяемые helpers имеют одинаковые generic, type-preserving и trust-boundary semantics.
