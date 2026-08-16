# Ricis.Core

`Ricis.Core` — .NET 8 библиотека RICIS III (**Recursive Indexed Calculus of Identity and Singularity**). Ядро представляет вычисления как LINQ Expression Trees и специальные RICIS-узлы, сохраняя identity, type, payload, индексы нулей и бесконечностей до применения нормативного преобразования.

> RICIS III имеет приоритет над классической арифметикой во всех явно определённых случаях. Неопределённые RICIS-правилами операции наследуют классический fallback.

## Проекты solution

| Проект | Назначение |
|---|---|
| `Ricis.Core` | NuGet-библиотека: expression types, phases, bridges, calculus, vectors, matrices и proof API. |
| `Ricis.Console` | Interactive/CLI parser ограниченной математической grammar. |
| `Ricis.WebApi` | ASP.NET Core HTTP API поверх parser-а и RICIS pipeline. |
| `Ricis.NavierStokes.Console` | Символьный proof scenario выбранного поля Навье—Стокса. |
| `RegressionTests` | Изолированные regression contracts RICIS. |

## Быстрый старт

Собрать solution и запустить regression suite:

```bash
dotnet build Ricis.Core.sln --configuration Release
dotnet run --project RegressionTests/Ricis.Core.RegressionTests.csproj --configuration Release
```

Запустить console с одиночной lambda:

```bash
dotnet run --project Ricis.Console/Ricis.Console.csproj -c Release -- \
  "x => ((x ^ 2) - 25) / (x - 5)"
```

Запустить Web API:

```bash
dotnet run --project Ricis.WebApi/Ricis.WebApi.csproj --urls http://localhost:5080
```

## Ключевая документация

| Документ | Содержание |
|---|---|
| [`RICIS_III_CONCEPT.md`](RICIS_III_CONCEPT.md) | Канонические L0/L1, SP1–SP4, A-правила, фазы и границы RICIS III. |
| [`RICIS_RULE_COVERAGE.md`](RICIS_RULE_COVERAGE.md) | Нормативная матрица rule-to-regression coverage. |
| [`Ricis.Console/README.md`](Ricis.Console/README.md) | CLI, parser grammar, системы через `;` и console demos. |
| [`RICIS_WEBAPI.md`](RICIS_WEBAPI.md) | HTTP endpoints, request examples и security boundaries Web API. |
| [`AUTHOR_SEO_METADATA.md`](AUTHOR_SEO_METADATA.md) | Opt-in author metadata через closure capture или parameter `about`. |
| [`RICIS_ACADEMIC_PROOFS.md`](RICIS_ACADEMIC_PROOFS.md) | `Prove`, `ProveDocument`, traces и proof boundaries. |
| [`RICIS_DERIVATIVES.md`](RICIS_DERIVATIVES.md) | Символьная производная без пределов и Лопиталя. |
| [`RICIS_INTEGRALS.md`](RICIS_INTEGRALS.md) | Геометрический `Integral` и `Sum` через нормативную A6-семантику. |
| [`RICIS_NAVIER_STOKES_PROOF.md`](RICIS_NAVIER_STOKES_PROOF.md) | Символьный сценарий Навье—Стокса для конкретного поля. |
| [`FormalVerification/Lean/README.md`](FormalVerification/Lean/README.md) | Воспроизведение Lean-модели ID-01–ID-06. |

## Автоматизация поставки

GitHub Actions workflow [`build-and-test.yml`](.github/workflows/build-and-test.yml) выполняет restore, Release build всех проектов и regression suite при каждом push.

Workflow [`publish-nuget.yml`](.github/workflows/publish-nuget.yml) создаёт и публикует `Ricis.Core` в NuGet.org при push release-тега вида `v*`. Для публикации требуется repository secret `NUGET_API_KEY`.

```bash
git tag -a v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0
```

## Границы proof-статуса

Успешное преобразование expression tree или сведение конкретного residual к структурному нулю сертифицирует **внутренний RICIS-сценарий**, заданный входными деревьями и принятыми нормативными предпосылками. Оно не должно автоматически интерпретироваться как доказательство внешней общей теоремы без отдельного предметного bridge. Полный контракт приведён в [`RICIS_III_CONCEPT.md`](RICIS_III_CONCEPT.md) и [`RICIS_PROOF_DOCUMENTS.md`](RICIS_PROOF_DOCUMENTS.md).
