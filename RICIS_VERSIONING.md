# Ricis.Core Versioning Policy

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

Version metadata не заменяет regression suite и API/Lean compatibility checks. Перед тегом требуется выполнить Release build, полный regression run, API/Swagger smoke checks и Lean verification, если изменён proof pipeline.
