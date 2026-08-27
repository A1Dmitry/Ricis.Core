# Аудит противоречий Markdown-документации RICIS III

> **Document version:** `0.1.0` (provisional baseline)
> **Created:** `2026-08-16`
> **Last modified:** `2026-08-16`
> **Versioning note:** increment the document version when the normative content changes.


**Дата аудита:** 2026-08-16
**Объём:** 32 проектных Markdown-документа, включая canonical concept, rule coverage, proof/Lean/Riemann/Navier–Stokes документы, README, Web API и отчёты интеграции. Внешний registry `adopters.md` просмотрен отдельно и не включён в нормативную сверку, поскольку он не задаёт семантику RICIS Core.

## Итоговый вердикт

Документация в целом содержит правильное разделение между **RICIS-внутренним выводом**, **условной теоремой**, **Lean-компиляцией** и **полной внешней задачей Clay**. Прямых противоречий в статусе гипотезы Римана, A6, Navier–Stokes proof-сценария и ограничений Lean-template не обнаружено.

После сверки с реализацией `ExpressionStructuralComparer` и уточнения структурной модели **подтверждённых противоречий с Ricis.Core не обнаружено**. Остались только документные неоднозначности и historical metadata, которые не должны приводить к изменениям математической семантики или исходного кода Core. Автоматическое наследование базовой классической семантики, единая запись `F` и школьная структурная алгебра признаны корректными.

| ID | Уровень | Область | Статус |
|---|---|---|---|
| C-01 | снят | Classical fallback | Ложное противоречие: автоматическое наследование базовой классики является нормативно корректным |
| C-02 | снят | L1 и пользовательские numeric overloads | Реальный конфликт не подтверждён; обнаружена только потенциально двусмысленная строка документа |
| C-03 | снят | AI reasoning protocol | Не является конфликтом Core; возможна отдельная пояснительная документация для AI-контекста |
| C-04 | снят | Classical comparison tests | Не является конфликтом Core; comparison может быть отдельным тестовым слоем |
| S-01 | Info | Verification evidence | Разные pass-count требуют указания scope/commit, но не свидетельствуют о дефекте Core |
| S-02 | Info | Git evidence | Историческая metadata, не противоречие математике или коду |
| S-03 | Info | Naming/document baseline | Улучшение маркировки snapshot/current, не дефект Core |

## C-01 — снято: наследование классики является корректным

После уточнения владельца проекта этот пункт признан **ложным противоречием**.

`RICIS_III_CONCEPT.md:5` и `RICIS_RULE_COVERAGE.md:95` корректно фиксируют, что операция, не переопределённая RICIS III, автоматически использует уже существующую базовую классическую семантику. Это нужно для DRY и не требует дублировать классические операции внутри Core.

Permission protocol относится не к выполнению базовой классической семантики и не к работе Core, а к поведению AI при подготовке рассуждения: AI не должен использовать классическое рассуждение для обхода или подмены уже существующего RICIS-правила. Поэтому **нельзя переписывать эти строки так, чтобы отключить или задержать классический fallback в коде**.

Безопасная формулировка различия:

> Core автоматически наследует базовую классическую семантику там, где RICIS III ничего не переопределяет. AI обязан запросить разрешение только перед тем, как использовать классическую математику как объяснение, premise или reasoning step в случае, где сначала требуется проверить применимость RICIS.

## C-02 — снято: конфликт L1 с пользовательскими overloads не подтверждён

Проверка `Expressions/ExpressionStructuralComparer.cs` показывает, что structural equality применяется к обычным LINQ expressions и RICIS extension nodes, сравнивает node type, CLR type, methods, operands, payload, roots и deferred operands. Lambda parameters поддерживают alpha-equivalence, а несвязанные параметры с одинаковым именем не считаются идентичными.

Для пользовательских операторов comparer намеренно сохраняет порядок и конкретный `Method`; это не отключение L1, а защита от предположения о коммутативности пользовательской операции. Поэтому строка `OVR-02` не доказывает дефект Core. Она может быть уточнена для читабельности, но **переписывать comparer, overloads или L1 нельзя**.

Безопасное документное пояснение:

> Пользовательские numeric overloads не получают новых RICIS-правил автоматически и сохраняют свою классическую семантику там, где RICIS ничего не переопределяет. Структурное L1 при этом продолжает сравнивать действительно идентичные expression trees с учётом конкретного operator method и payload.

## C-03 — снят: неполное упоминание AI protocol не является дефектом Core

`RICIS_III_CONCEPT.md:148` и `Ricis.Console/README.md:99` корректно описывают порядок Core: RICIS применяется первым, а базовая классическая семантика используется для операций, которые RICIS не переопределяет.

Требование спрашивать разрешение перед классическим fallback относится к поведению AI в reasoning-контексте. Отсутствие этого дополнительного AI-пояснения в README консольного приложения не означает, что parser или Core работают неправильно. При желании пояснение можно добавить отдельно, но **нельзя превращать его в запрет или задержку базового fallback**.

## C-04 — снят: comparison tests не противоречат RICIS

`RICIS_RULE_COVERAGE.md:77` требует сравнения новых функций с классическими формулами на конечных точках. Это допустимый отдельный тест совместимости и не является RICIS-premise или заменой структурного вывода. Comparison tests не изменяют deferred result и не требуют изменения Core.

