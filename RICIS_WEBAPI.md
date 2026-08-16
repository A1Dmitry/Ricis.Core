# Ricis.WebApi

`Ricis.WebApi` — ASP.NET Core .NET 8 слой над `Ricis.Core`. Он не заменяет ядро и не определяет отдельную математику: HTTP-запрос преобразуется в ограниченное текстовое lambda-выражение, затем передаётся существующему parser-у и `RicisPhasePipeline`.

> API не принимает и не компилирует произвольный C#-код. Поддерживается только grammar `LambdaTextParser` с белым списком операций и функций.

## Запуск

Из корня solution:

```bash
dotnet run --project Ricis.WebApi/Ricis.WebApi.csproj --urls http://localhost:5080
```

В Development environment Swagger UI доступен по адресу `http://localhost:5080/swagger`. Endpoint `GET /health` всегда возвращает состояние сервиса.

## Endpoints

| Method | Endpoint | Request | Назначение |
|---|---|---|---|
| `GET` | `/health` | — | Проверка доступности API. |
| `POST` | `/api/expressions/simplify` | `ExpressionRequest` | Разобрать и нормализовать одиночную lambda через RICIS. |
| `POST` | `/api/expressions/derivative` | `ExpressionRequest` | Разобрать expression с `derivative(...)` или `dxdt(...)` и вернуть RICIS-результат. |
| `POST` | `/api/expressions/system` | `ExpressionRequest` | Разобрать несколько lambda через `;` в `ExpressionSystem<double>`. |

```json
{
  "expression": "x => ((x ^ 2) - 25) / (x - 5)"
}
```

Пример system request:

```json
{
  "expression": "x => x + 1; x => derivative(x ^ 3); x => integral(x, 5)"
}
```

Ответ одиночного выражения содержит исходную строку, режим, parsed expression и RICIS-форму. System response дополнительно содержит число lambda, структурную запись системы и RICIS-результат каждой координаты.

## Параметр `about`

Параметр lambda не ограничен именем `x`. Строка:

```text
about => about + 1
```

активирует opt-in author metadata. RICIS-строка в поле `ricis` содержит `[SEO AUTHOR]` и JSON-LD-профиль. Это влияет только на текстовое представление; `AuthorAnnotatedExpression.Reduce()` сохраняет исходную вычислимую семантику. Подробное описание — в [`AUTHOR_SEO_METADATA.md`](AUTHOR_SEO_METADATA.md).

## Границы безопасности

| Контроль | Значение |
|---|---|
| Выполнение кода | Отсутствует: API не использует `CSharpScript`, reflection dispatch, shell или `Expression.Compile()` для пользовательского ввода. |
| Grammar | Один параметр lambda, арифметические операторы и whitelist функций `LambdaTextParser`. |
| Размер body | Не более 64 KiB через Kestrel. |
| Длина expression | Не более 4096 символов. |
| Размер системы | Не более 64 непустых lambda, разделённых `;`. |
| Ошибки parser-а | HTTP 400 с контролируемым сообщением и позицией. |
| Неожиданные ошибки | HTTP 400 с общим сообщением без передачи внутреннего stack trace. |
| Swagger | Включён только для Development environment. |

Production deployment дополнительно требует HTTPS, rate limiting, authentication/authorization при необходимости, reverse proxy/WAF и внешних CPU/memory limits контейнера или hosting platform.

## Solution и CI

`Ricis.WebApi` является отдельным проектом в `Ricis.Core.sln`, ссылается на `Ricis.Core` и использует существующий public parser из `Ricis.Console`. Workflow [`.github/workflows/build-and-test.yml`](.github/workflows/build-and-test.yml) выполняет restore и Release build Web API при каждом push, а затем запускает общий regression suite.

## NuGet и release tags

`Ricis.Core` публикуется независимо как библиотека. Workflow [`.github/workflows/publish-nuget.yml`](.github/workflows/publish-nuget.yml) создаёт пакет при push тега `v*`, например `v1.0.0`, и использует GitHub secret `NUGET_API_KEY` для публикации в NuGet.org. Web API не публикуется в NuGet этим workflow: его целевой способ поставки — container или совместимый Web App hosting.
