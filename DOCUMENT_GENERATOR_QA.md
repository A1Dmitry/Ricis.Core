# QA-спринт: генераторы proof-документов

**Проект:** `Ricis.Core`  
**Роль:** QA-эксперт по RICIS и C#-архитектор  
**Критерий:** production-grade quality с приоритетом семантического ядра RICIS, DRY, SOLID и DDD  
**Статус:** исправление завершено; локальный quality gate и внешние LaTeX/Lean gates пройдены; готово к публикации v0.7.0.

## 1. Цель и границы

Спринт проверяет, что Log, Academic, JSON, LaTeX и Lean document paths используют один уже вычисленный RICIS derivation, не выполняют пользовательские lambda-условия во время proof/rendering и не смешивают audit report с kernel-checkable Lean theorem. Рендереры относятся к presentation layer: они не должны изменять expression tree, повторно запускать visitor pipeline или самостоятельно интерпретировать математическую семантику.

Главный RICIS trust boundary зафиксирован следующим образом:

> Generic C# expression tree может быть записан в audit/document report, но не становится Lean theorem только потому, что его текст помещён в `.lean`-файл.

Для неподдержанной формы Lean генератор обязан отказать явно через `RicisUnsupportedLeanProofShapeException`. Kernel-checkable source строится только через структурированный `RicisLeanTemplate`, валидированные Lean identifiers и конечный набор `RicisLeanProofRow`.

## 2. Найденный дефект и ремонт

До ремонта `RicisProofDocumentTemplates` для `RicisProofDocumentFormat.Lean` создавал generic scaffold с комментариями и пустым namespace. Отдельный JAC path затем вручную склеивал этот audit scaffold со structured A6 theorem. Такая форма была compilable, но архитектурно опасной: внешний вид Lean-файла мог быть принят за формальное доказательство, а два различных слоя вывода объединялись строковой операцией.

Ремонт выполнен без дублирования proof logic. Generic Lean document factory теперь выполняет controlled rejection и сообщает, что нужно использовать `RicisLeanTemplate.Render`. Typed log по-прежнему имеет отдельный `RicisProofLogFormat.Lean`, но его контракт явно comment-only и `NOT KERNEL VERIFIED`. JAC structured theorem экспортируется самостоятельно, а audit report остаётся отдельным результатом того же canonical log snapshot.

Удалён ставший недостижимым generic Lean comment helper. Academic, Log, JSON и LaTeX продолжают получать один и тот же derivation через существующий injected factory path.

## 3. QA-матрица

| ID | Проверка | Ожидаемый результат | Статус |
|---|---|---|---|
| DOC-01 | Log factory | Сохраняет scope, theorem, definitions, normative steps и node-to-root trace | PASS |
| DOC-02 | Academic factory | Применяет внешний `Func<string,string>` только к готовому документу | PASS |
| DOC-03 | JSON factory | Выдаёт валидный JSON с derivation, derived, profile и limitations | PASS |
| DOC-04 | LaTeX factory | Выдаёт standalone UTF-8/Cyrillic document с закрытыми окружениями | PASS |
| DOC-05 | Generic Lean factory | Отклоняет unsupported C# proof shape; scaffold не создаётся | PASS |
| DOC-06 | Structured Lean factory | Создаёт только валидируемые theorem rows без `sorry` | PASS |
| DOC-07 | Typed-log JSON | Рендерит immutable ordered snapshot и явно сообщает `kernelVerification=false` | PASS |
| DOC-08 | Typed-log LaTeX | Экранирует special characters и сохраняет sequence/stage/message | PASS |
| DOC-09 | Typed-log Lean | Создаёт comment-only audit report без theorem declarations | PASS |
| DOC-10 | Factory injection | Формат выбирается один раз; renderer не запускает solver и visitor повторно | PASS |
| RICIS-01 | Conditions/constraints | Не компилируются и не исполняются на proof path | PASS |
| RICIS-02 | Single canonical run | Checked result, trace и exports получают один derivation | PASS |
| RICIS-03 | L1/SP2/A6 | Renderer не меняет RICIS expression semantics | PASS |
| RICIS-04 | Lean trust boundary | Generic text не объявляется kernel theorem | PASS |
| JAC-01 | Structured JAC Lean | A6 theorem отделён от typed audit | PASS |
| EXT-01 | `pdflatex` | Реальный standalone artifact компилируется без fatal errors и `overfull \\hbox` | PASS |
| EXT-02 | Lean compiler | Сгенерированный structured JAC artifact компилируется без `sorry`/`sorryAx` | PASS |

