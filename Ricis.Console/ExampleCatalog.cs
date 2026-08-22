namespace Ricis.ConsoleApp;
using Ricis.Core.Resources;

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
        new("L0", RicisLegacyTextResources.Get("runtime.legacy.b7e60827beb4"), "x => 10 / (x - 2)"),
        new("L1", RicisLegacyTextResources.Get("runtime.legacy.63a39a7fe9fc"), "x => (x^2 - 25) / (x - 5)"),
        new("L2", RicisLegacyTextResources.Get("runtime.legacy.486752ca4265"), "x => 1 / (2*x - 6)"),
        new("L3", RicisLegacyTextResources.Get("runtime.legacy.9e09bb7e1bf5"), "x => 1 / (x^2 - 4)"),
        new("L5", RicisLegacyTextResources.Get("runtime.legacy.9489b881671c"), "x => sin(x) / cos(x)"),
        new("L6", "Sinc", "x => sin(x) / x"),
        new("L7", RicisLegacyTextResources.Get("runtime.legacy.4a0e0c0a62d6"), "x => sin(2*x) / cos(2*x)"),
        new("L8", RicisLegacyTextResources.Get("runtime.legacy.d18de99780bb"), "x => (x*x*x*x - 1) / (x - 1)"),
        new("L9", RicisLegacyTextResources.Get("runtime.legacy.0d00e36dd207"), "x => 1 / log(x)"),
        new("L10", RicisLegacyTextResources.Get("runtime.legacy.f62a21f84e24"), "x => (exp(x) - 1) / x"),
        new("L11", RicisLegacyTextResources.Get("runtime.legacy.096b3282f0f3"), "x => (1 - cos(x)) / (x*x)"),
        new("L12", RicisLegacyTextResources.Get("runtime.legacy.e4e6245255d7"), "x => tan(x) / x"),
        new("L13", RicisLegacyTextResources.Get("runtime.legacy.915a073d3c5b"), "x => 1 / (x * (x + 1))"),
        new("L14", RicisLegacyTextResources.Get("runtime.legacy.283cbb54f0f9"), "x => 1 / (1 - x*x)"),
        new("L15", RicisLegacyTextResources.Get("runtime.legacy.b3c89f773201"), "x => exp(1 / x)"),
        new("L16", RicisLegacyTextResources.Get("runtime.legacy.da31efe9c14d"), "x => 1 / x"),
        new("L17", RicisLegacyTextResources.Get("runtime.legacy.355a464ac40d"), "x => 1 / (x*x)"),
        new("L18", RicisLegacyTextResources.Get("runtime.legacy.42a7471d6877"), "x => log(x)"),
        new("L19", RicisLegacyTextResources.Get("runtime.legacy.3d1b70c492dd"), "x => sin(x) / x"),
        new("L20", RicisLegacyTextResources.Get("runtime.legacy.accc9a201221"), "x => exp(1 / x)"),
        new("L21", RicisLegacyTextResources.Get("runtime.legacy.f642639ba781"), "x => 1 / x"),
        new("L22", RicisLegacyTextResources.Get("runtime.legacy.c0bb34fce98c"), "x => 1 / (1 - x)"),
        new("L23", RicisLegacyTextResources.Get("runtime.legacy.672e6314636d"), "x => 1 / (1 - x)"),
        new("L24", RicisLegacyTextResources.Get("runtime.legacy.605388bae686"), "x => x / (x*x)"),
        new("L25", RicisLegacyTextResources.Get("runtime.legacy.bc8c47e88288"), "x => 1 / (cos(x) * sinh(x) - 1)"),
        new("L26", RicisLegacyTextResources.Get("runtime.legacy.357a8cb07c4b"), "x => 1 / (pow(x, 4) - 1)"),
        new("L27", RicisLegacyTextResources.Get("runtime.legacy.74319be6e198"), "x => 1 / (x*x*x)"),
        new("L28", RicisLegacyTextResources.Get("runtime.legacy.1320e02e6928"), "x => (pow(x, 3) - 8) / (x - 2)"),
        new("L29", RicisLegacyTextResources.Get("runtime.legacy.854733c2aed4"), "x => 1 / (x*x*(x - 1))"),
        new("L30", RicisLegacyTextResources.Get("runtime.legacy.b62d5144f801"), "x => sqrt(x)"),
        new("L31", RicisLegacyTextResources.Get("runtime.legacy.32a6185aae55"), "x => sin(pi*x) / x"),
        new("L32", RicisLegacyTextResources.Get("runtime.legacy.de81672a0e2e"), "x => sqrt(2 / (pi*x)) * cos(x - pi/4)"),
        new("L33", RicisLegacyTextResources.Get("runtime.legacy.ed5933dbd788"), "x => exp(pow(x, 1.5))"),
        new("L34", RicisLegacyTextResources.Get("runtime.legacy.552cc9632faf"), "x => 1 / (x - 1) + log(abs(x))"),
        new("L35", RicisLegacyTextResources.Get("runtime.legacy.67cf6c5834df"), "x => log(1 + exp(-1 / x)) / x"),
        new("L36", RicisLegacyTextResources.Get("runtime.legacy.6bcb84d90a0b"), "x => 1 / pow(1 - x, 2.0/3)"),
        new("L37", RicisLegacyTextResources.Get("runtime.legacy.963d03b12323"), "x => 1 / sqrt(abs(x) + 2.220446049250313e-16)"),
        new("L38", "Schwarzschild g00", "x => 1 / (1 - 2 / x)"),
        new("L39", "QCD instanton", "x => exp(-8 * pi * pi / x)"),
        new("L40", RicisLegacyTextResources.Get("runtime.legacy.8c383d7703cb"), "x => 1 / (12 * x)"),
        new("L41", "Yang-Mills monopole", "x => 1 / sqrt(x*x + 2.220446049250313e-16)"),
        new("L42", RicisLegacyTextResources.Get("runtime.legacy.b20bf2dc99a0"), "x => (x - sin(x)) / pow(x, 3)"),
        new("L43", RicisLegacyTextResources.Get("runtime.legacy.59c304f316f3"), "x => (sinh(x) - x) / pow(x, 3)"),
        new("L44", RicisLegacyTextResources.Get("runtime.legacy.82b165c47050"), "x => 1 / (x*x + 1)"),
        new("L45", RicisLegacyTextResources.Get("runtime.legacy.52905db01881"), "x => 1 / (1 - tan(x))"),
        new("L46", RicisLegacyTextResources.Get("runtime.legacy.bf2aa661cea0"), "x => 1 / (exp(x*x) - 1)"),
        new("L47", RicisLegacyTextResources.Get("runtime.legacy.047cecf6160f"), "x => 1 / log(x)"),
        new("L48", RicisLegacyTextResources.Get("runtime.legacy.ffa894015659"), "x => log(x) / (1 / x)"),
        new("L49", RicisLegacyTextResources.Get("runtime.legacy.0603df6c97a9"), "x => 1 / (pow(x, 5) - 32)"),
        new("L50", RicisLegacyTextResources.Get("runtime.legacy.d64c42f90483"), "x => 1 / ((x - 1) * (x - 1.0000001))"),
        new("L51", RicisLegacyTextResources.Get("runtime.legacy.bb2c21408b68"), "x => derivative(x ^ 3)"),
        new("L52", RicisLegacyTextResources.Get("runtime.legacy.cbe0808df1d3"), "x => integral(x + 1, 5)"),
        new("L53", RicisLegacyTextResources.Get("runtime.legacy.c781959cd715"), "x => sum(x, 1)"),
        new("L54", RicisLegacyTextResources.Get("runtime.legacy.e8196e6f8476"), "x => compoundInterest(100, 10, 2)"),
        new("L55", RicisLegacyTextResources.Get("runtime.legacy.0890758f78b7"), "x => min(x, 0)"),
        new("L56", RicisLegacyTextResources.Get("runtime.legacy.8f40ed00de3d"), "x => positivePart(x)"),
        new("L57", RicisLegacyTextResources.Get("runtime.legacy.87b5e3bbd5f4"), "x => negativePart(x)"),
        new("L58", RicisLegacyTextResources.Get("runtime.legacy.be8f4a280bfe"), "x => distance(x, 5)"),
        new("L59", RicisLegacyTextResources.Get("runtime.legacy.160cff5e06e9"), "x => max(x, 5)"),
        new("L60", RicisLegacyTextResources.Get("runtime.legacy.7b4f8d08a490"), "x => clamp(x, -1, 1)"),
        new("L61", RicisLegacyTextResources.Get("runtime.legacy.60a1ae00b98c"), "x => cosh(x)"),
        new("L62", RicisLegacyTextResources.Get("runtime.legacy.aaef16077e3c"), "x => tanh(x)"),
        new("L63", RicisLegacyTextResources.Get("runtime.legacy.7649cda1d3f6"), "x => log10(abs(x) + 1)"),
        new("L64", RicisLegacyTextResources.Get("runtime.legacy.58a48d30f1ec"), "x => sign(x)"),
        new("L65", RicisLegacyTextResources.Get("runtime.legacy.4629cc8a2f24"), "x => mod(x, 2)"),
        new("L66", RicisLegacyTextResources.Get("runtime.legacy.a9052b33df77"), "x => pow(x, 3)"),
    ];
}
