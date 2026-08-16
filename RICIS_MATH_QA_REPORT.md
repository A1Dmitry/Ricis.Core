# Математический QA-аудит Ricis.Core

> **Document version:** `0.1.0` (provisional baseline)
> **Created:** `2026-08-16`
> **Last modified:** `2026-08-16`
> **Versioning note:** increment the document version when the normative content changes.


## 1. Объём проверки

Аудит выполнен по публичным математическим API, expression extensions, solver-ам, vector/matrix/calculus слоям, proof API, каноническому `RICIS_III_CONCEPT.md`, матрице `RICIS_RULE_COVERAGE.md` и всем regression suites в `RegressionTests`.

Проверялась не только наличие метода, но и его соответствие приоритету RICIS III: `L0/L1 → SP1–SP4 → RICIS bridges/A-rules → structural algebra → constant bridges → classical fallback`.

> Классический fallback допустим только там, где RICIS не задаёт собственного правила. Для `0_F`, `∞_F`, сингулярных ключей, indexed payload и identity-first equality классический результат не может заменить RICIS-результат.

## 2. Baseline

| Проверка | Результат |
|---|---|
| Release solution build | Успешно, 0 errors, 0 warnings |
| Regression suite | **304/304 passed** |
| Первичный Axiom Gate | AX01–AX07 passed |
| Предыдущий-параметрический identity | PREV01–PREV03 passed |
| Analytic sugar | AN01–AN06 passed |
| Complex layer | CPLX01–CPLX07 passed |
| Continuous sugar | SUGAR01–SUGAR08 passed |
| Vector layer | VECTOR01–VECTOR08 passed |
| Navier–Stokes proof field | NS01–NS08 passed |
| `git diff --check` | Passed |

По текущему baseline непосредственных regression failures не обнаружено. Найденные ниже пункты являются **coverage gaps и математическими obligations**, а не утверждением о уже существующем runtime-баге.

## 3. Уже покрытые публичные группы

| Группа | Реализованные функции | QA-статус |
|---|---|---|
| RICIS identity и structural algebra | `F/F`, `0/0`, common factors, nested ratios, difference of powers, factorial cancellation, indexed zero/infinity | Покрыто RC, AX и priority suites |
| Singular bridges | `F·0`, `F/0`, `F/∞_G`, `0_F/0_G`, `∞_F/∞_G`, `0_F·∞_G`, A7 operations | Покрыто KnownRicisLimits, RuleContract, Axiom Gate |
| Root/singularity handling | Polynomial, trigonometric, logarithmic, exponential roots; multiple keys; SP4 expression indexing | Покрыто RC24–RC31, stress and singularity suites |
| Continuous sugar | `Abs`, `Min`, `Max`, `Clamp`, `PositivePart`, `NegativePart`, `Distance` | SUGAR01–SUGAR08 |
| Analytic scalar sugar | `Sin`, `Cos`, `Tan`, `Sinh`, `Cosh`, `Tanh`, `Exp`, `Log`, `Log10`, `Sqrt`, constant/delayed `Pow` | AN01–AN06 |
| Symbolic calculus | `DxDt`, `Derivative`, partial derivative, gradient, divergence, Laplacian, time and convective derivatives | Derivative and NS suites |
| Complex expressions | `AsComplex`, `Re`, `Im`, conjugation, add/subtract/multiply, squared norm, norm | CPLX01–CPLX07 |
| Vectors/matrices | Generic vector storage, vector add/subtract/scale/dot, dimensions, BigInteger vector support, matrix expression contracts | Vector and matrix suites |
| Proof/document/Lean | `Prove`, `ProveDocument`, proof operations, system solving, RH conditional case, canonical LeanDoc and A6 LeanDoc | Academic, proof, Lean and RH suites |
| Application surfaces | Console parser, expression systems, Web API, Swagger, WebAssembly, Navier–Stokes console | Integration/API coverage |

## 4. Недостающие функции и obligations

### P0 — необходимо для замыкания текущего математического ядра

| Gap | Почему это важно для RICIS | Предлагаемый API и обязательные тесты |
|---|---|---|
| Domain-aware analytic nodes | `Log`, `Sqrt`, `Tan` и `Pow` уже строятся как `Math.*`, но отдельный нормативный contract для `Log(0)`, `Log(negative)`, `Sqrt(negative)`, `0^0`, negative-base fractional power и poles не выделен | `DomainOf`, `IsDefinedAt`, `IsPoleAt`, `IsBranchPoint`; тесты должны сначала применять SP2/SP4, затем RICIS bridge или явный classical rejection |
| Exact remainder/modulo | Остаток упоминался как special structural operation, но нужен отдельный публичный algebraic contract для `F % G`, нулевого divisor и одинаковых remainder trees | `Remainder`, `Modulo`; тесты ID-04, `F%G/(F%G)`, `F%0`, `0%G`, zero-index preservation |
| Inverse trigonometric functions | `Sin/Cos/Tan` покрыты, но inverse branch semantics и их singular/domain keys не представлены | `Asin`, `Acos`, `Atan`, `Atan2`; tests for finite points, branch/domain metadata and `Tan(Atan(F))` only under explicit domain premise |
| Complete ratio/pole classification | Current A1/A4/A5/A6 bridges are covered, but a public classification result for finite, indexed-zero, indexed-infinity, pole and rejected-domain states is missing | `RicisValueKind`, `Classify(Expression)`; test that classification never discards payload or converts incompatible types |
| Structured conditional/branch node | `Clamp` covers a limited conditional shape; general piecewise functions are not first-class | `Piecewise`, `Condition`, `Select`; test structural identity before branch evaluation and SP4 indexing of branch expressions |

