# Бизнес-план: сквозное логирование нормализации RICIS III

## Цель

Каждый запуск нормализации должен иметь единый node-to-root audit trail, пригодный для пошагового доказательства и объяснения. Все существующие public и internal вызовы сохраняют прежнее поведение, если последний необязательный параметр `ILog<TStage>` равен `null`. При передаче журнала каждый слой публикует событие до/после своей попытки, включая структурно неизменённые и пропущенные фазы.

## Контракт совместимости

`ILog<TStage>` добавляется только последним параметром метода и имеет nullable default `null`. Значение `null` означает «логирование отключено» и не должно менять дерево, порядок фаз, исключения или вычисленный результат. Ненулевой журнал использует общий canonical journal через `For<TNextStage>()`; тип стадии отражает фактический visitor/handler/orchestrator.

## Обязательные слои и события

| Порядок | Слой RICIS III | Текущий компонент | Обязательное событие |
|---:|---|---|---|
| 0 | Orchestration boundary | `RicisPhasePipeline.SimplifyCore` | `RICIS_PIPELINE_START`, `RICIS_PIPELINE_COMPLETE` |
| 1 | Type/identity normalization | `TypeConsistencyPhase`, `IdentityReductionVisitor` | phase start/complete/trace/skip/exception |
| 2 | Standard algebraic operations | `StandardOperationsPhase`, `StandardOperationsVisitor` | phase start/complete/trace/skip/exception |
| 3 | Logical reduction | `LogicalReductionVisitor`, Quine–McCluskey layer | phase start/complete/trace/skip/exception |
| 4 | Limit/singularity bridge | `LimitBridgeVisitor`, A1/A4/A6 handling | phase start/complete/trace/skip/exception |
| 5 | Vector/matrix and special expression traversal | corresponding visitors | child stage events with full node-to-root snapshots |
| 6 | Proof/document orchestration | `RicisAcademicProofExtensions` | proof start, verification, complete, document export |

## Acceptance criteria

Система считается принятой, если при ненулевом log snapshot содержит ordered events для каждой реально запущенной фазы, а при `null` результаты и существующие tests остаются неизменными. Для каждой trace event должны быть доступны before/after snapshots; для skipped и exception events причина должна быть записана. Tex/JSON/Log exporters должны получать один и тот же snapshot и не запускать pipeline повторно.

QA обязан проверить: отсутствие лога не изменяет результат; ненулевой log получает все phase events; child stage types не теряются; trace sequence строго возрастает; `RICIS_PHASE_SKIPPED` и `RICIS_PHASE_EXCEPTION` сохраняются; node-to-root before/after присутствуют; все существующие public overloads продолжают компилироваться.

## Порядок исполнения ролей

Бизнес-аналитик фиксирует карту слоёв и event contract. Программист изменяет только существующие orchestration/phase boundaries по DRY, добавляя optional log последним параметром и не дублируя solver. QA добавляет отдельные тесты на каждый новый overload и на каждую новую логическую ветвь. После полного gate DevOps фиксирует evidence и выполняет commit/push.
