# RICIS compilation verification

**Verification date:** 2026-08-16

## Results

| Check | Result |
|---|---|
| `Ricis.Core.csproj` Release build | Passed, 0 warnings, 0 errors |
| `RegressionTests/Ricis.Core.RegressionTests.csproj` Release build | Passed, 0 warnings, 0 errors |
| `Ricis.Console/Ricis.Console.csproj` Release build | Passed, 0 warnings, 0 errors |
| Regression suite | **293/293 passed** |
| `--lean-doc-demo` generated source | Compiled successfully with `lake env lean` |
| `--lean-a6-demo` generated source | Compiled successfully with `lake env lean` |
| Generated source placeholder scan | No `sorry` or `sorryAx` found |
| Git state | `main` clean after commit |

## Reproduction commands

```bash
export DOTNET_ROOT=/usr/lib/dotnet
export PATH="$DOTNET_ROOT:$HOME/.elan/bin:$PATH"

dotnet build Ricis.Core.csproj --configuration Release
dotnet build RegressionTests/Ricis.Core.RegressionTests.csproj --configuration Release
dotnet build Ricis.Console/Ricis.Console.csproj --configuration Release

dotnet run --project RegressionTests/Ricis.Core.RegressionTests.csproj \
  --configuration Release --no-build

dotnet run --project Ricis.Console/Ricis.Console.csproj \
  --configuration Release --no-build -- --lean-doc-demo \
  > /tmp/ricis_generated.lean

dotnet run --project Ricis.Console/Ricis.Console.csproj \
  --configuration Release --no-build -- --lean-a6-demo \
  > /tmp/ricis_a6_generated.lean

cd FormalVerification/Lean
lake env lean /tmp/ricis_generated.lean
lake env lean /tmp/ricis_a6_generated.lean
```

The verification was run against commit `2f08e81` before this evidence commit. The evidence commit adds no source behavior; it records the successful compilation result for the GitHub audit trail.
