namespace Ricis.ConsoleApp;

internal sealed record ConsoleExample(string Id, string Title, string Input);

/// <summary>
/// Input-only catalogue derived from the user's stress expressions. Expected
/// answers from that source are intentionally not encoded here; RICIS itself
/// derives each symbolic result at runtime.
/// </summary>
internal static class ExampleCatalog
{
    public static IReadOnlyList<ConsoleExample> All { get; } =
    [
        new("L0", "Базовая сингулярность", "x => 10 / (x - 2)"),
        new("L1", "Устранимая квадратичная форма", "x => (x^2 - 25) / (x - 5)"),
        new("L2", "Коэффициенты", "x => 1 / (2*x - 6)"),
        new("L3", "Квадратичный знаменатель", "x => 1 / (x^2 - 4)"),
        new("L5", "Простая тригонометрия", "x => sin(x) / cos(x)"),
        new("L6", "Sinc", "x => sin(x) / x"),
        new("L7", "Составная тригонометрия", "x => sin(2*x) / cos(2*x)"),
        new("L8", "Устранимая четвёртая степень", "x => (x*x*x*x - 1) / (x - 1)"),
        new("L9", "Логарифмический знаменатель", "x => 1 / log(x)"),
        new("L10", "Экспоненциальная форма", "x => (exp(x) - 1) / x"),
        new("L11", "Тригонометрическая форма", "x => (1 - cos(x)) / (x*x)"),
        new("L12", "Тангенс над параметром", "x => tan(x) / x"),
        new("L13", "Стеночная модель", "x => 1 / (x * (x + 1))"),
        new("L14", "Модель blow-up", "x => 1 / (1 - x*x)"),
        new("L15", "Существенная сингулярность", "x => exp(1 / x)"),
        new("L16", "Простой полюс", "x => 1 / x"),
        new("L17", "Полюс второго порядка", "x => 1 / (x*x)"),
        new("L18", "Логарифмическая сингулярность", "x => log(x)"),
        new("L19", "Повтор sinc", "x => sin(x) / x"),
        new("L20", "Повтор существенной сингулярности", "x => exp(1 / x)"),
        new("L21", "Модель Big Bang", "x => 1 / x"),
        new("L22", "Модель горизонта", "x => 1 / (1 - x)"),
        new("L23", "Модель Burgers", "x => 1 / (1 - x)"),
        new("L24", "Вложенная сингулярность", "x => x / (x*x)"),
        new("L25", "Смешанная sinh/cos", "x => 1 / (cos(x) * sinh(x) - 1)"),
        new("L26", "Полюса Pow", "x => 1 / (pow(x, 4) - 1)"),
        new("L27", "Полюс третьего порядка", "x => 1 / (x*x*x)"),
        new("L28", "Устранимая кубическая форма", "x => (pow(x, 3) - 8) / (x - 2)"),
        new("L29", "Двойной полюс", "x => 1 / (x*x*(x - 1))"),
        new("L30", "Корневая ветвь", "x => sqrt(x)"),
        new("L31", "Прокси Gamma", "x => sin(pi*x) / x"),
        new("L32", "Прокси Bessel", "x => sqrt(2 / (pi*x)) * cos(x - pi/4)"),
        new("L33", "Прокси Airy", "x => exp(pow(x, 1.5))"),
        new("L34", "Прокси Riemann zeta", "x => 1 / (x - 1) + log(abs(x))"),
        new("L35", "Интеграл Fermi-Dirac", "x => log(1 + exp(-1 / x)) / x"),
        new("L36", "Дробная степень blow-up", "x => 1 / pow(1 - x, 2.0/3)"),
        new("L37", "Вихревой слой", "x => 1 / sqrt(abs(x) + 2.220446049250313e-16)"),
        new("L38", "Schwarzschild g00", "x => 1 / (1 - 2 / x)"),
        new("L39", "QCD instanton", "x => exp(-8 * pi * pi / x)"),
        new("L40", "UV полюс", "x => 1 / (12 * x)"),
        new("L41", "Yang-Mills monopole", "x => 1 / sqrt(x*x + 2.220446049250313e-16)"),
        new("L42", "Высокий порядок", "x => (x - sin(x)) / pow(x, 3)"),
        new("L43", "Гиперболическая форма", "x => (sinh(x) - x) / pow(x, 3)"),
        new("L44", "Неприводимый квадратный знаменатель", "x => 1 / (x*x + 1)"),
        new("L45", "Тангенциальный полюс", "x => 1 / (1 - tan(x))"),
        new("L46", "Составной экспоненциальный полюс", "x => 1 / (exp(x*x) - 1)"),
        new("L47", "Логарифмический ноль", "x => 1 / log(x)"),
        new("L48", "Неопределённая log-форма", "x => log(x) / (1 / x)"),
        new("L49", "Полином пятой степени", "x => 1 / (pow(x, 5) - 32)"),
        new("L50", "Близкие корни", "x => 1 / ((x - 1) * (x - 1.0000001))"),
        new("L51", "Символьная производная", "x => derivative(x ^ 3)"),
        new("L52", "Геометрический интеграл", "x => integral(x + 1, 5)"),
        new("L53", "Сумма выражений", "x => sum(x, 1)"),
        new("L54", "Сложный процент", "x => compoundInterest(100, 10, 2)"),
        new("L55", "Минимум выражений", "x => min(x, 0)"),
        new("L56", "Положительная часть", "x => positivePart(x)"),
        new("L57", "Отрицательная часть", "x => negativePart(x)"),
        new("L58", "Расстояние выражений", "x => distance(x, 5)"),
    ];
}
