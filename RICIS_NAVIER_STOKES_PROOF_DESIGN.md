# Нормативный proof-контур RICIS: Навье—Стокс

> **Document version:** `0.1.0` (provisional baseline)
> **Created:** `2026-08-15`
> **Last modified:** `2026-08-15`
> **Versioning note:** increment the document version when the normative content changes.


## Цель консольного сценария

Новый проект `Ricis.NavierStokes.Console` будет создавать документное доказательство для гладкого стационарного вихревого поля в трёхмерной несжимаемой системе:

\[
\partial_t\mathbf u+(\mathbf u\cdot\nabla)\mathbf u+\nabla p-\nu\Delta\mathbf u=\mathbf0,
\qquad \nabla\cdot\mathbf u=0.
\]

В сценарии выбираются точные отложенные поля

\[
\mathbf u(x,y,z,t)=(-y,x,0),\qquad
p(x,y,z,t)=\frac{x^2+y^2}{2},\qquad \nu=1.
\]

Все компоненты строятся как `Expression<Func<double,double,double,double,double>>`; численный решатель, пределы, конечно-разностные приближения и Лопиталь не используются.

## Нормативная цепочка

| ID | Инвариант | Символьный результат |
|---|---|---|
| NS-01 | Типовая структура поля скорости | Вектор `u=(u,v,w)` состоит из независимых отложенных expression tree. |
| NS-02 | Несжимаемость | `∂x u+∂y v+∂z w=0`. |
| NS-03 | Стационарность | `∂t\mathbf u=\mathbf0`. |
| NS-04 | Конвективный перенос | `(\mathbf u\cdot\nabla)\mathbf u=(-x,-y,0)`. |
| NS-05 | Градиент давления | `\nabla p=(x,y,0)`. |
| NS-06 | Вязкий член | `\Delta\mathbf u=\mathbf0`. |
| NS-07 | RICIS-сокращение остатка | Каждый компонент `∂t\mathbf u+(\mathbf u\cdot\nabla)\mathbf u+\nabla p-\nu\Delta\mathbf u` нормализуется до типизированного нуля. |

## QA-критерий недостающих функций

Сценарий считается обнаружившим недостающую функцию, если один из NS-01–NS-07 не может быть выражен чистым expression tree либо не нормализуется обычным конвейером RICIS. Целевые операции: частная производная, градиент, дивергенция, лапласиан, конвективная производная, векторное сложение, масштабирование и остаток системы.

> Сценарий сертифицирует точную RICIS-идентичность для заданного гладкого поля. Он не выполняет численного моделирования и не заменяет внешнюю постановку о всех возможных начальных данных.[1]

## Reference

[1] [Clay Mathematics Institute — Navier-Stokes Equation](https://www.claymath.org/millennium/navier-stokes-equation/)
