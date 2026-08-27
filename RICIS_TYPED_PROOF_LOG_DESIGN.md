# Архитектурный дизайн: единый `ILog<TStage>` и proof-document factory

**Статус:** `РЕФАКТОРИРОВАНО ДЛЯ СТРОГОГО DRY`
**Связанная задача:** [`RICIS_TYPED_PROOF_LOG_AGILE.md`](RICIS_TYPED_PROOF_LOG_AGILE.md)

## Решение

`ILog<TStage>` остаётся единственным typed event journal proof-процесса. Параметр `TStage` обозначает реальный источник события: orchestration, visitor либо handler. Он **не** выбирает формат документа.

Формат выбирает существующий `RicisProofDocumentFormat`. До запуска proof-run `ResolveDocumentConstructor(format)` инъецирует одну лямбду-конструктор документа. После единственного symbolic derivation тот же constructor получает `profile`, полный text derivation и derived expression. Он не исполняет conditions/constraints, не запускает visitor повторно и не меняет expression tree.

```text
format enum
   │
   └── ResolveDocumentConstructor(format)
          │
          ├── Academic: existing Markdown builder
          └── RicisProofDocumentTemplates.Factories
                 ├── Log
                 ├── Json
                 ├── Latex
                 └── Lean scaffold

one proof run ─── ILog<TStage> ─── full phase trace ─── node → root routes
      │                                                     │
      └──────────── injected constructor receives one common derivation ───────┘
```

| Существующий элемент | Назначение после рефакторинга |
|---|---|
| `ILog<TStage>` | `Info`, `Warning`, `Exception`, `Trace`, typed child stages и общий snapshot. |
| `RicisPhaseTraceStep` | До/после tree каждого pipeline этапа и все маршруты от каждого узла к корню. |
| `RicisProofDocumentFormat` | Единственный selector: `Log`, `Academic`, `Json`, `Latex`, `Lean`. |
| `RicisProofDocumentTemplates.Factories` | Таблица `format → constructor lambda` для всех не-academic форматов. |
| `ResolveDocumentConstructor` | Выбирает academic builder либо существующую format factory до proof-run. |
| `ProveDocumentWithLog` | Additive explicit injection API; строит proof один раз и добавляет typed visitor/handler log в common derivation. |

## Инварианты

| ID | Инвариант |
|---|---|
| D-01 | Полный proof protocol содержит каждую нормативную фазу: changed, unchanged и skipped. |
| D-02 | Для до- и после-снимка каждой фазы фиксируются все маршруты от посетившегося узла к root expression. |
| D-03 | `TStage` берётся из реального CLR type и остаётся видимым в injected document path. |
| D-04 | `Log`, `Json`, `Latex` и Lean scaffold потребляют одинаковый already-derived protocol; ни один format не доказывает выражение повторно. |
| D-05 | Generic Lean output является documentation scaffold и не заявляет kernel verification; типизированный `RicisLeanTemplate` остаётся отдельным bridge для поддерживаемых theorem rows. |
| D-06 | Прежние `ProveDocument` overloads и `Func<string,string>` transform сохраняют source compatibility; injection path имеет явное имя `ProveDocumentWithLog`, чтобы `null` transform не стал неоднозначным. |

## Проверяемый порядок форматов

1. `Log` фиксирует полный line-oriented protocol.
2. `Json` записывает тот же protocol в поле `derivation`.
3. `Latex` вкладывает тот же protocol в verbatim proof trace.
4. `Lean` вкладывает тот же protocol в comment scaffold без создаваемого theorem.

Тесты `PDF01`–`PDF08` проверяют format factory, node-to-root маршрут, Lean boundary, LaTeX и документ с injected `ILog<TStage>`.