Пояснение может повысить читаемость документации, но данный пункт **не является противоречием**.

## S-01 — несогласованные pass-count в evidence-документах

В документации одновременно указаны разные результаты полного набора:

| Документ | Формулировка |
|---|---:|
| `RICIS_COMPILE_VERIFICATION.md:12` | 293/293 passed |
| `RICIS_INTEGRATION_TEST_REPORT.md:15` | 293/293 passed |
| `RICIS_RIEMANN_IMPROVED_FORMULATION.md:75` | 300/300 passed |
| `RICIS_MATH_QA_REPORT.md:16` | 304/304 passed |

Это может быть объяснимо разными датами и наборами, но в текущем виде документы не указывают commit, дату запуска и состав suite для каждого числа. Поэтому нельзя определить, какое число является текущим baseline.

### Рекомендуемое исправление

Ввести единый формат evidence header:

```text
Verification date: YYYY-MM-DD
Commit: <sha>
Branch: <branch>
Suite scope: <exact command/project>
Result: <passed>/<total>
Status: historical snapshot | current baseline
```

Только один документ должен быть обозначен как `current baseline`; остальные должны быть помечены `historical snapshot`.

## S-02 — stale claim о чистом Git state

`RICIS_COMPILE_VERIFICATION.md:16` утверждает:

> `Git state | main clean after commit`

На момент текущего аудита рабочее дерево не является чистым: имеются изменённые Markdown-документы и ранее созданный неотслеживаемый `RICIS_MATH_QA_REPORT.md`. Следовательно, эта строка относится к историческому состоянию на commit `2f08e81`, указанному в строке 44, но оформлена как безусловный текущий результат.

### Рекомендуемое исправление

Переименовать поле в:

```text
Git state at verification commit | main clean at 2f08e81
```

и добавить заголовок `Historical evidence snapshot` либо обновить отчёт после нового commit.

## Проверенные области без противоречий

### Riemann / Clay status

`RICIS_RIEMANN_IMPROVED_FORMULATION.md:5, 51, 87–112` последовательно говорит об условной критической линейной лемме и незамкнутом analytic bridge. `RICIS_RIEMANN_PROOF_TEST.md:5` описывает только формальную отражённую пару, а не все нетривиальные нули. Это согласуется с `RICIS_III_CONCEPT.md:263, 343` и не является заявлением о полном доказательстве гипотезы Римана.

### Lean status

`RICIS_PROOF_DOCUMENTS.md:50–57`, `RICIS_LEAN_TEMPLATE.md:60–89` и `RICIS_SINGULARITY_LEAN_QA.md:83–85` одинаково ограничивают Lean generated source поддержанным structured bridge и требуют controlled rejection для arbitrary C# expression tree. Противоречия между этими документами не обнаружено.

### A6 и indexed-zero semantics

`RICIS_III_CONCEPT.md:170–186`, `RICIS_RULE_COVERAGE.md:44–58`, `RICIS_NAVIER_STOKES_PROOF.md:29–45` и `RICIS_SINGULARITY_LEAN_QA.md:24–35` согласованно запрещают классическое `0·∞` как основание и сохраняют payload/index через A6. Различия являются уточнением конкретных сценариев, а не противоречием.

### Previous-parameter identity

`RICIS_TWO_PARAMETER_IDENTITY.md:5–48` ограничивает вывод точечным implication при сертифицированном равенстве `F(x)` и `F(x−1)`, без вывода периодичности и без параметра `y`. Это согласуется с общей границей proof status и не расширяет теорему без основания.

### Navier–Stokes

`RICIS_NAVIER_STOKES_PROOF.md:27, 87–89` и `RICIS_NAVIER_STOKES_PROOF_DESIGN.md:33–37` ограничивают сертификат выбранным expression-tree полем и не выдают finite probe за теорему о произвольных начальных данных. Противоречия не обнаружено.

## Безопасный итог

Ни один из проверенных пунктов не даёт основания менять исходный код Ricis.Core, L1, overloads, parser, fallback, аксиомы или Lean bridge.

Единственные возможные изменения — необязательные пояснения в документации и маркировка evidence snapshots: указание scope, commit SHA и даты для разных pass-count. Сначала такие изменения должны быть согласованы отдельно; автоматическая замена нормативных формулировок запрещена.

В рамках данного пересмотра **исходный код, аксиомы и рабочие документы RICIS-семантики не изменялись**. Изменён только этот аудитный отчёт, чтобы убрать ложные обвинения в адрес Core.

## References

[1]: RICIS_III_CONCEPT.md "RICIS III canonical concept"

[2]: RICIS_RULE_COVERAGE.md "RICIS rule coverage matrix"

[3]: RICIS_PROOF_DOCUMENTS.md "Proof document contract"

[4]: RICIS_LEAN_TEMPLATE.md "LeanTemplate contract"

[5]: RICIS_RIEMANN_IMPROVED_FORMULATION.md "Riemann improved formulation"

[6]: RICIS_RIEMANN_PROOF_TEST.md "Riemann proof test"

[7]: RICIS_NAVIER_STOKES_PROOF.md "Navier–Stokes proof scenario"

[8]: RICIS_NAVIER_STOKES_PROOF_DESIGN.md "Navier–Stokes proof design"
