using System.Drawing;

namespace Asefile.Common.ColorExtension;

public static class ColorExt
{
    public static (byte, byte, byte) RGB(this Color c) => (c.R, c.G, c.B);
    public static (byte, byte, byte, byte) RGBA(this Color c) => (c.R, c.G, c.B, c.A);
    
    /// <summary>
    /// Component wise subtraction clamping each component to a minimum value of 0
    /// </summary>
    /// <param name="source"></param>
    /// <param name="other"></param>
    /// <returns>source - other component wise</returns>
    public static Color Subtract(this Color source, Color other) =>
        Color.FromArgb(Math.Max(0, source.A - other.A), Math.Max(0, source.R - other.R),
            Math.Max(0, source.G - other.G), Math.Max(0, source.B - other.B));

    /// <summary>
    /// Component wise addition clamping each component to a maximum of 255
    /// </summary>
    /// <param name="source"></param>
    /// <param name="other"></param>
    /// <returns>source + other component wise</returns>
    public static Color Add(this Color source, Color other) =>
        Color.FromArgb(Math.Min(source.A + other.A, 255), Math.Min(source.R + other.R, 255),
            Math.Min(source.G + other.G, 255), Math.Min(source.B + other.B, 255));
    
    /// <summary>
    /// Divides the separate components of a color, then returns the results as double values
    /// </summary>
    /// <param name="c"></param>
    /// <param name="d"></param>
    /// <returns>R, G, B, A as doubles that've been divided by d</returns>
    public static (double, double, double, double) DivideComponents(this Color c, double d) =>
        (c.R / d, c.G / d, c.B / d, c.A / d);
    
    /// <summary>
    /// Performs Math.Max on each colors components
    /// </summary>
    /// <param name="c"></param>
    /// <param name="other"></param>
    /// <returns>A color whose components are the greater value between the two subjects</returns>
    public static Color Lighter(this Color c, Color other) =>
        Color.FromArgb(Math.Max(c.A, other.A), Math.Max(c.R, other.R), Math.Max(c.G, other.G), Math.Max(c.B, other.B));
}