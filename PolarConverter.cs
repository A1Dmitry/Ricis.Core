// RicisCore/PolarConverter.cs

using System.Linq.Expressions;
using Ricis.Core.Expressions;

namespace Ricis.Core;

public static class PolarConverter
{


    /// <summary>
    /// Полярное представление для монолита — каждая сингулярность отдельно
    /// </summary>
    public static string ToPolarSector(InfinityExpression monolith, int totalSectors = 8, int maxDenominator = 100)
    {
        
        throw new NotImplementedException();
    }

    /// <summary>
    /// Строгое вычисление значения Numerator в точке сингулярности.
    /// Возвращает:
    ///   • double значение, если успешно вычислено и конечно
    ///   • точно 0.0 только при строгом равенстве
    ///   • null при любом исключении, NaN или ±∞
    /// </summary>
    private static double? EvaluateNumeratorExactly(InfinityExpression inf)
    {
        try
        {
            var visitor = new SubstitutionVisitor(inf.SingularityValue, inf.Variable.Name);
            var substituted = visitor.Visit(inf.Numerator);

            var lambda = Expression.Lambda<Func<double>>(Expression.Convert(substituted, typeof(double)));
            var value = lambda.Compile()();

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return null;
            }

            return value;
        }
        catch
        {
            return null;
        }
    }

}