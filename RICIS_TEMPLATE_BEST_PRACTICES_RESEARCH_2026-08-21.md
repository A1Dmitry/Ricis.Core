# Best Practices для шаблонов отчётов RICIS

## Источники

1. Microsoft, *Globalization and localization in ASP.NET Core*: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-10.0
2. Microsoft, *Make an ASP.NET Core app's content localizable*: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization/make-content-localizable?view=aspnetcore-10.0
3. Microsoft, *Provide localized resources for languages and cultures*: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization/provide-resources?view=aspnetcore-10.0
4. Microsoft, *.NET localization*: https://learn.microsoft.com/en-us/dotnet/standard/globalization-localization/localization
5. Scriban project: https://github.com/scriban/scriban

## Выводы, применимые к RICIS

Microsoft разделяет executable code и localizable resources. Это поддерживает требование не зашивать академический текст, сообщения и шаблонные фразы в C#-код: code block должен оставаться общим, а текстовые ресурсы и языковые варианты должны быть внешними ресурсами/саттелитными сборками [1] [4]. Для культур используются отдельные `CultureInfo`/`CurrentUICulture`, resource lookup и parent-culture fallback [1] [3] [4].

Microsoft описывает два допустимых режима работы с ресурсами: strongly keyed resources для стабильных ключей и literal-based localizer для ускорения разработки [2]. Для RICIS предпочтителен strongly keyed режим: event semantic keys и template keys должны быть стабильными, а переводимый текст не должен использоваться как machine identifier.

Ресурсы должны быть локализуемыми независимо от executable code, а форматный шаблон должен быть отделён от semantic document model. Поэтому `AcademicDocumentModel`, `LatexDocumentModel`, `LeanDocumentModel`, `JsonReportModel` и `TextTraceModel` не должны содержать готовую разметку или локализованные фразы; они должны хранить семантические блоки, ключи ресурсов, параметры и классифицированные события.

Scriban позиционируется как лёгкий и безопасный .NET text-template engine [5], но любой внешний template engine должен использоваться только с ограниченным model surface, allowlist globals/members, ограничением размера/времени и без доступа к произвольному CLR reflection/file/network API. Для академических и Lean-шаблонов особенно опасно позволять шаблону выполнять произвольную бизнес-логику.

## Сравнение стратегий

| Стратегия | Плюсы | Риски | Решение для RICIS |
|---|---|---|---|
| Строковая конкатенация в C# | Простая | Hardcoded text, нет локализации, слабая тестируемость | Отклонить |
| Один универсальный `RicisLogEntry` dump | Быстро | Trace leakage, одинаковая семантика для разных форматов | Отклонить как финальный report pipeline |
| `.resx` только для фраз + code templates | Хорошая localization | LaTeX/Lean structure остаётся в C# | Использовать только для resource strings |
| Внешние `.scriban` templates + typed models | Отделение текста/разметки, языки и форматы расширяемы | Нужен sandbox/allowlist и schema validation | Рекомендуется для Academic/Text/LaTeX/JSON |
| Razor templates | Сильная экосистема | Тяжелее, HTML/Web-oriented, не идеально для Lean/LaTeX | Не выбирать как общий engine |
| Handlebars/Mustache | Простота и ограниченная логика | Ограниченная работа со сложными секциями и typed diagnostics | Возможен для простых Text/JSON, не основной |
| Собственные минимальные templates | Полный контроль | Необходимо поддерживать parser/runtime | Только если внешняя зависимость запрещена |

## Рекомендуемая архитектура

Рекомендуется четырёхслойная схема:

```text
Typed event
  -> Semantic classifier (sender + EventCode + severity + attributes + message + exception)
  -> Report-specific document model
  -> Culture/resource resolver
  -> External template renderer
  -> Artifact
```

`ILog<TSender>` не должен напрямую писать в универсальный готовый документ. Его задача — принять событие в контексте отправителя и передать его semantic handler конкретной report pipeline. Каждая pipeline строит собственную модель.

Шаблон получает только typed model, а не `ILog`, `RicisLogEntry`, CLR objects или expression visitors. Таким образом, шаблон не может повторно запускать доказательство и не видит внутренний Trace, если он не является частью данной модели.

Предлагаемая структура ресурсов:

```text
ReportTemplates/
  academic/en-US/*.scriban
  academic/ru-RU/*.scriban
  text/en-US/*.scriban
  latex/en-US/*.scriban
  lean/en-US/*.scriban
ReportResources/
  AcademicReport.en.resx
  AcademicReport.ru.resx
  LatexReport.en.resx
  LatexReport.ru.resx
```

Шаблон выбирается по `ReportKind`, `CultureInfo` и `TemplateVersion`. Resource keys переводят фразы, а template files отвечают за формат и порядок секций. Fallback: specific culture -> neutral culture -> controlled default; missing required resource/template должен быть диагностической ошибкой, а не молчаливой подстановкой технического ключа в опубликованный академический документ.

## QA acceptance criteria

1. Ни один report template не содержит доступа к `ILog`, journal или произвольному sender object.
2. Text Log видит Trace; Academic/LaTeX/Lean не видят Trace автоматически.
3. Один и тот же event sequence даёт разные report models, соответствующие семантике формата.
4. Template model validation ловит missing required sections, unknown keys и неподдержанный `TemplateVersion`.
5. Locale fallback тестируется для specific, neutral и missing culture.
6. Resource keys стабильны и не совпадают с переводимым текстом.
7. Template engine sandbox имеет allowlist и не имеет доступа к reflection/file/network.
8. Exception model разделяет cause, sender stage, handled/rethrown status и public explanation.
9. Academic output проверяется golden-file tests для каждого поддерживаемого языка.
10. LaTeX escaping и Lean comment escaping проверяются отдельно от semantic classification.

## Рекомендация

Для текущего проекта выбрать **external Scriban templates + strongly typed report models + `.resx` resource keys + sender-aware semantic classifiers**. Не использовать Scriban для бизнес-логики и не передавать в него raw log entries. Для Lean сохранить отдельный renderer/model и не считать текстовый Lean audit comment kernel proof.
