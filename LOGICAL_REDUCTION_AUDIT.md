# Logical Reduction Audit

## Статус

Аудит завершён как Phase 1 следующего спринта. Historical baseline Core regression: **344/344 PASS**. Current verified Core gate: **386/386 PASS**; measured evidence is recorded in `RICIS_TASK_TIME_2H_SPRINT_2026-08-20_02.md`.

## Фактическое поведение .NET Expression.Reduce

Для обычных `AndAlso`, `OrElse`, `Not` и `ConditionalExpression` свойство `CanReduce` возвращает `false`, а `Reduce()` возвращает тот же узел. Проверено изолированным .NET 8 probe:

| Expression | CanReduce | Reduce result |
|---|---:|---|
| `x AndAlso True` | `false` | тот же узел |
| `True AndAlso x` | `false` | тот же узел |
| `x OrElse False` | `false` | тот же узел |
| `False OrElse x` | `false` | тот же узел |
| `Not(True)` | `false` | тот же узел |
| `IIF(True, x, False)` | `false` | тот же узел |

Следовательно, стандартный LINQ expression-tree infrastructure предоставляет обход и reduction contract для extension nodes, но не является логическим simplifier и не выполняет Boolean identities автоматически.

## Текущее состояние Ricis.Core

`RicisPhasePipeline` содержит стадии identity, polar, algebraic, bridge, transform, type consistency и standard operations. `AlgebraicReductionVisitor` реализует structural arithmetic rules (`+ 0`, `- 0`, `* 1`, cancellation, ratios, powers, factorization), а `StandardOperationsVisitor` реализует indexed-zero/infinity и arithmetic A-rules. В этих visitors нет отдельной нормативной обработки `AndAlso`, `OrElse`, `Not` или `ConditionalExpression`.

`ExpressionStructuralComparer` умеет сравнивать `AndAlso` и `OrElse`, но сравнение узлов не является их редукцией.

## Вывод

Нужен отдельный **LogicalReductionVisitor**, подключённый в нормативный pipeline отдельной стадией. Он должен быть ограничен безопасными structural identities и не исполнять пользовательские lambdas/method calls. Для short-circuit semantics нельзя безусловно преобразовывать `False && rhs` или `True || rhs` в константу, если `rhs` может иметь observable side effects или бросать исключение; reducer должен иметь явную policy boundary. Начальная безопасная область: constant folding, Boolean identities с доказанной short-circuit equivalence, redundant conditional branches и устранение одинаковых branches.

## Контрольные требования

1. Каждый новый public method получает direct regression test.
2. Visitor должен сохранять user-defined operator/method semantics.
3. RICIS extension nodes не должны разрушаться обычным `base.VisitExtension`.
4. Trace/log должен фиксировать logical stage как отдельный этап.
5. Lean/document generators должны видеть canonical reduced tree и полный node-to-root trace.
6. До и после implementation весь существующий suite обязан оставаться зелёным.
