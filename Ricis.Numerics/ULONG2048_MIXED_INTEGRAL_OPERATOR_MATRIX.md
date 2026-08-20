# ULong2048 Mixed-Integral Operator Matrix

**Status:** Implemented and direct-tested; generated operator surface is the normative source contract. Performance claims remain bounded by the recorded benchmark evidence.
**Evidence:** [`ULong2048.cs`](ULong2048.cs), [`ULong2048.IntegralOperators.generated.cs`](ULong2048.IntegralOperators.generated.cs), [`scripts/generate_ulong2048_integral_operators.py`](../scripts/generate_ulong2048_integral_operators.py), [`Ricis.Numerics.UnitTests/ULong2048Suite.cs`](Ricis.Numerics.UnitTests/ULong2048Suite.cs), and [`Ricis.Numerics.UnitTests/ULong2048ShiftRootSuite.cs`](Ricis.Numerics.UnitTests/ULong2048ShiftRootSuite.cs).
**Current gate:** 386/386 Core regression, 18/18 Finance regression, 8/8 Lean artifacts; no performance improvement is claimed without benchmark evidence.

## Purpose

`ULong2048` represents the exact unsigned fixed-width interval `0..2^2048-1`. This document defines the mixed-operand surface with every built-in C# integer category, while preventing silent loss of sign or range information.

> The conversion rule is **result-domain first**. An operation that is naturally closed in `0..2^2048-1` returns `ULong2048`. An operation involving a signed operand returns `BigInteger`, which is the only existing project type that represents every mathematically valid result without narrowing a full unsigned 2048-bit value.

## Operand families

| Family | C# types | Conversion path | Arithmetic result |
|---|---|---|---|
| Unsigned fixed-width | `byte`, `ushort`, `uint`, `ulong`, `nuint`, `UInt128` | Exact `ULong2048` promotion | `ULong2048` wrapping result; checked forms throw on overflow |
| Signed fixed-width | `sbyte`, `short`, `int`, `long`, `nint`, `Int128` | Exact `BigInteger` promotion | Exact `BigInteger` result; no accidental negative-to-unsigned reinterpretation |
| Arbitrary precision | `BigInteger` | Existing explicit interoperability boundary | Exact `BigInteger` result |

## Generated operator surface

The generated partial source supplies both operand orders where C# permits them.

| Operator group | Unsigned family | Signed family |
|---|---|---|
| `+`, `-`, `*`, `/`, `%` | `ULong2048`; `checked +`, `checked -`, `checked *` are also supplied | `BigInteger` exact result |
| `&`, `|`, `^` | `ULong2048` | `BigInteger` exact two's-complement result |
| `==`, `!=`, `<`, `<=`, `>`, `>=` | Exact limb comparison | Exact `BigInteger` comparison |
| `<<`, `>>`, `>>>` | Existing `int` contract plus generated integral shift-count adapters | Negative signed shift count delegates to the existing inverse-direction rule |

The generated source is checked in and regenerated deterministically by `scripts/generate_ulong2048_integral_operators.py`. This is the DRY source of truth for all type/operator combinations.

## Overflow and division contracts

Ordinary unsigned operations use the same modulo-`2^2048` semantics as `ULong2048` native operators. Checked unsigned addition, subtraction and multiplication throw `OverflowException` whenever the mathematical result is outside the unsigned 2048-bit domain. Division and remainder preserve the existing `DivideByZeroException` contract.

Signed mixed operations do not have a checked/wrapping ambiguity because the result type is `BigInteger`; the exact result is returned for either operand order.

## Test obligations

Direct Test Explorer coverage verifies each family, both operand orders, wrapping/checked boundaries, comparison symmetry, bitwise behavior, integral shift adapters, signed-negative inputs and generated-source freshness. `BigInteger` is used as the external exact oracle for every mixed-domain scenario.
