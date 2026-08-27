# RICIS Core integration test report

> **Document version:** `0.1.0` (provisional baseline)
> **Created:** `2026-08-16`
> **Last modified:** `2026-08-16`
> **Versioning note:** increment the document version when the normative content changes.


**Дата:** 2026-08-16  
**Ветка:** `main`  
**Объём:** solution build, all executable projects, regression suite, console parser/system scenarios, Web API smoke/security checks and LeanDoc compilation.

## Итог

Полный интеграционный цикл завершён успешно. Совместимость между core library, regression tests, both console applications, Web API, expression-system parser and Lean-first proof generation подтверждена. В процессе не потребовалось изменять исходный код.

| Область | Проверка | Результат |
|---|---|---|
| .NET solution | `dotnet restore Ricis.Core.sln` | Passed |
| .NET solution | `dotnet build Ricis.Core.sln --configuration Release` | Passed, 0 warnings, 0 errors |
| Regression | Full suite | **293/293 passed** |
| Ricis.Console | `--self-test` | Parser checks passed |
| Ricis.Console | `--all` | **58/58 expressions processed** |
| Ricis.Console | Single lambda simplification | `((x²−25)/(x−5)) → (x+5)` |
| Ricis.Console | Semicolon expression system | 3 expressions parsed and simplified |
| Ricis.Console | `about` opt-in | SEO metadata emitted; symbolic tree remains deferred |
| Ricis.NavierStokes.Console | Full executable run | Certificate output completed successfully |
| Ricis.WebApi | `/health` | HTTP 200, `status=ok` |
| Ricis.WebApi | simplify endpoint | HTTP 200, RICIS result returned |
| Ricis.WebApi | derivative endpoint | HTTP 200, derivative returned |
| Ricis.WebApi | system endpoint | HTTP 200, `count=3` |
| Web API security | C# method/code injection payload | HTTP 400 |
| Web API security | malformed expression | HTTP 400 |
| Web API limits | 5000-character expression | HTTP 400 |
| Lean | canonical `--lean-doc-demo` + `lake env lean` | Passed |
| Lean | A6 `--lean-a6-demo` + `lake env lean` | Passed |
| Lean source scan | `sorry` / `sorryAx` | Not found |

## Compatibility observations

The canonical command-line contract is the documented contract: `--self-test`, `--all`, `--expr`, direct lambda input and named demonstration flags. An initial probe with `--help` returned the expected “unknown argument” response because `--help` is not implemented as a command-line flag; interactive help is exposed as `help`, and the README documents the supported CLI flags. The probe was corrected to the supported commands and did not reveal a compatibility defect.

The Web API was started locally in Development mode on loopback only. Health, simplification, derivative and semicolon-separated system requests completed successfully. The parser rejected a `System.IO.File.Delete(...)` payload, malformed syntax and an oversized expression without executing arbitrary code.

The generated canonical Lean document contained 56 lines and the A6 document contained 37 lines. Both compiled with Lean 4.33.0 through the project toolchain and neither contained forbidden placeholder tokens.

## Reproduction

```bash
export DOTNET_ROOT=/usr/lib/dotnet
export PATH="$DOTNET_ROOT:$HOME/.elan/bin:$PATH"

dotnet restore Ricis.Core.sln
dotnet build Ricis.Core.sln --configuration Release
dotnet run --project RegressionTests/Ricis.Core.RegressionTests.csproj -c Release --no-build
dotnet run --project Ricis.Console/Ricis.Console.csproj -c Release --no-build -- --self-test
dotnet run --project Ricis.Console/Ricis.Console.csproj -c Release --no-build -- --all

cd FormalVerification/Lean
lake env lean /tmp/full_lean_doc.lean
lake env lean /tmp/full_a6_doc.lean
```
