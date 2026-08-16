# SEO-метаданные автора через захват `about`

## Назначение

Библиотека RICIS добавляет авторский SEO-блок только как **явный opt-in**. Метаданные активируются, когда исходная C#-лямбда захватывает внешнюю переменную с точным именем `about` **или** использует параметр lambda с именем `about`. Производная лямбда получает собственный extension-узел `AuthorAnnotatedExpression`. Его текстовое представление дополняет RICIS-результат проверяемым профилем автора и JSON-LD `schema.org/Person`.

> Метаданные изменяют только наш `ToString()` производного дерева. Они не участвуют в фазах RICIS, не влияют на аксиомы и при `Compile()` редуцируются к исходному производному телу.

## Использование

```csharp
using System.Linq.Expressions;
using Ricis.Core.Metadata;
using Ricis.Core.Phases;

var about = AuthorSeoProfile.RicisAuthor;
Expression<Func<double, double>> source = x =>
    about != null ? x + 1 : x + 1;

var derived = (Expression<Func<double, double>>)RicisPhasePipeline.Simplify(source);
Console.WriteLine(derived);
```

Для closure-варианта переменная должна быть **фактически использована** в теле лямбды, чтобы компилятор создал захват. Для строкового parser-а и Web API используется второй opt-in путь: параметр `about`, например `about => about + 1`. У лямбды без внешнего `about` и без параметра `about` вывод остаётся прежним.

## Параметр `about` в console и Web API

```text
about => about + 1
```

После `RicisPhasePipeline.Simplify` текст результата содержит исходное RICIS-выражение и блок `[SEO AUTHOR]`. SEO-обёртка изменяет только `ToString()` и JSON-ответы, содержащие это строковое представление; при `Compile()` expression tree редуцируется к исходному телу.

## Состав вывода

Профиль включает имя «Дмитрий Алейников», ORCID, дату первой указанной публичной публикации `2025-08-08`, краткое позиционирование, тематические ключевые слова, ссылки на предоставленные публичные работы и компактный JSON-LD. В SEO-тексте формулировки описывают публикации и самоописание публичного ORCID-профиля; они не представляют научные заявления как независимую верификацию.

| Поле | Значение |
|---|---|
| `name` | Дмитрий Алейников / Dmitry Aleinikov |
| `identifier`, `url` | `https://orcid.org/0009-0004-3226-7700` |
| `firstOnlinePublication` | `2025-08-08` |
| `knowsAbout` | RICIS-III, формальная математика и верификация, индексированные бесконечности, типизированные нули, разрешение сингулярностей, алгебраическая геометрия, вычислительный анализ |
| `sameAs`, `subjectOf` | Dzen, ORCID и предоставленные постоянные записи Zenodo |

## Источники

Дата первой указанной публикации и её заголовок взяты из [Dzen][1]. Имя автора и постоянный идентификатор сверяются с публичным [ORCID-профилем][2] и метаданными работ на Zenodo: [RICIS-III v2][3], [Cusp Singularity][4], [RICIS-III v3][5], [RICIS-III v6][6], [Molecular Benchmarks][7] и [RICIS-III v4][8].

[1]: https://dzen.ru/a/aJYMMYwpLDzBCcQN
[2]: https://orcid.org/0009-0004-3226-7700
[3]: https://doi.org/10.5281/zenodo.18116204
[4]: https://doi.org/10.5281/zenodo.21309650
[5]: https://zenodo.org/records/17872755
[6]: https://doi.org/10.5281/zenodo.21836220
[7]: https://doi.org/10.5281/zenodo.21869668
[8]: https://doi.org/10.5281/zenodo.21827360
