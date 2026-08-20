# ILog/Tex audit for expression-system solving — 2026-08-20

## Scope

Проверен путь специализированного решения двухпеременной системы выражений: извлечение коэффициентов из expression tree, проверка determinant, символическое исключение, формирование координатного результата, сборка proof protocol и передача результата в LaTeX document factory.

## Найденный разрыв

Внутренний generic `RicisPhasePipeline` уже последовательно передавал nullable typed log через child facades. Однако специализированный binary system solver не имел параметра `ILog<T>` и не публиковал внутренние события. Его обычный document path поэтому формировал корректный system protocol, но не мог включить typed audit journal в LaTeX output.

> Renderer не может восстановить отсутствующие события: LaTeX factory получает уже собранный `derivation`. Поэтому исправление выполнено на orchestration boundary, а не внутри Tex renderer.

## Исправление

Существующий binary `Prove` получил необязательный nullable-параметр `ILog<RicisProofOrchestrationStage> log = null`. Старый вызов без log сохранён обратно совместимым. При наличии журнала solver публикует четыре canonical события:

| Sequence | Event code | Содержание |
|---:|---|---|
| 1 | `RICIS_SYSTEM_START` | начало решения, количество уравнений/ограничений и claim |
| 2 | `RICIS_SYSTEM_COEFFICIENTS` | извлечённые коэффициенты и determinant без исполнения гипотез |
| 3 | `RICIS_SYSTEM_ELIMINATION` | trace от claim до рассчитанных `x` и `y` |
| 4 | `RICIS_SYSTEM_COMPLETE` | завершение symbolic solution и derived expression |

Добавлен `ProveDocumentWithLog` для binary system. Он выполняет один solver pass, добавляет snapshot журнала в derivation и только затем вызывает общий document constructor. Поэтому LaTeX получает одновременно system steps и typed `ILog` events; второй proof pass не выполняется.

## Regression evidence

`PDF10: Binary system ILog reaches every solver step and LaTeX` проверяет exact порядок четырёх event codes, вычислимость derived expression, наличие system step 4 и наличие event codes в LaTeX document. До исправления такого direct test отсутствовал.

## Quality gate

| Проверка | Результат |
|---|---:|
| Solution build | PASS, 0 warnings, 0 errors |
| Core regression suite | 391/391 PASS |
| Numerics UnitTests | 124/124 PASS |
| Finance regressions | 19/19 PASS |
| `git diff --check` | PASS |
| Deleted files in diff | 0 |

Правило no-deletion соблюдено: production methods не удалялись; изменён только существующий solver boundary и добавлен direct regression test.
