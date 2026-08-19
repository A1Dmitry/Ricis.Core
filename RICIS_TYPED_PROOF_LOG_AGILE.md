# Agile-задача: типизированный журнал proof-процесса

**Статус:** `ВЫПОЛНЕНО ЛОКАЛЬНО — ОЖИДАЕТ CI/PUBLICATION`
**Приоритет:** высокий
**Дата постановки:** 2026-08-19
**Владелец решения:** Ricis.Core

## Цель

Заменить неструктурированное накопление proof-текста через `StringBuilder` на DRY-подсистему типизированного журнала `ILog<TStage>`. Журнал должен фиксировать полную последовательность работы proof-процесса: информационные события, предупреждения, исключения и трассировку. Тип `TStage` обязан нести фактический тип этапа, visitor или handler, который сформировал событие.

## Деловая и техническая ценность

Текущий proof-процесс получает входные expression tree и строит текстовую трассу. Требуется сохранить эту trust boundary, но сделать трассу структурированной и многоканальной: один и тот же набор событий должен рендериться в JSON, LaTeX и Lean-oriented report без дублирования proof-логики.

> `RicisProofDocumentFormat` остаётся форматом представления. Он не должен подменять тип proof-процесса, статус доказательства или структуру исходных expression tree.

## Scope первого инкремента

| Область | Требование | Критерий готовности |
|---|---|---|
| Contract | Ввести `ILog<TStage>` и typed event model | Есть единый API `Info`, `Warning`, `Exception`, `Trace`; каждое событие содержит timestamp/sequence, severity, stage type и payload |
| DRY | Отделить capture событий от JSON/LaTeX/Lean rendering | Добавление нового renderer не требует изменения visitor/handler и proof derivation |
| Pipeline | Интегрировать трассировку в реальный proof pipeline | По крайней мере один proof path публикует этапы visitors/handlers и RICIS phase trace |
| Formats | Реализовать JSON, LaTeX и Lean-oriented report | Все три отчёта создаются из одного журнала; Lean report не заявляет kernel verification для arbitrary C# expression tree |
| Compatibility | Сохранить существующие `StringBuilder` overloads | Старые public методы продолжают работать через adapter, без silent fallback и изменения derived expression |
| Tests | Добавить regression matrix | Проверяются порядок, stage type, warnings/exceptions, три renderer-а, неизменность expression tree и trust boundary |

## Ограничения

| ID | Инвариант |
|---|---|
| L-01 | `conditions`, `constraints`, `claim` и `expected` остаются expression tree; журнал не компилирует и не исполняет их. |
| L-02 | `ILog<TStage>` типизирует фактический источник события, а не формат документа. |
| L-03 | JSON, LaTeX и Lean report получают одинаковую canonical event sequence. |
| L-04 | Generic C# expression tree нельзя выдавать за Lean kernel proof. |
| L-05 | Existing proof API остаётся source-compatible; `StringBuilder` — только compatibility adapter, не второе proof-ядро. |
| L-06 | Enum формата валидируется до дорогих вычислений и не должен обходить общий renderer boundary. |

## Agile-очередь по ролям

| Порядок | Роль | Невыполненная работа | Артефакт выхода |
|---:|---|---|---|
| 1 | Эксперт по постановке | Инвентаризировать proof stages, visitor/handler types и пересечения текущих overloads | Контрактная карта и acceptance matrix |
| 2 | Архитектор/Fullstack C# | Спроектировать `ILog<TStage>`, canonical event model, sink и renderer boundaries | API-design без дублирования |
| 3 | Разработчик | Реализовать базовый журнал, compatibility adapter и JSON/LaTeX/Lean renderer-ы | Компилируемый Core patch |
| 4 | QA | Написать и запустить unit/regression tests на события, порядок, форматы, invalid enum и trust boundary | Протокол тестов |
| 5 | Release/DevOps | Выполнить build, regression suite, diff check, commit/push/tag по принятой версии | Проверяемая публикация |

## Результат локального Agile-цикла

| Роль | Статус | Подтверждение |
|---|---|---|
| Постановка | выполнено | Контракт и ограничения зафиксированы в этом документе. |
| Архитектор/Fullstack C# | выполнено | Реализованы `ILog<TStage>`, canonical event journal, typed child stages и DRY renderer boundary. |
| Разработчик | выполнено | Pipeline и unary generic proof API получили additive typed-log path; существующий `StringBuilder` API сохранён. |
| QA | выполнено локально | `322` regression tests прошли; JSON/LaTeX/Lean reports, stage types, exception trace и deferred-expression boundary покрыты TLOG01–TLOG04. |
| Release/DevOps | ожидается | Нужны финальный diff check, GitHub CI и публикация v0.2.0. |

## Definition of Done

Задача считается полностью опубликованной только после того, как один canonical proof run порождает типизированную последовательность событий с источником каждого этапа, а JSON, LaTeX и Lean-oriented report воспроизводимо строятся из этой одной последовательности. Existing `StringBuilder` API сохраняет source compatibility, а новый typed-log path не создаёт второй proof engine. Полный `.NET` regression suite, API smoke check и Lean verification должны пройти на runtime/SDK .NET 8 или совместимой CI-среде перед публикацией.
