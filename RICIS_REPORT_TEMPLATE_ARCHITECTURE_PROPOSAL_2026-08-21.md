# Архитектурное предложение: специализированные модели и внешние шаблоны отчётов RICIS

## Решение

Для RICIS рекомендуется не расширять текущий общий renderer большим `switch` и не переносить существующий `RicisLogEntry` напрямую в шаблон. Оптимальная схема:

```text
ILog<TSender>
  -> sender-aware semantic classifier
  -> report-specific typed model
  -> culture/resource resolver
  -> external template
  -> format artifact
```

Внешние `.scriban` templates подходят как основной text-template layer для Academic, Text, LaTeX и JSON, если шаблоны получают только безопасные typed models через allowlist. Lean следует оставить отдельным renderer/model pipeline: шаблон не должен превращать произвольный audit event в kernel theorem.

## Почему не один шаблон и не один renderer

`ILog<TSender>` различает sender context, но текущий `RicisProofLog<TStage>` только записывает envelope в общий journal. Это полезный технический transport layer, однако не является семантическим report pipeline. Academic report должен отбирать proof facts, Text Log — сохранять полный Trace, JSON — фиксировать versioned machine schema, LaTeX — строить document sections, Lean — разделять theorem artifact и audit commentary.

## Typed semantic model

Предлагаемые базовые модели:

```csharp
public sealed record SemanticEvent(
    string EventCode,
    ReportSeverity Severity,
    SenderDescriptor Sender,
    EventPhase Phase,
    IReadOnlyDictionary<string, string> Attributes,
    SemanticPayload Payload);

public sealed record AcademicReportModel(
    ReportMetadata Metadata,
    IReadOnlyList<DefinitionBlock> Definitions,
    IReadOnlyList<AssumptionBlock> Assumptions,
    IReadOnlyList<ProofStepBlock> ProofSteps,
    IReadOnlyList<LimitationBlock> Limitations,
    ConclusionBlock Conclusion);

public sealed record TextTraceModel(
    ReportMetadata Metadata,
    IReadOnlyList<DiagnosticTraceRow> Rows);
```

Фактические классы и namespace должны быть уточнены после полного inventory всех sender types. Важно, чтобы template model не содержала `ILog`, journal, visitor instance или произвольный CLR object.

## Sender-aware classification

Классификатор обязан рассматривать не только `EventCode`, но и `StageType`, severity, attributes, message и exception data. Например, `RICIS_PHASE_TRACE` от `AlgebraicReductionVisitor` означает proof transformation; тот же event code от document builder означает уже document construction. Необходимо registry/strategy mapping:

```text
(SenderType, EventCode, Severity) -> SemanticEventKind + typed payload + visibility policy
```

Unknown combinations не должны молча попадать в Academic output. Они получают `Unclassified`/`Warning` и остаются в Text Trace до явного решения.

## Visibility policy

| Канал | Trace | Info | Warning | Exception |
|---|---:|---:|---:|---:|
| Text Log | Да | Да | Да | Да |
| Academic | Нет по умолчанию | Да, после semantic selection | Да, если влияет на доказательство | Только объяснение причины, без полного технического dump |
| JSON | Только если schema/model данного JSON требует | Да | Да | Typed exception model |
| LaTeX | Нет по умолчанию | Да, как sections/metadata | Да, если существенны | Structured explanation |
| Lean | Нет в theorem model | Только explicit theorem metadata | Только explicit audit comment | Не в theorem body |

Для технического LaTeX appendix должен существовать отдельный explicit option, например `IncludeTechnicalTrace = true`; default должен быть `false`.

## Templates и resources

Шаблоны должны храниться вне C#:

```text
ReportTemplates/
  academic/{culture}/proof.sbn
  text/{culture}/trace.sbn
  latex/{culture}/proof.sbn
  json/{culture}/report.sbn
  lean/{culture}/audit.sbn
ReportResources/
  AcademicReport.resx
  AcademicReport.ru.resx
  AcademicReport.en.resx
  LatexReport.ru.resx
  LatexReport.en.resx
```

Машинные keys (`Academic.ProofStep.Rule`, `Academic.Section.Assumptions`) должны быть стабильными и не зависеть от перевода. `.resx`/`ResourceManager` отвечают за локализуемые фразы, а `.scriban` отвечает за формат, порядок и условные секции. Для каждого culture применяется specific -> neutral -> controlled default fallback.

## Ограничения безопасности

Scriban или другой engine должен получать только immutable report model и зарегистрированные helpers: escaping, formatting numbers, localized resource lookup. Не разрешаются arbitrary reflection, file access, network access, proof execution и доступ к `ILog`. Template engine не должен содержать бизнес-правила доказательства.

## QA contract

QA обязан добавить golden-file tests по каждому report kind и минимум двум culture. Обязательны проверки:

1. Trace event существует в Text model и отсутствует в Academic/LaTeX/Lean model при default policy.
2. Sender type и attributes корректно классифицируют phase/event.
3. Unknown sender/event не становится proof step.
4. Handled exception содержит cause, phase, sender и handling status.
5. Missing resource/template вызывает явную диагностическую ошибку или контролируемый fallback.
6. Escaping проверяется отдельно для Markdown, LaTeX, JSON и Lean comments.
7. Template model не может инициировать вычисление или повторный запуск proof pipeline.
8. Null logger полностью отключает semantic handling и не изменяет результат вычисления.

## Внешние основания

Microsoft рекомендует отделять executable code от localizable resources, применять resource files, typed resource access и culture fallback [1] [2] [3] [4]. Scriban описывается как лёгкий text-template engine для .NET [5], но его необходимо использовать только с ограниченным model surface и allowlist.

## References

[1]: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-10.0 "Globalization and localization in ASP.NET Core"
[2]: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization/make-content-localizable?view=aspnetcore-10.0 "Make an ASP.NET Core app's content localizable"
[3]: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization/provide-resources?view=aspnetcore-10.0 "Provide localized resources for languages and cultures"
[4]: https://learn.microsoft.com/en-us/dotnet/standard/globalization-localization/localization ".NET localization"
[5]: https://github.com/scriban/scriban "Scriban project"

## Статус

Документ является архитектурным предложением и входом для следующей реализации. Production code пока не изменён этим предложением; до реализации необходимо завершить inventory sender types, exception paths и существующих tests.
