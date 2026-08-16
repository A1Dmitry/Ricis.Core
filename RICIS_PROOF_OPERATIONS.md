# Доказательные expression-операции RICIS

> **Document version:** `0.1.0` (provisional baseline)
> **Created:** `2026-08-15`
> **Last modified:** `2026-08-15`
> **Versioning note:** increment the document version when the normative content changes.


## Назначение

Этот слой формирует типовые шаги доказательства как **новые независимые expression tree**. Он не вызывает исходные делегаты во время построения, не использует численные подстановки, не вводит пределы и не меняет аксиомы RICIS. После построения каждое дерево проходит обычный конвейер: L1 → SP2 → O(1) → A1/A4 → A5/A6/A7.

## API

Все операции имеют форму `Expression<Func<T,T>>`, где `T : INumber<T>`.

| Операция | API | Производное дерево |
|---|---|---|
| Композиция | `F.Compose(G)` | `F∘G`; тело `G` подставляется вместо параметра `F` |
| Явная подстановка | `F.At(G)` | Точный alias `Compose`; запись `F[G]` |
| Разность | `F.Difference(G)` | `F−G`; одинаковые нормализованные деревья сразу дают типизированный `0` |
| Отношение | `F.Ratio(G)` | `F/G`; L1/SP2/A1 применяются в обычном порядке |
| Произведение | `F.Product(G)` | `F·G`; обычное структурное произведение, не геометрический Integral |

```csharp
Expression<Func<double, double>> f = x => x + 1;
Expression<Func<double, double>> g = y => y - 1;

var composition = f.Compose(g); // y => ((y - 1) + 1)
var witnessZero = f.Difference(f); // x => 0
var witnessOne = ((Expression<Func<double, double>>)(z => z / z))
    .Ratio(z => z / z); // z => 1
var product = f.Product(g); // x => ((x + 1) * (x - 1))
```

## Нормативные свойства

`Compose` и `At` делают подстановку исключительно через обход дерева параметров. `Difference`, `Ratio` и `Product` сначала нормализуют каждый вход независимо; параметр правой лямбды затем структурно перепривязывается к параметру левой. Поэтому исходные `F` и `G` не мутируют и могут участвовать в следующей операции как самостоятельные сущности.

| Свойство | Гарантия |
|---|---|
| Тождество | `F.Ratio(F) → 1` через Phase 0 до мостов сингулярностей |
| Нулевой свидетель | `F.Difference(F) → 0` после сравнения нормализованных деревьев |
| Generic math | `int`, `decimal`, `BigInteger` и иной `INumber<T>` не приводятся к `double` |
| Пользовательские операторы | Конвейер сохраняет классическую семантику не-intrinsic numeric operators |
| Пределы и Лопиталь | Не используются |

## Регрессии и консоль

Набор `RicisProofOperationsSuite` содержит PROOF01–PROOF06: композицию, alias `At`, нулевой свидетель, L1 для отношения, `BigInteger`-произведение и L1 внешней функции до подстановки.

```bash
dotnet run --project Ricis.Console/Ricis.Console.csproj -c Release -- --proof-demo
```
