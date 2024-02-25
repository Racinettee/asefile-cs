using System.Drawing;

namespace Asefile.Common;

public static class AseBlender
{
    public static Color Normal(Color source, Color target) => source;
    // --------------------------------------
    public static Color Darken(Color source, Color target)
    {
        var minR = Math.Min(source.R, target.R);
        var minG = Math.Min(source.G, target.G);
        var minB = Math.Min(source.B, target.B);

        return Color.FromArgb(source.A, minR, minG, minB);
    }
    private static byte MulUn8(int a, int b)
    {
        var t = (a * b) + 0x80;
        return (byte)(((t >> 8) + t) >> 8);
    }
    public static Color Multiply(Color source, Color target)
    {
        var r = MulUn8(source.R, target.R);
        var g = MulUn8(source.G, target.G);
        var b = MulUn8(source.B, target.B);

        return Color.FromArgb(source.A, r, g, b);
    }
    public static Color Burn(Color source, Color target)
    {
        var r = source.R == 0 ? 0 : Math.Max(0, 255 - (255 - target.R) * 255 / source.R);
        var g = source.G == 0 ? 0 : Math.Max(0, 255 - (255 - target.G) * 255 / source.G);
        var b = source.B == 0 ? 0 : Math.Max(0, 255 - (255 - target.B) * 255 / source.B);

        return Color.FromArgb(target.A, r, g, b);
    }
    // --------------------------------------------
    public static Color Lighten(Color source, Color target)
    {
        var r = Math.Max(source.R, target.R);
        var g = Math.Max(source.G, target.G);
        var b = Math.Max(source.B, target.B);

        return Color.FromArgb(source.A, r, g, b);
    }

    public static Color Screen(Color source, Color target)
    {
        var r = 255 - (((255 - source.R) * (255 - target.R)) / 255);
        var g = 255 - (((255 - source.G) * (255 - target.G)) / 255);
        var b = 255 - (((255 - source.B) * (255 - target.B)) / 255);

        return Color.FromArgb(source.A, r, g, b); 
    }
    public static Color Additive(Color source, Color target)
    {
        var r = Math.Min(source.R + target.R, 255);
        var g = Math.Min(source.G + target.G, 255);
        var b = Math.Min(source.B + target.B, 255);

        return Color.FromArgb(source.A, r, g, b);
    }
    public static Func<Color, Color, Color> GetBlender(BlendMode blendMode)
    {
        var blendFunc = blendMode switch
        {
            BlendMode.Darken => Darken,
            BlendMode.Multiply => Multiply,
            BlendMode.ColorBurn => Burn,
            BlendMode.Lighten => Lighten,
            BlendMode.Screen => Screen,
            BlendMode.Addition => new Func<Color, Color, Color>(Additive),

            _ => Normal,
        };
        return (source, target) =>
        {
            if (target.A == 0 && source.A == 0)
                return Color.Transparent;
            
            if (target.A == 0)
                return source;
            
            if (source.A == 0)
                return target;
            
            return blendFunc(source, target);
        };
    }
}