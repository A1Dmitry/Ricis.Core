# Архитектурный дизайн: `ILog<TStage>` для Ricis.Core

**Статус:** `ПРИНЯТ ДЛЯ РЕАЛИЗАЦИИ`
**Связанная задача:** [`RICIS_TYPED_PROOF_LOG_AGILE.md`](RICIS_TYPED_PROOF_LOG_AGILE.md)

## Решение архитектора

Вводится canonical typed event journal. `ILog<TStage>` не выбирает формат документа и не выполняет expression tree. Его generic-параметр фиксирует **фактический источник события**: proof orchestration, конкретный visitor или solver handler.

```text
Proof orchestration ─── ILog<RicisProofOrchestrationStage>
                            │ For<IdentityReductionVisitor>()
                            ▼
                         ILog<IdentityReductionVisitor>
                            │ For<AlgebraicReductionVisitor>()
                            ▼
                         ILog<AlgebraicReductionVisitor>

         одна упорядоченная sequence событий
                    │              │               │
                    ▼              ▼               ▼
                  JSON           LaTeX       Lean-oriented report
```

Все три отчёта читают одни и те же immutable `RicisLogEntry`. Они не запускают proof pipeline повторно и не получают доступ к исходным conditions, constraints, claim или expected кроме безопасных текстовых snapshot, уже зафиксированных логом.

## Контракт

| Тип | Ответственность |
|---|---|
| `ILog<TStage>` | Typed facade: `Info`, `Warning`, `Exception`, `Trace`, `For<TNextStage>`, snapshot |
| `RicisProofLog<TStage>` | Thread-safe in-memory implementation; child facades разделяют один event journal |
| `RicisLogEntry` | Immutable canonical event: sequence, UTC timestamp, severity, event code, message, source stage, optional before/after expression display, exception data and attributes |
| `RicisLogSeverity` | `Info`, `Warning`, `Exception`, `Trace` |
| `RicisProofLogFormat` | Независимый от proof-document enum: `Json`, `Latex`, `Lean` |
| `RicisProofLogReportRenderer` | Единственная DRY dispatch point для format renderer-ов |
| `IRicisProofLogRenderer` | Renderer adapter, работающий только с `IReadOnlyList<RicisLogEntry>` |

## Ключевые инварианты

| ID | Инвариант |
|---|---|
| D-01 | Каждый entry получает монотонный `Sequence`; порядок определяется им, а не timestamp. |
| D-02 | `StageType` пишется из `typeof(TStage)`; дочерние stage-facade используют общий journal. |
| D-03 | `Trace` хранит `Before` и `After` как render-safe string snapshots, не как исполняемые delegate. |
| D-04 | `Exception` фиксирует type/message/stack trace, но не делает exception proof-успехом. Pipeline сохраняет прежнее исключение. |
| D-05 | JSON, LaTeX и Lean renderer получают один snapshot и не меняют journal. |
| D-06 | Lean renderer формирует только комментарий-отчёт, а не Lean theorem/source. Он явно содержит `NOT KERNEL VERIFIED`. |
| D-07 | Existing `StringBuilder` остаётся compatibility sink. Он не будет вторым событийнo-логическим путём. |

## Реальное типизированное подключение pipeline

`RicisPhasePipeline` получает additive overload `SimplifyWithLog<TLogStage>(Expression, ILog<TLogStage>)`. Внутри список visitors заменяется internal stage wrappers:

| Pipeline stage | Typed child log |
|---|---|
| identity | `ILog<IdentityReductionVisitor>` |
| polar | `ILog<PolarTrigVisitor>` |
| structural algebra | `ILog<AlgebraicReductionVisitor>` |
| O(1) bridge | `ILog<LimitBridgeVisitor>` |
| singular transform | `ILog<RicisTransformVisitor>` |
| type consistency | `ILog<TypeConsistencyVisitor>` |
| standard operations | `ILog<StandardOperationsVisitor>` |
| author annotation | `ILog<AuthorAnnotatedExpression>` |

Wrapper знает compile-time visitor type; поэтому `TStage` не выводится из string и не превращается в декоративный runtime name.

## Совместимость proof API

Existing `Prove(..., StringBuilder)` остаётся без изменения результата. Additive overload получает `ILog<RicisProofOrchestrationStage>` и передаёт его в `SimplifyWithTraceAndLog`. Existing overload может использовать `StringBuilder` как прежний document protocol, а новый лог публикует структурные события параллельно. На следующем инкременте `StringBuilder` будет получать text из event-backed adapter, но этот шаг не должен менять public formatting output без отдельной regression matrix.

## Отчёты

| Формат | Назначение | Особенности |
|---|---|---|
| JSON | Машинный audit trail | Sequence, severity, stage type, event code, attributes, trace snapshots, exception metadata |
| LaTeX | Human-readable technical appendix | Экранирует TeX-special characters, выводит таблицу событий и trace blocks |
| Lean | Review report рядом с Lean workflow | Только comment block; `NOT KERNEL VERIFIED`; arbitrary C# expression не превращается в theorem |

## Отложенные решения

Первый инкремент не меняет `RicisProofDocumentFormat` и не пытается считать `Lean` обычным proof-document форматoм для generic C# expression. Mapping event reports в existing document API делается после того, как журнал, renderer-ы и pipeline trace подтвердятся regression suite.
