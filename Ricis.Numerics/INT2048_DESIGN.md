# Ricis.Numerics Fixed-Width 2048-bit Contract

## Domain

`Int2048` is a signed, fixed-width, 2048-bit integer with the exact two's-complement range:

```text
MinValue = −2^2047
MaxValue =  2^2047 − 1
```

The production representation is thirty-two unsigned 64-bit limbs in little-endian order. `BigInteger` is a **first-class explicit interoperability boundary**: callers can use `FromBigInteger`, `ToBigInteger`, explicit conversions and mixed overloads where the exact result intentionally remains `BigInteger`. It is not the stored representation, and same-type fixed-width arithmetic does not delegate its production result to `BigInteger`.

## Unsigned RSA magnitude domain

`ULong2048` is the unsigned companion with exact range `0..2^2048−1`. It is the canonical type for RSA modulus, public exponent and signature representative. Its representation is `[InlineArray(32)]` `ulong` limbs embedded directly in the value type: normal add/subtract/shift hot paths are allocation-free and are guarded by a direct allocation regression test. It supplies custom fixed-width addition, subtraction, multiplication, long division, modulo, modular multiplication and the raw public RSA operation `s^e mod n`.

For odd moduli — the normal RSA case — modular multiplication and exponentiation use inline-limb Montgomery reduction with a stack-only 65-limb scratch area. Even moduli retain a mathematically exact generic fallback. Neither custom route stores values in `BigInteger`.

> `RsaPublicOperation` is the mathematical RSAVP1-style primitive, not a complete signature verifier. RSA-PSS and PKCS#1 v1.5 encoding/hash verification remain separate security contracts and must not be inferred from successful modular exponentiation alone.

## Arithmetic semantics

| Operation | Ordinary operator | Checked operator / method | Required invariant |
|---|---|---|---|
| Addition / subtraction | 2048-bit two's-complement wrap | throws `OverflowException` | Carry/borrow travels through all 32 limbs |
| Unary negation | two's-complement wrap | `checked(-MinValue)` throws | `x + (-x) = 0` except checked `MinValue` |
| Multiplication | low 2048 bits | throws if product is out of range | 32×32 limb partial products with carry propagation |
| Division | truncates toward zero | same range behavior; `MinValue / -1` throws | `a = (a / b) * b + (a % b)` |
| Remainder | sign follows dividend | same | `|a % b| < |b|` for nonzero divisor |
| Comparison | signed two's-complement ordering | n/a | sign limb is compared before unsigned magnitude ordering |

Division is implemented by a custom magnitude long-division algorithm. Sign transfer is applied once after quotient/remainder magnitudes are established; no arithmetic operator delegates its production behavior to `BigInteger`.

## `INumber<Int2048>` contract

The type implements `INumber<Int2048>`, `ISignedNumber<Int2048>`, parsing/formatting interfaces, comparison/equality interfaces, and all inherited operator/conversion requirements. Integral-specific `IBinaryInteger<Int2048>` is intentionally not claimed in this first contract because it adds bit-count, endian byte I/O and full binary-integer semantics beyond the user-requested `INumber` surface. No API is advertised as implemented until it is complete and tested.

| Surface | Contract |
|---|---|
| `Zero`, `One`, `NegativeOne`, `AdditiveIdentity`, `MultiplicativeIdentity`, `Radix` | Exact fixed constants |
| `Abs`, `CopySign`, `Min`, `Max`, `Clamp`, sign predicates | Fixed-width signed semantics; `Abs(MinValue)` throws |
| `CreateChecked`, `CreateSaturating`, `CreateTruncating` | Checked/saturating/modulo conversion semantics |
| `TryConvertFrom*`, `TryConvertTo*` | Supported primitive, `BigInteger`, and `Int2048` conversions; unsupported types return `false` |
| `Parse`, `TryParse`, `ToString`, `TryFormat` | Culture-aware decimal entry points; numeric storage remains custom limbs |
| Operators | Addition, subtraction, multiplication, division, modulus, unary sign, increment/decrement, comparison and equality |

## Comparative performance evidence

`Ricis.Numerics.Benchmarks` runs deterministic 2048-bit comparisons against `BigInteger`, checks result parity **before** timing and records both elapsed time and managed allocations in JSON/Markdown. The evidence is intentionally non-gating: CPU, allocator, JIT and host contention make wall-clock thresholds unsuitable for CI. `ULONG2048_PREINLINE_BASELINE_2026-08-20.{md,json}` records the prior heap-backed baseline; `ULONG2048_MONTGOMERY_2026-08-20.{md,json}` records the inline/Montgomery result.

The optimized `ULong2048` hot path now has no per-operation managed allocation, and Montgomery improves raw RSA public exponentiation materially over the original long-division-based modular path. The runtime-optimized `BigInteger` still remains faster in the measured large multiply/divide/modular operations; that is an evidence result, not a claim to be hidden or relaxed by a threshold.

## Direct-test obligations

Every public member is covered directly. Tests use `BigInteger` only as an external oracle and verify at least: limb-boundary carry, borrow through zero limbs, sign-transfer cases, division/remainder identity, `MinValue` edge behavior, overflow behavior, parse/format round-trip, generic `INumber<T>` execution, full 2048-bit boundary values, ULong2048 allocation-free hot operators, and Montgomery parity over small, mid-width and full 2048-bit odd modulus shapes.

> The implementation must fail closed: any unrepresentable checked operation throws rather than silently narrowing, and no `BigInteger` result may become the stored representation of `Int2048`.
