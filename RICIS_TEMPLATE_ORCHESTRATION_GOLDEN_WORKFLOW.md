# Оркестрация построения академических шаблонов RICIS

**Status:** `Mandatory project knowledge source`  
**Applies to:** каждый новый semantic LaTeX template, локаль, исходный академический документ и golden comparison.  
**Primary example:** `Knowledge/LaTexExamples/NavierStokes-Ricis.structural-exemplar.tex`.

## Цель

Каждый новый шаблон строится не «по вкусу», а как воспроизводимое преобразование:

```text
исходный академический документ
  → неизменяемый source asset + SHA-256
  → рекурсивная структурная декомпозиция
  → immutable semantic MVVM model
  → два внешних locale templates (ru-RU, en-US)
  → golden Unit Test против исходного документа
  → двухпроходная LaTeX-компиляция
  → визуальный QA + full quality gate
```

Исходный документ является **golden specification формы и структуры**, но не автоматически доказательством истинности его предметных утверждений. Модель обязана явно хранить evidence boundary; внешний claim не может быть повышен до `KernelChecked` только совпадением layout или текста.

## Неподвижные инварианты

| ID | Правило |
|---|---|
| `ORCH-01` | Исходный документ сохраняется в repository knowledge corpus с SHA-256. Его нельзя заменять или удалять без отдельного одобрения. |
| `ORCH-02` | Любая новая строка отчёта принадлежит external locale template, source model или resource; report wording не hardcode-ится в C# renderer. |
| `ORCH-03` | Для каждой поддержанной локали существуют независимые `*.ru-RU.template` и `*.en-US.template`; fallback не считается локализацией. |
| `ORCH-04` | Source decomposition создаёт immutable recursive ViewModel: front matter, abstracts, sections, typed proof units, tables, closing matter, appendix и author projection. |
| `ORCH-05` | Raw runtime logs, expression trees, requester email, payment state и callback payload не передаются в template. Trace возможен только в explicit technical appendix. |
| `ORCH-06` | Golden Unit Test сравнивает title, subtitle, abstract count/order, numbered and unnumbered heading hierarchy, theorem/proof, table, closing blocks и appendix order с original source. |
| `ORCH-07` | Golden equality нормализует только разрешённые semantic overlays: visible evidence boundary, `Deferred` status, optional public author block и escaping. Любое другое расхождение является backlog item модели или template. |
| `ORCH-08` | Новый missing source field добавляется сначала в typed ViewModel, затем strict loader, renderer projection, оба external templates и unit tests. Нельзя решать отсутствие поля hardcoded text в template. |
| `ORCH-09` | Каждый locale template компилируется `pdflatex` дважды: первый pass создаёт `.aux/.toc`, второй фиксирует final TOC. |
| `ORCH-10` | QA обязательно проверяет no fatal LaTeX errors, no trace leakage, no requester identity leakage, visual hierarchy, `git diff --check` и отсутствие несанкционированных deletions. |

## Последовательность ролей

### 1. Аналитик: source contract

Аналитик создаёт table сопоставления source document → MVVM properties. Он выделяет title, subtitle, author policy, abstracts, TOC policy, section hierarchy, equations, definitions, axioms, theorems, proofs, tables, epilogue, conclusion, appendix и bibliography. Каждому source element назначается одна из трёх судеб: **modelled**, **approved semantic overlay**, **explicitly deferred**.

### 2. Разработчик: typed decomposition

Разработчик расширяет модели только добавлением immutable typed fields. Для каждого нового поля меняются strict external loader, escaping renderer, russian template и english template. Source-specific content хранится во внешнем exemplar/knowledge asset; renderer предоставляет лишь already-safe projection.

### 3. Тестировщик: golden and adversarial tests

Тестировщик добавляет настоящий MSTest/xUnit/NUnit Unit Test в test project. Expected structure извлекается из неизменяемого Russian source asset; actual structure строится как `exemplar → ViewModel → ru-RU template → rendered LaTeX`. Тест сообщает конкретный missing heading/block/property, а не общий mismatch. Отдельно проверяются English template assets, locale separation, escaping, no raw Trace и no identity leakage.

### 4. DevOps: compile and evidence

DevOps рендерит оба locale templates, компилирует каждый PDF двумя passes и сохраняет generated `.tex`, `.pdf` и logs как ignored evidence. Затем выполняет full repository gate, commit, push и обязательный completion report.

## Golden Unit Test contract

```text
Expected: immutable original Russian .tex source
Actual:   source-derived JSON model → strict loader → ru-RU template → renderer
Compare:  normalized academic skeleton and explicitly whitelisted overlays
```

Нормализатор не должен молча удалять или переписывать научные утверждения. Единственные whitelist-исключения должны быть названы в тесте: например, replacement external claim status `Deferred` и visible evidence boundary. Если source assertion не имеет typed Lean bridge, тест обязан требовать его **маркировку**, а не изображать совпадение формы предметным доказательством.

## Definition of done

| Gate | Required evidence |
|---|---|
| Source | Original asset, SHA-256, provenance path. |
| Model | External recursive exemplar и strict loader handle every structural source element. |
| Locales | `ru-RU` и `en-US` templates существуют и independent. |
| Unit test | Russian golden test passes and identifies disallowed difference precisely. |
| Rendering | Both locales compile after two LaTeX passes. |
| Privacy | Tests prove default no Trace and no requester identity/payment leakage. |
| Regression | Full Core, Numerics, Finance and Lean gates pass. |
| Git | No unapproved deletion, clean diff, commit and push. |

## Backlog rule

Если golden test обнаруживает missing source component, создаётся smallest typed backlog item in this order: **model field → loader → renderer projection → ru-RU template → en-US template → unit test → PDF validation**. Нельзя скрывать недостающее свойство текстовой заглушкой.
