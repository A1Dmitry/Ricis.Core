namespace Ricis.Kinematics;

/// <summary>
/// Denavit-Hartenberg (DH) parameters for a single robotic link.
/// </summary>
public readonly record struct DHParameter(double A, double Alpha, double D, double Theta)
{
    /// <summary>
    /// Creates a standard DH link parameter set.
    /// </summary>
    public static DHParameter Create(double a, double alpha, double d, double theta) =>
        new(a, alpha, d, theta);
}
