# JAC-001 — замкнутый proof-спринт: сингулярный якобиан, LaTeX и Lean

**Статус:** `ЗАВЕРШЁН`  
**Релиз:** `v0.6.0`.

## Постановка

Нужно было проверить полный существующий Core proof-path на одном сингулярном примере якобиана. Пример задаёт rank-one Jacobian

\[
J = \begin{pmatrix} 1 & 1 \\ 1 & 1 \end{pmatrix}, \qquad \det J = 1\cdot1 - 1\cdot1 = 0.
\]

В Core determinant передаётся в `RicisJacobianSingularityExpression<double>` как уже сертифицированный structural zero, что соответствует назначению типа: он не строит классический inverse, а хранит `0_{\det J}` и отложенный inverse payload. Скалярная часть proof использует реальную lambda-координату `d = \det J`, claim `d => d / d`, ожидаемую lambda `d => 1` и structural verification lambda `derived(d) == expected(d)`.

> Сценарий проверяет **символический вывод RICIS**, а не объявляет сингулярную матрицу классически обратимой. Ни conditions, ни constraints, ни claim не компилируются и не исполняются при доказательстве.

## Архитектурное решение

| Уровень | Реализация |
|---|---|
| Посылки | Несколько независимых `Expression<Func<double,bool>>`: `d = 0`, `d² = 0` и отдельное ограничение области `d ≥ 0`. |
| Тезис | Реальные lambda-expression trees: claim `d => d / d`, expected `d => 1` и verification `derived == expected`. |
| Solver | Канонический `RicisAcademicProofExtensions` и `RicisPhasePipeline`; L1/SP2 структурно выводит `d/d → 1`. |
| Полный trace | Один proof-run получает injected `ILog<RicisProofOrchestrationStage>` и возвращает node-to-root phases, visitor/handler events и verification event. |
| Jacobian bridge | `RicisJacobianSingularityExpression<double>` применяет A6 покомпонентно, сохраняя payload и не создавая классический `NaN`/inverse. |
| LaTeX | Документ формируется из того же checked proof trace как самостоятельный `\documentclass`-документ с UTF-8/Cyrillic preamble. |
| Lean | Первый блок — комментарий-аудит полного RICIS trace; второй — структурный A6 theorem из `RicisLeanTemplate`. Первый не выдаётся за kernel theorem; второй компилируется Lean. |

## Выполненные изменения

| Роль | Результат |
|---|---|
| Постановка | Добавлен `JACOBIAN_PROOF_SPRINT.md` с границами proof scope, Definition of Done и запретом на подмену audit scaffold формальной теоремой. |
| Архитектор | Добавлен `RicisCheckedProofArtifacts<T>` и единый `ProveDocumentsCheckedWithLog`: один solver pass формирует checked result, immutable typed trace и несколько document exports. |
| Разработчик | Добавлен `RicisJacobianProofScenario` с независимыми lambda-посылками, lambda-тезисом, A6 bridge, standalone LaTeX и combined Lean artifact. В `Ricis.Console` добавлены команды `--jacobian-proof-demo`, `--jacobian-proof-latex` и `--jacobian-proof-lean`. |
| QA | Добавлена suite `RicisJacobianProofArtifactSuite` и четыре регрессии: реальные lambda inputs, единственный canonical run с полным trace, standalone exports, A6 payload без классического inverse. |
| DevOps | GitHub Actions теперь устанавливает минимальный LaTeX validator, компилирует JAC-001 PDF и компилирует combined Lean artifact; `sorry`, `sorryAx`, LaTeX fatal errors и overfull trace lines блокируют CI. |

## Исправленный дефект LaTeX

Первая реальная компиляция `pdflatex -halt-on-error` обнаружила, что Unicode-символ `∞` в `verbatim` trace не поддерживается базовым pdfLaTeX. Этот дефект устранён: математические Unicode-символы детерминированно нормализуются в trace, а в обычном LaTeX-тексте экранируются как математические команды. После этого были обнаружены overfull строки typed-log; они устранены детерминированным переносом длинных строк внутри `verbatim`, без изменения исходного proof trace.

## Итоговый quality gate

| Проверка | Фактический результат |
|---|---|
| Release build | `0` warnings, `0` errors. |
| Core regressions | `328/328` passed, включая `JPR01`–`JPR04`. |
| Finance regressions | `9/9` passed. |
| Единая версия | Все solution-проекты сообщили `0.6.0`. |
| LaTeX | JAC-001 скомпилирован `pdflatex -halt-on-error` в пятистраничный PDF без fatal diagnostics и без `Overfull \hbox` trace warnings. |
| Lean | Combined audit-plus-A6 artifact скомпилирован `lake env lean`; `sorry` и `sorryAx` отсутствуют. |

## Границы формальной силы

`RicisProofDocumentFormat.Lean` остаётся audit scaffold для произвольных C# expression trees: он записывает воспроизводимый trace в Lean-комментарий, но не превращает `Expression.ToString()` в theorem statement. В данном спринте kernel-checked является именно структурный A6 блок `RicisLeanTemplate`. Это разграничение сохранено в API, artifact и CI.
