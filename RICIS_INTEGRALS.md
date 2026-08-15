# Геометрический Integral RICIS

## Нормативная семантика

`Integral` не является классическим интегралом. Он **не** строит сумму Римана, предел, квадратуру, первообразную или численную аппроксимацию. Это программный интерфейс к уже действующему геометрическому правилу A6:

> `0_F · ∞_L → F · L`.

Здесь `F` — отложенная лямбда-выражение полосы, а `L` — ширина диапазона: константа либо другое отложенное выражение. Результат — новое независимое expression tree `F·L`.

## API

```csharp
using System.Linq.Expressions;
using Ricis.Core.Extensions;

Expression<Func<double, double>> f = x => x + 1;
Expression<Func<double, double>> area = f.Integral(5.0);
// x => ((x + 1) * 5)

Expression<Func<double, double>> width = x => x - 1;
Expression<Func<double, double>> symbolicArea = f.Integral(width);
// x => ((x + 1) * (x - 1))
```

Обе перегрузки имеют ограничение `where T : INumber<T>` и не приводят finite-выражения к `double`. Вторая перегрузка связывает параметр ширины с параметром `F`, но не исполняет ни одну лямбду при построении дерева.

| Вход | Производное дерево |
|---|---|
| `F.Integral(L)` | `F·L` |
| `F = t=>t/t`, `L=4` | L1 сначала даёт `1`, затем `1·4→4` |
| `F : BigInteger→BigInteger`, `L : BigInteger` | Точное `BigInteger`-дерево `F·L` |

## Структурная Sum

`Sum` объединяет две отложенные лямбды в одно независимое дерево `F+G`:

```csharp
Expression<Func<double, double>> f = x => x + 1;
Expression<Func<double, double>> g = y => y - 1;
Expression<Func<double, double>> sum = f.Sum(g);
// x => ((x + 1) + (x - 1))
```

Параметр `G` связывается с параметром `F`, после чего обе исходные лямбды и полученная сумма проходят обычный RICIS-конвейер. Поэтому тождества и SP2 применяются к каждому слагаемому до построения независимого результата. `Sum` не создаёт бесконечный ряд и не выполняет скрытый цикл.

## Связь с A6

Внутреннее ядро `Integral` материализует уже нормативный результат A6 напрямую. Базовые классы `0_F` и `∞_G` пока содержат double-метаданные ключей для решателя полюсов; поэтому generic API не создаёт искусственную double-сингулярность для `int`, `decimal` или `BigInteger`. Семантика остаётся той же: `Integral(F,L)` возвращает именно `F·L`.

## Регрессии и консоль

Набор `RicisIntegralSuite` содержит I01–I05: постоянная и отложенная ширина, L1 перед построением, точный `BigInteger` и структурное равенство результату A6. Набор `RicisSumSuite` содержит SUM01–SUM03: связывание параметров двух лямбд, L1 до сложения и точный `BigInteger`.

Для запуска демонстрации используйте:

```bash
dotnet run --project Ricis.Console/Ricis.Console.csproj -c Release -- --integral-demo

# Структурная сумма двух отложенных функций
dotnet run --project Ricis.Console/Ricis.Console.csproj -c Release -- --sum-demo
```
