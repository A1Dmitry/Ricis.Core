# Logical Reduction Design

## Нормативная граница

`LogicalReductionVisitor` является отдельной фазой RICIS и работает только со встроенными Boolean expression nodes. Он не исполняет expression tree, не вызывает пользовательские методы и не раскрывает RICIS extension payload.

## Разрешённые эквивалентные правила

| Правило | Почему безопасно |
|---|---|
| `!true → false`, `!false → true` | Полная constant evaluation без пользовательского кода |
| `!!x → x` | Для `bool` два встроенных logical negations сохраняют результат и порядок вычисления `x` |
| `true && x → x` | `true` не меняет short-circuit decision; `x` вычисляется один раз |
| `x && true → x` | `x` вычисляется один раз, результат `bool` совпадает |
| `false || x → x` | `false` не меняет short-circuit decision; `x` вычисляется один раз |
| `x || false → x` | `x` вычисляется один раз, результат `bool` совпадает |
| `condition ? x : x → x` | Условие продолжает вычисляться, ветви дают один и тот же результат |
| `constant bool == constant bool` и `!=` | Полная constant evaluation без пользовательского кода |
| `constant bool ? constant : constant` | Все части не имеют observable execution и могут быть свернуты |

## Намеренно запрещённые правила

`false && x → false`, `true || x → true`, `x && false → false` и `x || true → true` запрещены в общем случае: они могут пропустить вычисление `x`, его побочные эффекты или исключение. Аналогично не выполняется `x && x → x` и `x || x → x`, потому что это изменило бы число вычислений `x`.

User-defined Boolean operators (`BinaryExpression.Method != null`), lifted nullable Boolean expressions и узлы с типами, отличными от `System.Boolean`, visitor не изменяет.

## Алгоритмический слой Квайна—Мак-Класки

После локальных structural rules visitor пытается применить ограниченную минимизацию только к чистому Boolean tree, составленному из `bool` parameters, constants, встроенных `Not`, `AndAlso` и `OrElse`. Для truth table используется максимум шесть переменных; это предохранитель от экспоненциального роста, а не математическое ограничение метода. Строятся prime implicants, затем детерминированно выбирается покрытие единичных minterms и собирается эквивалентная DNF.

Tree с `MethodCallExpression`, user-defined Boolean operator, nullable/lifted Boolean или иным узлом отвергается как неподходящий для QM backend и остаётся в structural form. Поэтому алгоритм не исполняет пользовательский код и не меняет потенциально observable short-circuit behavior. Для чистых parameter-only trees повторное чтение параметров не создаёт side effects.

## Placement

Фаза ставится после `AlgebraicReductionVisitor` и до `LimitBridgeVisitor`. Она не конфликтует с RICIS arithmetic semantics и попадает в обычный phase trace/log pipeline через существующий generic stage wrapper. Для обычного дерева trace теперь содержит восемь нормативных фаз, включая `Фаза 1.25 — логическая редукция`.
