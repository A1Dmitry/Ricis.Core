    
    using System.Linq.Expressions;
    using Ricis.Core.Expressions;
    using System;
    
    namespace Ricis.Core.Extensions
    {
        public static partial class ExpressionExtensions
        {
            /// <summary>
            /// Добавляет сингулярность в список singularities без численной подстановки.
            /// Если числитель является явной константой 0, создаётся индексированная ноль-сиуемость (InfinityZero).
            /// В фазах упрощения численные оценки запрещены — всё делается символически (O(1)-аналог).
            /// </summary>
            /// <param name="numerator">Выражение числителя</param>
            /// <param name="param">Параметр, по которому локализуется корень</param>
            /// <param name="value">Значение параметра в корне</param>
            /// <param name="singularities">Список, в который добавляется сингулярность</param>
            public static void AddSingularityIfValid(this
                Expression numerator,
                ParameterExpression param,
                double value,
                List<InfinityExpression> singularities)
            {
                // Символическая проверка: если числитель — явная нулевая константа, используем специальный индекс 0.
                var infinity = InfinityExpression.CreateLazy(
                    numerator.IsZero() ? RicisType.InfinityZero : numerator,
                    param,
                    value);

                singularities.Add(infinity);
            }
        }
    }
