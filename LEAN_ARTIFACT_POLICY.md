# Lean Evidence Artifact Policy

## Назначение

Каждый Lean-результат, который проект считает подтверждённым, обязан существовать как версионируемый artifact в репозитории. Результат не считается зафиксированным, если он существует только во временном `/tmp`, в stdout regression test или в комментарии CI.

> Компиляция Lean подтверждает конкретный сохранённый Lean source. Regression test подтверждает конкретный C# contract. Эти два вида evidence связаны provenance, но не подменяют друг друга.

## Обязательный источник знаний для модели

Каждый Lean-результат, который успешно прошёл `lake env lean` или был подтверждён regression quality gate, обязан быть прикреплён к проекту как постоянный файл под `FormalVerification/Lean/Artifacts/` и зарегистрирован в `manifest.json`. В manifest каждая запись обязана содержать объект:

```json
"knowledgeSource": {
  "mandatoryForModelStudy": true,
  "role": "mandatory-project-knowledge-source"
}
```

Эта отметка означает, что artifact является обязательным источником знаний при последующем анализе проекта моделью. Временный файл в `/tmp`, stdout, локальный незакоммиченный результат или сообщение CI не являются прикреплением к проекту и не могут считаться источником знаний. Изменение или удаление такого artifact требует обновления provenance manifest и повторного прохождения quality gate.

Verifier обязан отклонять любую запись без `knowledgeSource.mandatoryForModelStudy == true` и точного значения `knowledgeSource.role`. Поэтому правило является не только документационным, но и машинно проверяемым.

## Evidence statuses

| Status | Смысл | Обязательная проверка |
|---|---|---|
| `KernelChecked` | Lean source содержит theorem/example, который проверен закреплённым Lean toolchain | `lake env lean <source>` без ошибок; запрещены `sorry` и `sorryAx` |
| `RegressionChecked` | Результат проверен C# regression test и зафиксирован Lean-readable evidence record или structured source | Указаны suite/test IDs, commit context и источник C#; автоматически проверяется наличие artifact |
| `AuditOnly` | Typed log или explanatory Lean comments; не является kernel theorem | Artifact может содержать комментарии, но обязан явно содержать `NOT KERNEL VERIFIED` |
| `RenderedOnly` | LaTeX/PDF/JSON presentation output | Проверяется отдельно; не объявляется Lean proof |

## Обязательные provenance fields

Каждая запись `manifest.json` обязана содержать `id`, `status`, `source`, `description`, `origin`, `testIds`, `theoremNames`, `leanToolchain`, `generatedBy`, `generatedFrom`, `forbiddenMarkers` и `knowledgeSource`. `KernelChecked` дополнительно обязан перечислять конкретные theorem names. `RegressionChecked` обязан перечислять реальные C# test IDs, а не только название suite.

`generatedFrom` должен указывать на commit или deterministic source path. Если artifact генерируется из C#, generated source обязан быть сохранён в `FormalVerification/Lean/Artifacts/`, а не только в temporary directory.

## Правила trust boundary

Generic C# expression trees не преобразуются в kernel theorem автоматически. Документный Lean scaffold запрещён. Для kernel evidence используются только structured Lean templates и безопасные identifiers. Audit report обязан оставаться comment-only и явно указывать отсутствие kernel verification.

Результаты regression tests фиксируются отдельно от kernel theorem. Нельзя менять status `RegressionChecked` на `KernelChecked` без реально скомпилированного Lean theorem source.

## Public API test dependency

Любое добавление или изменение `public` метода обязано сопровождаться regression tests в том же изменении. Нормативный контракт зафиксирован в [`PUBLIC_API_TEST_POLICY.md`](./PUBLIC_API_TEST_POLICY.md). Для Lean-related public API regression test дополнительно обязан фиксировать соответствующий artifact/provenance, если результат объявлен подтверждённым. Public method без собственного test ID и регистрации suite не считается готовым к commit.

## CI acceptance

CI выполняет следующие проверки:

1. Все `source` из manifest существуют и находятся внутри repository.
2. Ни один artifact не содержит `sorry` или `sorryAx`.
3. Каждый `KernelChecked` source компилируется закреплённым toolchain.
4. Каждый `RegressionChecked` artifact связан с существующим C# test ID или suite ID.
5. `AuditOnly` содержит явную границу `NOT KERNEL VERIFIED`.
6. Manifest сам является валидным JSON и не содержит дубликатов IDs.
7. Каждый artifact помечен как обязательный источник знаний для изучения моделью.
8. Public API changes принимаются только при наличии соответствующих regression tests согласно `PUBLIC_API_TEST_POLICY.md`.

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
