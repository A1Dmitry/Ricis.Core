# RICIS JSON semantic report increment — evidence

## Contract

JSON pipeline реализован как независимый semantic artifact с schema `ricis-semantic-report/v1` и `reportType=json-semantic`. Он не сериализует `RicisLogEntry` напрямую и не экспортирует технические before/after snapshots, raw exception trace или CLR runtime objects.

## Implementation

`RicisJsonReportModelFactory` классифицирует события через `RicisSemanticEventClassifier` и строит плоский `RicisJsonReportDocument`. Каждый event содержит sequence, semantic kind, visibility, sender short name, phase, event code, public message и status. Handled exception получает exception type, handling status и public cause; полный stack trace остаётся только Text Log.

`RicisJsonReportSerializer` проверяет version, strict sequence order и сериализует только semantic document model. Используется source-generated `System.Text.Json` context, поэтому build не получает AOT/trimming warnings.

Внешний schema asset `Logging/Templates/ricis-semantic-report.v1.schema.json` копируется в output и package. Он фиксирует `schema`, `reportType`, `kernelVerification=false`, event required fields и запрещает дополнительные raw-journal properties.

## QA

| Test | Acceptance |
|---|---|
| JSON01 | versioned schema, report type, kernel boundary и sender projection |
| JSON02 | public exception cause сохраняется; before/after, exceptionTrace и raw expression не раскрываются |
| JSON03 | sequence order сохраняется; unknown event получает `unclassified` |
| JSON04 | внешний schema asset опубликован и совпадает с serializer contract |

Во время QA устранён реальный defect в case-sensitive exception assertion и устранены AOT warnings переходом на source-generated metadata.

## Gate

| Check | Result |
|---|---:|
| Solution build | PASS, 0 warnings / 0 errors |
| Core regressions | 408/408 PASS |
| Numerics UnitTests | 124/124 PASS |
| Finance regressions | 19/19 PASS |
| `git diff --check` | PASS |
| Deleted files | 0 |
| Gate marker | `JSON_REPORT_FULL_GATE_PASS` |

Следующий отдельный increment: независимая semantic LaTeX pipeline. Текущая legacy LaTeX renderer не считается заменённой JSON implementation; JSON increment закрыт в пределах указанного contract.
