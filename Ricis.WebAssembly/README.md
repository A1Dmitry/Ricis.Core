# Ricis.WebAssembly

`Ricis.WebAssembly` — standalone **Blazor WebAssembly** клиент для `Ricis.WebApi`. Он даёт браузерный интерфейс к уже существующим parser и RICIS pipeline, но не переносит и не дублирует symbolic engine в JavaScript.

## Назначение

Клиент предоставляет три явно заданных операции:

| Операция | API route | Результат |
|---|---|---|
| Упрощение | `POST /api/expressions/simplify` | Parsed и производная RICIS форма. |
| Производная | `POST /api/expressions/derivative` | Символическая derivative форма. |
| Система | `POST /api/expressions/system` | Structural system для lambda, разделённых `;`. |

Все операции используют typed `RicisApiClient`. В браузер передаётся только parser-language строка; она **не исполняется** как C#, JavaScript или произвольный код. Разбор, allowlist функций, ограничение длины и построение expression tree выполняются на серверной стороне `Ricis.WebApi`.

## Локальный запуск

Откройте два терминала из корня solution.

```bash
# Terminal 1: server API
dotnet run --project Ricis.WebApi/Ricis.WebApi.csproj --urls http://localhost:5044

# Terminal 2: Blazor WebAssembly client
dotnet run --project Ricis.WebAssembly/Ricis.WebAssembly.csproj --urls http://localhost:5066
```

Откройте `http://localhost:5066` в браузере. Development API base URL находится в [`wwwroot/appsettings.json`](wwwroot/appsettings.json):

```json
{
  "RicisApi": {
    "BaseUrl": "http://localhost:5044/"
  }
}
```

Для другого окружения задайте абсолютный `http` или `https` URL. `Program.cs` намеренно отклоняет невалидную или неполную конфигурацию до запуска приложения.

В workspace есть ссылка **«Открыть Swagger / API Explorer»**. Она получает адрес как `swagger/` относительно этого же configured API base URL. Swagger UI и OpenAPI JSON публикуются API только в Development environment; WebAssembly не содержит и не дублирует Swagger server.

## CORS boundary

`Ricis.WebApi` применяет именованную политику `RicisWebAssembly`. Development origin `http://localhost:5066` перечислен в `Ricis.WebApi/appsettings.json` как единственный разрешённый origin. Политика разрешает только необходимые HTTP headers и methods, не включает credentials и не использует `AllowAnyOrigin`.

При публикации необходимо заменить development origin на конкретный HTTPS origin deployed WebAssembly приложения. Не добавляйте wildcard origin для authenticated или будущих credential-bearing endpoint.

## Проверка

```bash
dotnet build Ricis.Core.sln --configuration Release

dotnet publish Ricis.WebAssembly/Ricis.WebAssembly.csproj \
  --configuration Release \
  --output /tmp/ricis-webassembly-publish
```

End-to-end smoke test должен включать `GET /health`, `simplify`, `derivative`, `system`, valid browser CORS preflight и negative CORS test с неразрешённым `Origin`.
