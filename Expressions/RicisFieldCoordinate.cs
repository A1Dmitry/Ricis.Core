namespace Ricis.Core.Expressions;

/// <summary>
/// Identifies a coordinate of a deferred scalar field
/// <c>F(x,y,z,t)</c> used by formal RICIS vector calculus.
/// </summary>
public enum RicisFieldCoordinate
{
    /// <summary>The first spatial coordinate x.</summary>
    X = 0,

    /// <summary>The second spatial coordinate y.</summary>
    Y = 1,

    /// <summary>The third spatial coordinate z.</summary>
    Z = 2,

    /// <summary>The time coordinate t.</summary>
    T = 3,
}
