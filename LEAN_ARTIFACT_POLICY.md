# Lean Evidence Artifact Policy

## Назначение

Каждый Lean-результат, который проект считает подтверждённым, обязан существовать как версионируемый artifact в репозитории. Результат не считается зафиксированным, если он существует только во временном `/tmp`, в stdout regression test или в комментарии CI.

> Компиляция Lean подтверждает конкретный сохранённый Lean source. Regression test подтверждает конкретный C# contract. Эти два вида evidence связаны provenance, но не подменяют друг друга.

## Evidence statuses

| Status | Смысл | Обязательная проверка |
|---|---|---|
| `KernelChecked` | Lean source содержит theorem/example, который проверен закреплённым Lean toolchain | `lake env lean <source>` без ошибок; запрещены `sorry` и `sorryAx` |
| `RegressionChecked` | Результат проверен C# regression test и зафиксирован Lean-readable evidence record или structured source | Указаны suite/test IDs, commit context и источник C#; автоматически проверяется наличие artifact |
| `AuditOnly` | Typed log или explanatory Lean comments; не является kernel theorem | Artifact может содержать комментарии, но обязан явно содержать `NOT KERNEL VERIFIED` |
| `RenderedOnly` | LaTeX/PDF/JSON presentation output | Проверяется отдельно; не объявляется Lean proof |

## Обязательные provenance fields

Каждая запись `manifest.json` обязана содержать `id`, `status`, `source`, `description`, `origin`, `testIds`, `theoremNames`, `leanToolchain`, `generatedBy`, `generatedFrom` и `forbiddenMarkers`. `KernelChecked` дополнительно обязан перечислять конкретные theorem names. `RegressionChecked` обязан перечислять реальные C# test IDs, а не только название suite.

`generatedFrom` должен указывать на commit или deterministic source path. Если artifact генерируется из C#, generated source обязан быть сохранён в `FormalVerification/Lean/Artifacts/`, а не только в temporary directory.

## Правила trust boundary

Generic C# expression trees не преобразуются в kernel theorem автоматически. Документный Lean scaffold запрещён. Для kernel evidence используются только structured Lean templates и безопасные identifiers. Audit report обязан оставаться comment-only и явно указывать отсутствие kernel verification.

Результаты regression tests фиксируются отдельно от kernel theorem. Нельзя менять status `RegressionChecked` на `KernelChecked` без реально скомпилированного Lean theorem source.

## CI acceptance

CI выполняет следующие проверки:

1. Все `source` из manifest существуют и находятся внутри repository.
2. Ни один artifact не содержит `sorry` или `sorryAx`.
3. Каждый `KernelChecked` source компилируется закреплённым toolchain.
4. Каждый `RegressionChecked` artifact связан с существующим C# test ID или suite ID.
5. `AuditOnly` содержит явную границу `NOT KERNEL VERIFIED`.
6. Manifest сам является валидным JSON и не содержит дубликатов IDs.

## Directory contract

```text
FormalVerification/Lean/
├── Artifacts/
│   ├── manifest.json
│   ├── kernel/
│   ├── regression/
│   └── audit/
├── Generated/
└── RicisIdentity/
```

`Generated/` и `RicisIdentity/` сохраняются как compatibility sources. Новые подтверждённые результаты фиксируются в `Artifacts/` и добавляются в manifest.

## References

[1]: ./RICIS_III_CONCEPT.md
[2]: ./FormalVerification/Lean/RicisIdentity/TypeIdentity.lean
[3]: ./FormalVerification/Lean/Generated/ComplexSingularityA6.lean
[4]: ./RegressionTests/RicisTypedProofLogSuite.cs
[5]: ./RegressionTests/RicisJacobianProofArtifactSuite.cs
