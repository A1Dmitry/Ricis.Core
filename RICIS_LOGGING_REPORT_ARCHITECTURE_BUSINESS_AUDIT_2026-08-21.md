# Бизнес-анализ архитектуры логирования и отчётов RICIS

## Цель

Определить, как каждый тип отчёта должен интерпретировать событие в зависимости от `EventCode`, severity, CLR-типа отправителя, атрибутов, текста сообщения и данных исключения. Технический `Trace` не должен автоматически попадать в академический, JSON, LaTeX или Lean output: это самостоятельный поток обычного технического лога.

## Первичный QA-аудит текущего состояния

Текущий `ILog<TStage>` типизирует отправителя и передаёт `Info`, `Warning`, `Exception`, `Trace`. `RicisProofLog<TStage>` сейчас является общим thread-safe journal facade: он сохраняет событие, но не выполняет sender-aware semantic classification. `For<TNextStage>()` меняет metadata отправителя, но не меняет правила интерпретации события.

Текущий `RicisProofLogReportRenderer` принимает один `IReadOnlyList<RicisLogEntry>` и напрямую отдаёт его в три renderer-а. JSON сериализует универсальную запись; LaTeX печатает severity/stage/event/message и Trace before/after; Lean печатает все события как audit comments. Это технически единообразно, но не соответствует требованию, что каждый report class сначала анализирует событие и строит собственную модель. Особенно критичен автоматический leak Trace payload в LaTeX и Lean.

## Бизнес-контракт по слоям

| Вид результата | Источник данных | Собственная модель | Что публикуется |
|---|---|---|---|
| Обычный Text Log | Все события, включая Trace | Ordered diagnostic record | Полный технический маршрут, before/after, sender и exception details |
| Academic Report | Info, selected Warning, semantically accepted proof events | Definitions, assumptions, obligations, derivation steps, conclusion | Только отобранные доказательные сведения; Trace не публикуется автоматически |
| JSON Report | Classified events and normalized metadata | Versioned event/document schema | Машинная модель конкретного JSON contract, а не универсальный dump |
| LaTeX Report | Academic model plus selected explanatory data | Sections, derivation blocks, tables, theorem/limitation blocks | Читаемый академический Tex; Trace only by explicit technical appendix option |
| Lean Report | Explicit theorem/proof model and separately marked audit comments | Lean declarations, theorem dependencies, kernel-status metadata | Только допустимые Lean artifacts; generic Trace не превращается в theorem |

## Правила sender-aware classification

Класс обработки обязан анализировать CLR sender type и его attributes. `StageType` является маршрутом к семантике отправителя, а не только строкой для отображения. Для каждой комбинации sender/event требуется registry или strategy:

| Отправитель/признак | Семантическая роль |
|---|---|
| Normalization visitor | Применённое правило, precondition, before/after, changed/skipped |
| Solver/orchestrator | Этап решения, извлечённые коэффициенты, elimination, derived claim |
| Proof verifier | Проверка expected/conditions/constraints и статус verification |
| Renderer/document builder | Построение представления, формат и artifact status |
| Exception-capable stage | Причина, recoverability, rethrow/handled status и affected phase |
| Unknown sender | Warning/diagnostic; не должен автоматически попадать в academic derivation |

Текст сообщения и attributes не должны рассматриваться как декоративные строки. Они являются входом классификатора: `ruleFamily`, `phaseName`, `changed`, `wasSkipped`, `branch`, `equationCount`, `claim`, `exceptionType` и другие ключи должны преобразовываться в типизированные поля специализированной модели.

## Правила обработки исключений

Для обработанного исключения report class обязан определить причину и статус обработки: `Observed`, `Handled`, `Rethrown`, `ConvertedToWarning`, `Fatal`. Одного `ExceptionTrace` недостаточно. Событие должно включать sender stage, event code, phase, exception type, message, inner exception chain, recoverability и решение обработчика. Academic report публикует только релевантное объяснение сбоя; Text Log сохраняет полный trace; Lean output не должен вставлять необработанный stack trace в theorem body.

## Обязательные архитектурные выводы

1. Универсальный `RicisLogEntry` может оставаться transport envelope, но не должен быть финальной report model.
2. Нужен semantic classifier, который принимает envelope и выдаёт typed semantic event либо `UnclassifiedEvent`.
3. Каждый report builder должен иметь собственную projection/model-building логику.
4. Trace должен быть доступен Text Log и явно включаемому technical appendix, но не должен автоматически экспортироваться в Academic/LaTeX/Lean.
5. JSON обязан иметь собственную versioned schema; прямой сериализованный dump canonical entry следует считать compatibility/debug format, а не полноценным domain report.
6. QA обязан проверять не только наличие события, но и правильность классификации sender + event + attributes + message + exception.

## Acceptance criteria следующей реализации

| ID | Требование |
|---|---|
| LOG-BA-01 | Один и тот же входной event sequence создаёт разные специализированные модели для Text, Academic, JSON, LaTeX и Lean |
| LOG-BA-02 | Trace присутствует в Text Log и отсутствует в Academic/LaTeX/Lean без explicit option |
| LOG-BA-03 | Sender type классифицирует этап и влияет на report model |
| LOG-BA-04 | Attributes становятся typed semantic fields, а не только string dictionary |
| LOG-BA-05 | Exception report фиксирует причину и обработку исключения |
| LOG-BA-06 | Academic report содержит только доказательные steps и side conditions |
| LOG-BA-07 | LaTeX строится из собственной document model, не конкатенацией общего journal |
| LOG-BA-08 | Lean report различает kernel-verifiable artifact и audit commentary |
| LOG-BA-09 | Null optional logger полностью отключает обработку и не меняет computation |
| LOG-BA-10 | Для каждого нового public logger/report method есть direct QA test |

## Статус

Это бизнес-аналитическая спецификация и результат первичного QA-аудита. Реализацию нельзя начинать как локальный formatter refactor до завершения инвентаризации всех sender stage types, текущих report tests и exception paths.
