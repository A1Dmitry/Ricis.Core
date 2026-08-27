# RICIS semantic report vertical slice — evidence

## Scope

Реализован первый вертикальный срез архитектуры специализированных отчётов. `ILog<TSender>` остаётся backward-compatible transport entrypoint, а report pipeline теперь строит собственные semantic models по sender type, severity, event code, attributes, message и exception payload.

## Production components

Добавлен `RicisSemanticEventClassifier`, который классифицирует события в `Lifecycle`, `ProofStep`, `TechnicalTransformation`, `Warning`, `HandledException` или `Unclassified` и назначает visibility policy. Добавлен `RicisSemanticReportModelFactory` с независимыми `RicisTextReportModel` и `RicisAcademicReportModel`.

Text model сохраняет полный diagnostic payload: severity, sender, event code, phase, message, before/after snapshots, exception type и exception trace. Academic model получает только семантически отобранные proof steps и public limitations; технический Trace payload в него не переносится.

Добавлен внешний `RicisFileReportTemplateSource` со specific-culture -> neutral-culture -> default fallback. Template files хранятся отдельно от C# в `Logging/Templates/` и копируются в output/package. `RicisSafeReportTemplateRenderer` принимает только плоские model projections и allowlisted collection blocks; шаблоны не получают `ILog`, journal, visitor или arbitrary runtime object.

## External templates

`text.en-US.template` выводит полный технический Text Log, включая before/after и exception type. `academic.en-US.template` выводит title, status, proof steps, limitations и conclusion без технических expression snapshots.

## QA cases

| Test | Проверка |
|---|---|
| SEM01 | Sender type, severity, event code и phase/rule attributes влияют на classification |
| SEM02 | Text model содержит Trace, Academic model его исключает |
| SEM03 | Внешние Text/Academic templates создают независимые artifacts; Trace leakage отсутствует |
| SEM04 | Exception сохраняет техническую причину в Text и public explanation в Academic limitation |
| SEM05 | `null` logger не меняет вычислительный результат |
| SEM06 | Unknown sender/event не становится доказательным Academic step |

Во время QA были устранены реальные дефекты: resource lookup mismatch, отсутствие named collection support (`Steps`), необработанный `Limitations` block и потеря before/after полей в Text template. Временные diagnostics удалены.

## Quality gate

| Проверка | Результат |
|---|---:|
| Solution build | PASS, 0 warnings / 0 errors |
| Core regression | 404/404 PASS |
| Numerics UnitTests | 124/124 PASS |
| Finance regressions | 19/19 PASS |
| `git diff --check` | PASS |
| Deleted files | 0 |
| Full gate marker | `SEMANTIC_REPORT_FULL_GATE_PASS` |

## Architectural boundary

Это первый vertical slice, а не завершение всей migration. JSON, LaTeX и Lean по-прежнему требуют отдельных semantic models/pipelines вместо прямого универсального journal projection. Данный increment доказывает contract на Text/Academic separation и задаёт основу для следующих report-specific implementations.
