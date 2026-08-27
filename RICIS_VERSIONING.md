# Ricis.Core Versioning Policy

> **Document version:** `0.4.0`
> **Created:** `2026-08-16`
> **Last modified:** `2026-08-19`
> **Versioning note:** increment the document version when the normative content changes.


## Источник истины

`Ricis.Core` является источником версии для всего solution. Файл `Directory.Build.props` задаёт `RicisCoreVersion`; по умолчанию это `0.1.0`. Из него производятся `Version`, `VersionPrefix`, `AssemblyVersion`, `FileVersion` и `InformationalVersion` всех проектов, которые входят в solution.

Внутренние приложения и тестовые проекты остаются связанными с текущим `Ricis.Core` через `ProjectReference`. Поэтому build одного commit не создаёт несовместимого набора внутренних версий: Core, Web API, WebAssembly, Console и RegressionTests получают одну version metadata.

## Release override

При публикации NuGet workflow получает версию из ручного input или тега:

```text
v1.2.3
→ RicisCoreVersion=1.2.3
→ build Ricis.Core
→ pack Ricis.Core 1.2.3
→ verify *.nuspec
→ publish NuGet
```

Тег должен соответствовать SemVer-подобному формату `MAJOR.MINOR.PATCH` с необязательным prerelease/build suffix. Рекомендуемый порядок релиза — создать тег `vX.Y.Z` после прохождения CI.

## Внешние зависимости

`Directory.Packages.props` централизует версии внешних NuGet-пакетов. Сейчас там находятся `Microsoft.AspNetCore.OpenApi 8.0.29`, `Microsoft.AspNetCore.Components.WebAssembly 8.0.29`, `Microsoft.AspNetCore.Components.WebAssembly.DevServer 8.0.29` и `Swashbuckle.AspNetCore 6.6.2`. Project files больше не содержат локальных version attributes для этих packages.

## Правила изменения версии

| Изменение | Версия |
|---|---|
| Исправление без изменения публичного contract | PATCH |
| Новый совместимый public API или функциональность | MINOR |
| Несовместимое изменение public API/формата | MAJOR |
| Предварительный релиз | prerelease suffix, например `1.1.0-rc.1` |

## v0.4.0

Версия `0.4.0` добавляет отдельную публичную библиотеку `Ricis.Finance` с DDD Domain/Application слоями, порциями integration contracts и самостоятельными FIN01–FIN06 регрессиями. Это MINOR-релиз: существующие `Ricis.Core` proof APIs и application projects остаются совместимыми.

## v0.3.0

Версия `0.3.0` ввела совместимый `ProveDocumentWithLog` API, полный node-to-root route в существующем proof trace, `Latex` в действующий `RicisProofDocumentFormat` и единую инъецируемую document factory.

## v0.2.0

Версия `0.2.0` ввела совместимый публичный `ILog<TStage>` API и canonical typed event journal.

Version metadata не заменяет regression suite и API/Lean compatibility checks. Перед тегом требуется выполнить Release build, полный regression run, API/Swagger smoke checks и Lean verification, если изменён proof pipeline.
