# TASK TIME EVIDENCE — UTILITY COVERAGE 2026-08-20

**TaskId:** `UTILITY-COVERAGE-2026-08-20-01`
**StartUtc:** 2026-08-20T15:45:54Z

**ComplexityClass:** `M`
**PlannedHours:** `2.0`
**DependencyRisk:** `Low`
**Scope:** one public utility family with direct named tests, no API deletion and no external dependency.

## Candidate gaps

[35mCircleSectors.cs[m[36m:[m[32m7[m[36m:[m/// Represents the RICIS public type <c>[1;31mCircleSectors[m</c>.
[35mCircleSectors.cs[m[36m:[m[32m9[m[36m:[mpublic readonly struct [1;31mCircleSectors[m
[35mCircleSectors.cs[m[36m:[m[32m12[m[36m:[m    /// Gets the <c>Fraction</c> value of <c>[1;31mCircleSectors[m</c>.
[35mCircleSectors.cs[m[36m:[m[32m16[m[36m:[m    private [1;31mCircleSectors[m(Rational fraction)
[35mCircleSectors.cs[m[36m:[m[32m30[m[36m:[m    public static [1;31mCircleSectors[m FromRadians(double radians, int maxDenominator = 100)
[35mCircleSectors.cs[m[36m:[m[32m42[m[36m:[m        return new [1;31mCircleSectors[m(best);
[35mExactEvaluator.cs[m[36m:[m[32m9[m[36m:[m/// Represents the RICIS public type <c>[1;31mExactEvaluator[m</c>.
[35mExactEvaluator.cs[m[36m:[m[32m11[m[36m:[mpublic static [1;31mclass ExactEvaluator[m
[35mExecution/NumericalEvaluationSafety.cs[m[36m:[m[32m60[m[36m:[m            if ((node.Method is not null && ![1;31mNumericConstants[m.IsIntrinsicNumeric(node.Type)) ||
[35mExpressions/RicisComplexFunction.cs[m[36m:[m[32m40[m[36m:[m        [1;31mNumericConstants[m.Register<T>();
[35mExtensions/ExpressionExtensions.cs[m[36m:[m[32m32[m[36m:[m        [1;31mNumericConstants[m.Register<T>();
[35mExtensions/ExpressionExtensions.cs[m[36m:[m[32m324[m[36m:[m        _ => [1;31mNumericConstants[m.IsZero(value)
[35mExtensions/ExpressionExtensions.cs[m[36m:[m[32m332[m[36m:[m        _ => [1;31mNumericConstants[m.IsOne(value)
[35mExtensions/RicisAcademicProofExtensions.cs[m[36m:[m[32m115[m[36m:[m        [1;31mNumericConstants[m.Register<T>();
[35mExtensions/RicisAcademicProofExtensions.cs[m[36m:[m[32m1635[m[36m:[m            (product.Method is null || [1;31mNumericConstants[m.IsIntrinsicNumeric(product.Type)))
[35mExtensions/RicisAcademicProofExtensions.cs[m[36m:[m[32m1647[m[36m:[m        0 => [1;31mNumericConstants[m.OneOf(scalarType),
[35mExtensions/RicisComplexExtensions.cs[m[36m:[m[32m27[m[36m:[m        [1;31mNumericConstants[m.Register<T>();
[35mExtensions/RicisComplexExtensions.cs[m[36m:[m[32m29[m[36m:[m            [1;31mNumericConstants[m.ZeroOf(typeof(T)),
[35mExtensions/RicisCompoundInterestExtensions.cs[m[36m:[m[32m39[m[36m:[m            [1;31mNumericConstants[m.OneOf(typeof(T)),
[35mExtensions/RicisCompoundInterestExtensions.cs[m[36m:[m[32m42[m[36m:[m        Expression multiplier = [1;31mNumericConstants[m.OneOf(typeof(T));
[35mExtensions/RicisCompoundInterestExtensions.cs[m[36m:[m[32m180[m[36m:[m        [1;31mNumericConstants[m.Register<T>();
[35mExtensions/RicisContinuousExtensions.cs[m[36m:[m[32m29[m[36m:[m        var zero = [1;31mNumericConstants[m.ZeroOf(typeof(T));
[35mExtensions/RicisContinuousExtensions.cs[m[36m:[m[32m78[m[36m:[m        [1;31mNumericConstants[m.Register<T>();
[35mExtensions/RicisContinuousExtensions.cs[m[36m:[m[32m131[m[36m:[m        var zero = [1;31mNumericConstants[m.ZeroOf(typeof(T));
[35mExtensions/RicisContinuousExtensions.cs[m[36m:[m[32m147[m[36m:[m        var zero = [1;31mNumericConstants[m.ZeroOf(typeof(T));
[35mExtensions/RicisContinuousExtensions.cs[m[36m:[m[32m198[m[36m:[m        [1;31mNumericConstants[m.Register<T>();
[35mExtensions/RicisContinuousExtensions.cs[m[36m:[m[32m212[m[36m:[m        [1;31mNumericConstants[m.Register<T>();
[35mExtensions/RicisContinuousExtensions.cs[m[36m:[m[32m230[m[36m:[m        [1;31mNumericConstants[m.Register<T>();
[35mExtensions/RicisDerivativeExtensions.cs[m[36m:[m[32m38[m[36m:[m        [1;31mNumericConstants[m.Register<T>();
[35mExtensions/RicisIntegralExtensions.cs[m[36m:[m[32m32[m[36m:[m        [1;31mNumericConstants[m.Register<T>();
[35mExtensions/RicisIntegralExtensions.cs[m[36m:[m[32m103[m[36m:[m        [1;31mNumericConstants[m.Register<T>();
[35mExtensions/RicisProofExtensions.cs[m[36m:[m[32m57[m[36m:[m            ? [1;31mNumericConstants[m.ZeroOf(typeof(T))
[35mExtensions/RicisProofExtensions.cs[m[36m:[m[32m112[m[36m:[m        [1;31mNumericConstants[m.Register<T>();
[35mNumericConstants.cs[m[36m:[m[32m14[m[36m:[mpublic static [1;31mclass NumericConstants[m
[35mNumericConstants.cs[m[36m:[m[32m24[m[36m:[m    static [1;31mNumericConstants[m()
[35mNumericConstants.cs[m[36m:[m[32m123[m[36m:[m                "Вызовите [1;31mNumericConstants[m.Register<T>() или используйте generic API конечного исполнения.");
[35mPolarConverter.cs[m[36m:[m[32m1[m[36m:[m// RicisCore/[1;31mPolarConverter[m.cs — strict RICIS polar collapse (no limits)
[35mPolarConverter.cs[m[36m:[m[32m17[m[36m:[mpublic static [1;31mclass PolarConverter[m
[35mPolarConverter.cs[m[36m:[m[32m72[m[36m:[m        [1;31mCircleSectors[m sector;
[35mPolarConverter.cs[m[36m:[m[32m75[m[36m:[m            sector = [1;31mCircleSectors[m.FromRadians(radians);
[35mPolarConverter.cs[m[36m:[m[32m155[m[36m:[m                var sector = [1;31mCircleSectors[m.FromRadians(value, maxDenominator);
[35mProofs/RicisPreviousParameterIdentityProofCase.cs[m[36m:[m[32m75[m[36m:[m            derivedBody = [1;31mNumericConstants[m.ZeroOf(derivedBody.Type);
[35mRegressionTests/RicisPublicCompatibilitySuite.cs[m[36m:[m[32m13[m[36m:[m        ("API26: [1;31mPolarConverter[m.ToPolarSector renders public singularity view", PolarSectorRendersSingularity),
[35mRegressionTests/RicisPublicCompatibilitySuite.cs[m[36m:[m[32m27[m[36m:[m        var rendered = [1;31mPolarConverter[m.ToPolarSector(infinity, totalSectors: 8);
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m12[m[36m:[m        ("API01: [1;31mExactEvaluator[m вычисляет рациональное expression", [1;31mExactEvaluator[mComputesRationalExpression),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m13[m[36m:[m        ("API02: [1;31mExactEvaluator[m отклоняет неподдерживаемый узел и неизвестный параметр", [1;31mExactEvaluator[mRejectsUnsupportedShape),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m14[m[36m:[m        ("API03: [1;31mCircleSectors[m нормализует радианную четверть и форматирует сектора", [1;31mCircleSectors[mNormalizeAndFormat),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m15[m[36m:[m        ("API04: [1;31mCircleSectors[m отклоняет NaN, infinity и неверное число секторов", [1;31mCircleSectors[mRejectsInvalidInput),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m16[m[36m:[m        ("API05: [1;31mPolarConverter[m возвращает exact sin/cos и оставляет неизвестный сектор", [1;31mPolarConverter[mExactAndUnknownSector),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m17[m[36m:[m        ("API06: [1;31mPolarConverter[m сворачивает sin и pole tan через public API", [1;31mPolarConverter[mCollapsesTrigAndPole),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m18[m[36m:[m        ("API07: [1;31mNumericConstants[m возвращает typed identities и predicates", [1;31mNumericConstants[mExposeTypedIdentities),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m19[m[36m:[m        ("API08: [1;31mNumericConstants[m отклоняет незарегистрированный тип", [1;31mNumericConstants[mRejectsUnregisteredType),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m33[m[36m:[m    private static void [1;31mExactEvaluator[mComputesRationalExpression()
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m45[m[36m:[m    private static void [1;31mExactEvaluator[mRejectsUnsupportedShape()
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m55[m[36m:[m    private static void [1;31mCircleSectors[mNormalizeAndFormat()
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m57[m[36m:[m        var quarter = [1;31mCircleSectors[m.FromRadians(Math.PI / 2);
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m62[m[36m:[m        var negativeQuarter = [1;31mCircleSectors[m.FromRadians(-Math.PI / 2);
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m66[m[36m:[m    private static void [1;31mCircleSectors[mRejectsInvalidInput()
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m68[m[36m:[m        AssertThrows<ArgumentException>(() => [1;31mCircleSectors[m.FromRadians(double.NaN));
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m69[m[36m:[m        AssertThrows<ArgumentException>(() => [1;31mCircleSectors[m.FromRadians(double.PositiveInfinity));
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m70[m[36m:[m        AssertThrows<ArgumentOutOfRangeException>(() => [1;31mCircleSectors[m.FromRadians(0).InSectors(0));
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m73[m[36m:[m    private static void [1;31mPolarConverter[mExactAndUnknownSector()
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m75[m[36m:[m        var exact = [1;31mPolarConverter[m.ExactSinCos(new Rational(1, 4));
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m78[m[36m:[m        var unknown = [1;31mPolarConverter[m.ExactSinCos(new Rational(1, 7));
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m82[m[36m:[m    private static void [1;31mPolarConverter[mCollapsesTrigAndPole()
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m84[m[36m:[m        var sin = [1;31mPolarConverter[m.TryCollapseTrig(nameof(Math.Sin), Math.PI / 2);
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m87[m[36m:[m        var tan = [1;31mPolarConverter[m.TryCollapseTrig(nameof(Math.Tan), Math.PI / 2);
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m92[m[36m:[m        Assert(ReferenceEquals(call, [1;31mPolarConverter[m.CollapseConstantTrig(call)), "Непостоянный аргумент не должен сворачиваться.");
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m95[m[36m:[m    private static void [1;31mNumericConstants[mExposeTypedIdentities()
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m97[m[36m:[m        var zero = [1;31mNumericConstants[m.ZeroOf(typeof(int));
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m98[m[36m:[m        var one = [1;31mNumericConstants[m.OneOf(typeof(int));
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m101[m[36m:[m        var found = [1;31mNumericConstants[m.TryOneOf(typeof(int), out var registered);
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m104[m[36m:[m        Assert([1;31mNumericConstants[m.IsIntrinsicNumeric(typeof(BigInteger)) && ![1;31mNumericConstants[m.IsIntrinsicNumeric(typeof(string)),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m106[m[36m:[m        Assert([1;31mNumericConstants[m.IsZero(0) && [1;31mNumericConstants[m.IsOne(1) && ![1;31mNumericConstants[m.IsOne(0),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m110[m[36m:[m    private static void [1;31mNumericConstants[mRejectsUnregisteredType()
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m112[m[36m:[m        Assert(![1;31mNumericConstants[m.TryOneOf(typeof(string), out _), "TryOneOf(string) должен вернуть false.");
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m113[m[36m:[m        AssertThrows<NotSupportedException>(() => [1;31mNumericConstants[m.ZeroOf(typeof(string)));
[35mRicis.Console/Program.cs[m[36m:[m[32m698[m[36m:[m        var quarter = [1;31mCircleSectors[m.FromRadians(Math.PI / 2);
[35mRicis.Console/Program.cs[m[36m:[m[32m699[m[36m:[m        var polar = [1;31mPolarConverter[m.ExactSinCos(new Rational(1, 4));
[35mRicis.Console/Program.cs[m[36m:[m[32m700[m[36m:[m        var collapsedSin = [1;31mPolarConverter[m.TryCollapseTrig(nameof(Math.Sin), Math.PI / 2);
[35mRicis.Console/Program.cs[m[36m:[m[32m701[m[36m:[m        var zero = [1;31mNumericConstants[m.ZeroOf(typeof(int));
[35mRicis.Console/Program.cs[m[36m:[m[32m702[m[36m:[m        var one = [1;31mNumericConstants[m.OneOf(typeof(int));
[35mRicis.Console/Program.cs[m[36m:[m[32m708[m[36m:[m            (Name: "[1;31mExactEvaluator[m", Passed: exactOk),
[35mRicis.Console/Program.cs[m[36m:[m[32m709[m[36m:[m            (Name: "[1;31mCircleSectors[m", Passed: quarter.Fraction == new Rational(1, 4)),
[35mRicis.Console/Program.cs[m[36m:[m[32m710[m[36m:[m            (Name: "[1;31mPolarConverter[m", Passed: polar.sin == 1.0 && polar.cos == 0.0 && collapsedSin is ConstantExpression { Value: double value } && value == 1.0),
[35mRicis.Console/Program.cs[m[36m:[m[32m711[m[36m:[m            (Name: "[1;31mNumericConstants[m", Passed: zero.Value is int zeroValue && zeroValue == 0 && one.Value is int oneValue && oneValue == 1),
[35mRicis.Console/Program.cs[m[36m:[m[32m716[m[36m:[m        Console.WriteLine($"  [1;31mExactEvaluator[m: {exactValue}");
[35mRicis.Console/Program.cs[m[36m:[m[32m717[m[36m:[m        Console.WriteLine($"  [1;31mCircleSectors[m: {quarter} / {quarter.InSectors(4)}");
[35mRicis.Console/Program.cs[m[36m:[m[32m718[m[36m:[m        Console.WriteLine($"  [1;31mPolarConverter[m: sin={polar.sin:G6}, cos={polar.cos:G6}");
[35mRicis.Console/Program.cs[m[36m:[m[32m719[m[36m:[m        Console.WriteLine($"  [1;31mNumericConstants[m: zero={zero.Value}, one={one.Value}");
[35mRicis.Console/Program.cs[m[36m:[m[32m862[m[36m:[m        Console.WriteLine("  В CLI --public-api-demo проверяет [1;31mExactEvaluator[m, [1;31mCircleSectors[m, [1;31mPolarConverter[m, [1;31mNumericConstants[m и RicisType.");
[35mRicisScalarPolicy.cs[m[36m:[m[32m50[m[36m:[m    public bool IsScalarType(Type type) => [1;31mNumericConstants[m.IsIntrinsicNumeric(type);
[35mRicisScalarPolicy.cs[m[36m:[m[32m52[m[36m:[m    public ConstantExpression ZeroOf(Type type) => [1;31mNumericConstants[m.ZeroOf(type);
[35mRicisScalarPolicy.cs[m[36m:[m[32m54[m[36m:[m    public ConstantExpression OneOf(Type type) => [1;31mNumericConstants[m.OneOf(type);
[35mRicisScalarPolicy.cs[m[36m:[m[32m66[m[36m:[m    public bool IsZeroValue(object value) => value is not null && [1;31mNumericConstants[m.IsZero(value);
[35mRicisScalarPolicy.cs[m[36m:[m[32m68[m[36m:[m    public bool IsOneValue(object value) => value is not null && [1;31mNumericConstants[m.IsOne(value);
[35mRicisScalarPolicy.cs[m[36m:[m[32m71[m[36m:[m        node.Method is null || [1;31mNumericConstants[m.IsIntrinsicNumeric(node.Type);
[35mSimplifiers/PolarTrigVisitor.cs[m[36m:[m[32m26[m[36m:[m        var collapsed = [1;31mPolarConverter[m.CollapseConstantTrig(call);
[35mUnitTests/RicisRegressionCatalogMSTestAdapter.generated.cs[m[36m:[m[32m683[m[36m:[m    [TestMethod("API01: [1;31mExactEvaluator[m вычисляет рациональное expression")]
[35mUnitTests/RicisRegressionCatalogMSTestAdapter.generated.cs[m[36m:[m[32m687[m[36m:[m    [TestMethod("API02: [1;31mExactEvaluator[m отклоняет неподдерживаемый узел и неизвестный параметр")]
[35mUnitTests/RicisRegressionCatalogMSTestAdapter.generated.cs[m[36m:[m[32m691[m[36m:[m    [TestMethod("API03: [1;31mCircleSectors[m нормализует радианную четверть и форматирует сектора")]
[35mUnitTests/RicisRegressionCatalogMSTestAdapter.generated.cs[m[36m:[m[32m695[m[36m:[m    [TestMethod("API04: [1;31mCircleSectors[m отклоняет NaN, infinity и неверное число секторов")]
[35mUnitTests/RicisRegressionCatalogMSTestAdapter.generated.cs[m[36m:[m[32m699[m[36m:[m    [TestMethod("API05: [1;31mPolarConverter[m возвращает exact sin/cos и оставляет неизвестный сектор")]
[35mUnitTests/RicisRegressionCatalogMSTestAdapter.generated.cs[m[36m:[m[32m703[m[36m:[m    [TestMethod("API06: [1;31mPolarConverter[m сворачивает sin и pole tan через public API")]
[35mUnitTests/RicisRegressionCatalogMSTestAdapter.generated.cs[m[36m:[m[32m707[m[36m:[m    [TestMethod("API07: [1;31mNumericConstants[m возвращает typed identities и predicates")]
[35mUnitTests/RicisRegressionCatalogMSTestAdapter.generated.cs[m[36m:[m[32m711[m[36m:[m    [TestMethod("API08: [1;31mNumericConstants[m отклоняет незарегистрированный тип")]
[35mUnitTests/RicisRegressionCatalogMSTestAdapter.generated.cs[m[36m:[m[32m759[m[36m:[m    [TestMethod("API26: [1;31mPolarConverter[m.ToPolarSector renders public singularity view")]

## Existing utility tests

[35mRegressionTests/RicisPublicCompatibilitySuite.cs[m[36m:[m[32m13[m[36m:[m        ("API26: [1;31mPolarConverter[m.ToPolarSector renders public singularity view", PolarSectorRendersSingularity),
[35mRegressionTests/RicisPublicCompatibilitySuite.cs[m[36m:[m[32m27[m[36m:[m        var rendered = [1;31mPolarConverter[m.ToPolarSector(infinity, totalSectors: 8);
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m12[m[36m:[m        ("API01: [1;31mExactEvaluator[m вычисляет рациональное expression", [1;31mExactEvaluator[mComputesRationalExpression),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m13[m[36m:[m        ("API02: [1;31mExactEvaluator[m отклоняет неподдерживаемый узел и неизвестный параметр", [1;31mExactEvaluator[mRejectsUnsupportedShape),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m14[m[36m:[m        ("API03: [1;31mCircleSectors[m нормализует радианную четверть и форматирует сектора", [1;31mCircleSectors[mNormalizeAndFormat),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m15[m[36m:[m        ("API04: [1;31mCircleSectors[m отклоняет NaN, infinity и неверное число секторов", [1;31mCircleSectors[mRejectsInvalidInput),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m16[m[36m:[m        ("API05: [1;31mPolarConverter[m возвращает exact sin/cos и оставляет неизвестный сектор", [1;31mPolarConverter[mExactAndUnknownSector),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m17[m[36m:[m        ("API06: [1;31mPolarConverter[m сворачивает sin и pole tan через public API", [1;31mPolarConverter[mCollapsesTrigAndPole),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m18[m[36m:[m        ("API07: [1;31mNumericConstants[m возвращает typed identities и predicates", [1;31mNumericConstants[mExposeTypedIdentities),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m19[m[36m:[m        ("API08: [1;31mNumericConstants[m отклоняет незарегистрированный тип", [1;31mNumericConstants[mRejectsUnregisteredType),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m33[m[36m:[m    private static void [1;31mExactEvaluator[mComputesRationalExpression()
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m45[m[36m:[m    private static void [1;31mExactEvaluator[mRejectsUnsupportedShape()
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m55[m[36m:[m    private static void [1;31mCircleSectors[mNormalizeAndFormat()
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m57[m[36m:[m        var quarter = [1;31mCircleSectors[m.FromRadians(Math.PI / 2);
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m62[m[36m:[m        var negativeQuarter = [1;31mCircleSectors[m.FromRadians(-Math.PI / 2);
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m66[m[36m:[m    private static void [1;31mCircleSectors[mRejectsInvalidInput()
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m68[m[36m:[m        AssertThrows<ArgumentException>(() => [1;31mCircleSectors[m.FromRadians(double.NaN));
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m69[m[36m:[m        AssertThrows<ArgumentException>(() => [1;31mCircleSectors[m.FromRadians(double.PositiveInfinity));
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m70[m[36m:[m        AssertThrows<ArgumentOutOfRangeException>(() => [1;31mCircleSectors[m.FromRadians(0).InSectors(0));
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m73[m[36m:[m    private static void [1;31mPolarConverter[mExactAndUnknownSector()
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m75[m[36m:[m        var exact = [1;31mPolarConverter[m.ExactSinCos(new Rational(1, 4));
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m78[m[36m:[m        var unknown = [1;31mPolarConverter[m.ExactSinCos(new Rational(1, 7));
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m82[m[36m:[m    private static void [1;31mPolarConverter[mCollapsesTrigAndPole()
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m84[m[36m:[m        var sin = [1;31mPolarConverter[m.TryCollapseTrig(nameof(Math.Sin), Math.PI / 2);
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m87[m[36m:[m        var tan = [1;31mPolarConverter[m.TryCollapseTrig(nameof(Math.Tan), Math.PI / 2);
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m92[m[36m:[m        Assert(ReferenceEquals(call, [1;31mPolarConverter[m.CollapseConstantTrig(call)), "Непостоянный аргумент не должен сворачиваться.");
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m95[m[36m:[m    private static void [1;31mNumericConstants[mExposeTypedIdentities()
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m97[m[36m:[m        var zero = [1;31mNumericConstants[m.ZeroOf(typeof(int));
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m98[m[36m:[m        var one = [1;31mNumericConstants[m.OneOf(typeof(int));
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m101[m[36m:[m        var found = [1;31mNumericConstants[m.TryOneOf(typeof(int), out var registered);
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m104[m[36m:[m        Assert([1;31mNumericConstants[m.IsIntrinsicNumeric(typeof(BigInteger)) && ![1;31mNumericConstants[m.IsIntrinsicNumeric(typeof(string)),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m106[m[36m:[m        Assert([1;31mNumericConstants[m.IsZero(0) && [1;31mNumericConstants[m.IsOne(1) && ![1;31mNumericConstants[m.IsOne(0),
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m110[m[36m:[m    private static void [1;31mNumericConstants[mRejectsUnregisteredType()
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m112[m[36m:[m        Assert(![1;31mNumericConstants[m.TryOneOf(typeof(string), out _), "TryOneOf(string) должен вернуть false.");
[35mRegressionTests/RicisPublicUtilitySuite.cs[m[36m:[m[32m113[m[36m:[m        AssertThrows<NotSupportedException>(() => [1;31mNumericConstants[m.ZeroOf(typeof(string)));
[35mUnitTests/RicisRegressionCatalogMSTestAdapter.generated.cs[m[36m:[m[32m683[m[36m:[m    [TestMethod("API01: [1;31mExactEvaluator[m вычисляет рациональное expression")]
[35mUnitTests/RicisRegressionCatalogMSTestAdapter.generated.cs[m[36m:[m[32m687[m[36m:[m    [TestMethod("API02: [1;31mExactEvaluator[m отклоняет неподдерживаемый узел и неизвестный параметр")]
[35mUnitTests/RicisRegressionCatalogMSTestAdapter.generated.cs[m[36m:[m[32m691[m[36m:[m    [TestMethod("API03: [1;31mCircleSectors[m нормализует радианную четверть и форматирует сектора")]
[35mUnitTests/RicisRegressionCatalogMSTestAdapter.generated.cs[m[36m:[m[32m695[m[36m:[m    [TestMethod("API04: [1;31mCircleSectors[m отклоняет NaN, infinity и неверное число секторов")]
[35mUnitTests/RicisRegressionCatalogMSTestAdapter.generated.cs[m[36m:[m[32m699[m[36m:[m    [TestMethod("API05: [1;31mPolarConverter[m возвращает exact sin/cos и оставляет неизвестный сектор")]
[35mUnitTests/RicisRegressionCatalogMSTestAdapter.generated.cs[m[36m:[m[32m703[m[36m:[m    [TestMethod("API06: [1;31mPolarConverter[m сворачивает sin и pole tan через public API")]
[35mUnitTests/RicisRegressionCatalogMSTestAdapter.generated.cs[m[36m:[m[32m707[m[36m:[m    [TestMethod("API07: [1;31mNumericConstants[m возвращает typed identities и predicates")]
[35mUnitTests/RicisRegressionCatalogMSTestAdapter.generated.cs[m[36m:[m[32m711[m[36m:[m    [TestMethod("API08: [1;31mNumericConstants[m отклоняет незарегистрированный тип")]
[35mUnitTests/RicisRegressionCatalogMSTestAdapter.generated.cs[m[36m:[m[32m759[m[36m:[m    [TestMethod("API26: [1;31mPolarConverter[m.ToPolarSector renders public singularity view")]

## Completion evidence

**EndUtc:** 2026-08-20T15:47:58Z
**WallElapsedHours:** 0.034
**ActiveHours:** 0.034 (no external waiting)
**WaitingHours:** 0.000
**IterationCount:** 0
**PrimaryScope:** Audit confirmed API01–API08/API26 already cover the selected public utility families; no duplicate tests were added.
**FollowUpScope:** Synchronized stale public-utility rows and historical CLI/API/Finance/Lean gate counts in `PUBLIC_API_CLI_AUDIT.md`.
**FinalStatus:** Done
**VarianceHours:** -1.966
**VariancePercent:** -98.3%
**NoSorryCheck:** PASS
**DirectTestCheck:** PASS — 386/386 Core MSTest and 386/386 Core regression
**QualityGate:** PASS — diff check, adapter check, Release build 0 warnings/0 errors
