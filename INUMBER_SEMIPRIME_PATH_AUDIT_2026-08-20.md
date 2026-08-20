# INumber Semiprime Path Audit — 2026-08-20

**Status:** Evidence note for the Universal `INumber<T>` Reduction sprint. No production implementation decision is made by this document.

## Evidence supplied by the user

The supplied materials describe the N-only Fermat factorization invariant

```text
Δ(x) = x² − N = y²
P = x − y
Q = x + y
```

and identify a parity-step recurrence

```text
Δ(x + 2) = Δ(x) + 4x + 4.
```

They also describe bounded candidate regions and pre-root filters. These materials are treated as user-provided research context, not as automatic production-code instructions or formal proof of an O(1) factorization claim.

## Repository comparison

`Ricis.Core/Solvers/Fermat/FermatFactorizer.cs` implements the same N-only difference-of-squares reconstruction:

* it accepts only `BigInteger n`;
* it starts at `ceil(sqrt(N))`;
* it keeps the exact invariant `x² − N = y²`;
* it reconstructs `P=x−y`, `Q=x+y` and verifies `P·Q=N`;
* it uses an exact square-residue modulo-64 prefilter and increments `x` by one.

`RegressionTests/RicisFermatSystemSuite.cs` directly tests these invariants, including all exact square residue classes modulo 64.

## Direct 2048-bit compatibility probe

A temporary compile-only program referenced both `Ricis.Core` and `Ricis.Numerics` and attempted:

```csharp
var semiprime = ULong2048.FromBigInteger(17 * 19);
_ = FermatFactorizer.Solve(semiprime);
```

The compiler correctly returned:

```text
CS1503: cannot convert from 'Ricis.Numerics.ULong2048'
to 'System.Numerics.BigInteger'
```

Therefore the current public Fermat solver is **not** an `INumber<T>`-generic algorithmic entry point. Introducing an explicit conversion solely to make the probe compile would test the `BigInteger` boundary instead of the 2048-bit type and would violate the current sprint’s no-fallback requirement.

## Consequence for the current sprint

The valid proof path for `Int2048` and `ULong2048` is the universal generic Core reduction API. A future generic Fermat solver is a separate architectural increment that must define exact generic square-root, bit-length, residue-filter and ordering capabilities; `INumber<T>` alone does not provide all of those operations.
