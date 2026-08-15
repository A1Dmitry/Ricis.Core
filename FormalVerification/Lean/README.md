# Lean-проверка цепочки ID-01–ID-06

`RicisIdentity/TypeIdentity.lean` — независимая Lean-модель нормативной цепочки самоидентификации, отражения и точной рациональной координаты.

## Формальный контракт

Структура `TypeIdentityAxioms` явно содержит три нормативных поля:

1. `reflectionCoordinate` — ID-02: `reflect(sigma)=1-sigma`;
2. `identityPreservesType` — ID-01: отражение тождественной сущности сохраняет тип;
3. `typeCoordinateFaithful` — ID-03: равенство типов идентифицирует координату.

Из них Lean без `sorry` выводит:

| Lean-теорема | Нормативный шаг |
|---|---|
| `id04_linear_pair` | `sigma+reflect(sigma)=1` и `sigma-reflect(sigma)=0` |
| `id05_doubled_coordinate` | `2*sigma=1` |
| `id06_exact_half` | `sigma=1/2` как точное рациональное равенство |
| `id06_reflected_exact_half` | `reflect(sigma)=1/2` |

Эта модель проверяет, что заявленная аксиоматическая цепочка замыкается логически. Она не содержит `sorry`; поля структуры являются **явно переданным нормативным фундаментом**, а не скрытыми допущениями.

## Воспроизведение

Требуется Lean `v4.33.0` и Mathlib указанной версии.

```bash
export PATH="$HOME/.elan/bin:$PATH"
cd FormalVerification/Lean
lake update
lake env lean RicisIdentity/TypeIdentity.lean
```

Последняя команда должна завершиться без ошибок и напечатать список фундаментальных аксиом Lean для `id06_exact_half`; в частности, `sorryAx` в нём отсутствует.