## 4. Проверенные результаты

Baseline до ремонта: `328/328` Core regression tests проходили, но один из тестов закреплял нежелательный generic Lean scaffold. После ремонта этот контракт заменён на controlled rejection, а JAC regression разделяет structured Lean и comment-only audit.

Финальный локальный результат:

| Gate | Результат |
|---|---:|
| Release solution build | PASS, 0 warnings, 0 errors |
| Core regressions | `328/328` PASS |
| Finance regressions | `9/9` PASS |
| Standalone LaTeX | `pdflatex -halt-on-error` PASS |
| LaTeX fatal/overfull scan | PASS |
| Structured Lean | `lake env lean` PASS |
| Forbidden markers | `sorry` и `sorryAx` не обнаружены |

## 5. Архитектурные критерии приемки

Решение считается принятым только если document layer не содержит повторной derivation logic, не вызывает пользовательские delegates и не утверждает формальную Lean-проверку для generic C# expression tree. Structured Lean расширения должны вводиться через новые типизированные proof rows или отдельный bounded bridge, а не через интерполяцию произвольного текста.

LaTeX является presentation artifact и может содержать audit trace, но его успешная компиляция не равна доказательству математического утверждения. Lean comment report является review artifact. Kernel-checked статус разрешён только для source, который сгенерирован structured template и реально скомпилирован Lean.

## 6. Следующий технический шаг

Перед release необходимо выполнить удалённый CI с теми же двумя внешними gates. Если проект публикует NuGet, публикация должна выполняться отдельным workflow только при наличии настроенного `NUGET_API_KEY`; отсутствие credential не должно маскироваться как ошибка генератора документов.

## 7. Изменённые области

Изменены generic Lean factory boundary, публичное описание `RicisProofDocumentFormat.Lean`, JAC artifact separation и regression expectations. Полный proof engine, typed-log ordering и RICIS phase pipeline не переписывались.

Этот отчёт является QA-артефактом спринта и не заменяет formal Lean theorem. Он фиксирует границы, проверяемые свойства и фактические результаты тестов.

---

**Definition of Done:** код собирается в Release без warnings/errors; Core и Finance regressions проходят; generic unsupported Lean shape отклоняется; structured Lean компилируется; standalone LaTeX компилируется без fatal/overfull ошибок; audit report не выдаётся за theorem; рабочее дерево чистое после публикации.

---

Автор: **Manus AI**

## References

[1]: `RICIS_III_CONCEPT.md` — канонический порядок RICIS phases, L1, SP2, SP4, A6 и trust boundary.
[2]: `Proofs/RicisLeanProofModels.cs` — structured Lean rows и `RicisUnsupportedLeanProofShapeException`.
[3]: `Logging/RicisProofLogReportRenderer.cs` — отдельные JSON/LaTeX/Lean typed-log report contracts.
[4]: `RegressionTests/RicisProofDocumentFormatSuite.cs` — format factory regression matrix.
[5]: `RegressionTests/RicisTypedProofLogSuite.cs` — single-snapshot и no-delegate-execution invariants.

Метки `[1]`–`[5]` ссылаются на файлы текущего репозитория и используются как локальные нормативные источники QA-отчёта.

[1]: ./RICIS_III_CONCEPT.md
[2]: ./Proofs/RicisLeanProofModels.cs
[3]: ./Logging/RicisProofLogReportRenderer.cs
[4]: ./RegressionTests/RicisProofDocumentFormatSuite.cs
[5]: ./RegressionTests/RicisTypedProofLogSuite.cs

## Примечание о релизе

Версия solution повышена до `0.7.0`, поскольку generic Lean scaffold заменён на controlled rejection и изменился контракт unsupported proof shape. Release tag создаётся после проверки GitHub diff и публикации коммита.

## Закрытие QA

Локальные gates пройдены. Удалённый CI должен повторить Release build, Core/Finance regressions и внешние LaTeX/Lean checks; его фактический URL добавляется в итоговый release message после публикации.