### P1 — следующий слой функциональной полноты

| Gap | QA-обоснование | Предлагаемый API |
|---|---|---|
| Vector calculus completion | Есть gradient/divergence/Laplacian, но отсутствуют явные curl, Jacobian, Hessian и directional derivative contracts | `Curl`, `Jacobian`, `Hessian`, `DirectionalDerivative`; tests for dimensions, parameter order, structural zero and indexed singular components |
| Vector algebra completion | Есть generic dot and scalar scaling, но нужен отдельный нормативный contract для cross product, outer product and projection | `Cross`, `OuterProduct`, `Project`; tests for 2D rejection, 3D orientation, BigInteger and custom `INumber<T>` |
| Matrix algebra completion | Matrix expression surface не образует полного public algebra contract без отдельной проверки determinant, trace, inverse and solve | `Determinant`, `Trace`, `Inverse`, `Solve`; tests must reject singular inverse classically but preserve RICIS indexed singular metadata where applicable |
| Complex polar layer | Есть Re/Im/conjugation/norm, но нет first-class argument, phase and polar conversion | `Argument`, `Phase`, `ToPolar`, `FromPolar`; tests for branch cut, zero complex payload and conjugate symmetry |
| Exact rational public type | Proof layer has an exact rational internally, but `1/2` and other finite fractions should have an explicit public representation for generic proofs and Lean export | Public `RicisRational` or equivalent; tests for normalization, sign, denominator nonzero and no premature `double` conversion |
| General discrete operations | `Factorial` exists, but no complete contract for `Gcd`, `Lcm`, binomial, permutations or rising/falling factorial | Add only after deciding whether each operation is RICIS sugar or classical fallback; provide BigInteger-first tests |
| Root result model | Solvers return different shapes (`Root`, tuples, optional first root); a unified set/result model is needed for multiple roots, multiplicity and domain status | `RicisRootSet`, `RootMultiplicity`, `RootStatus`; tests preserve all keys and distinguish repeated roots |

### P2 — расширение аналитического и прикладного покрытия

| Gap | Предлагаемый scope |
|---|---|
| Rounding/discontinuous functions | `Floor`, `Ceiling`, `Round`, `Truncate`, `Sign`; define whether equality is structural only and how discontinuity keys are represented |
| Special functions | `Erf`, `Gamma`, `Beta`, Bessel functions and factorial extension; each requires explicit domain/pole metadata before being exposed as RICIS sugar |
| Sequences and series | `Sequence`, finite sum/product, recurrence and convergence metadata; do not silently introduce limits because RICIS has a separate O(1) bridge contract |
| Fourier/Laplace transforms | Only after a typed transform node and domain/linearity obligations exist; ordinary delegate evaluation is insufficient |
| Probability/statistics | Mean, variance, covariance and distribution functions should be separate domain models, not added to scalar algebra without semantic types |

## 5. Найденные математические узкие места

### 5.1 Domain semantics are less explicit than algebraic semantics

The algebraic singularity rules are well covered, but domain-invalid analytic expressions need the same first-class treatment. A test that merely calls `Math.Log(-1)` or `Math.Sqrt(-1)` would exercise classical runtime behavior, not a RICIS rule. The correct implementation needs an expression-level domain/pole result before compilation.

### 5.2 The generic numeric layer is uneven

Vectors and several scalar operations support `INumber<T>`, while analytic sugar and complex norm remain concentrated on `double`. This is acceptable as an explicit boundary, but it should be represented in the public type system. Otherwise users may infer generic support that the operation cannot provide.

### 5.3 Root solvers need one semantic result model

Polynomial, trigonometric, logarithmic and exponential solvers exist, but a unified result model for all roots, multiplicity, domain validity and source-expression indexing would make SP4 and A1 verification easier. Returning only the first root is insufficient for a theorem that claims complete singularity coverage.

### 5.4 Integral and Sum are intentionally not general calculus

The current `Integral` is the RICIS geometric A6 construction `0_F·∞_L → F·L`, and `Sum` is a structural operation over delayed expressions. They should not be silently expanded into conventional antiderivative or convergence engines. If generalized, the new nodes must keep their RICIS semantics explicit and separate from classical analysis.

### 5.5 RH and Navier–Stokes remain conditional proof cases

The proof engine correctly proves its algebraic residual once the domain-specific obligations are supplied. It does not itself supply analytic continuation, functional equations, PDE regularity or universal quantification. This is an architecture boundary, not a missing generic simplification rule.

## 6. Recommended implementation order

The first implementation tranche should be **domain-aware analytic classification**, exact modulo/remainder, inverse trigonometric nodes and a unified root result. These items directly protect existing RICIS bridges from losing domain or key metadata.

The second tranche should complete vector/matrix calculus with `Curl`, `Jacobian`, `Hessian`, `DirectionalDerivative`, determinant and inverse contracts. The third tranche can add complex polar operations and a public exact rational type. Discrete, special-function and transform APIs should remain behind explicit design discussions because they introduce new semantic domains rather than merely adding algebraic sugar.

## 7. QA verdict

The current public mathematical surface has strong regression protection for the implemented RICIS subset: the baseline build is clean and all 304 regression contracts pass. The principal risk is not an uncovered basic algebraic identity; it is **semantic incompleteness at analytic domains, roots, type promotion and higher-dimensional operators**.

No new operation should be added as an unqualified classical wrapper. Every new function must declare its domain, its RICIS behavior at zero/pole/infinity, its payload/index policy, its generic numeric boundary and at least one priority test proving that L1/SP rules execute before fallback evaluation.
