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
        var r = 255 - (255 - source.R) * (255 - target.R) / 255;
        var g = 255 - (255 - source.G) * (255 - target.G) / 255;
        var b = 255 - (255 - source.B) * (255 - target.B) / 255;

        return Color.FromArgb(source.A, r, g, b); 
    }
    public static Color Dodge(Color source, Color target)
    {
        var r = source.R == 255 ? 255 : Math.Min(255, target.R * 255 / (255 - source.R));
        var g = source.G == 255 ? 255 : Math.Min(255, target.G * 255 / (255 - source.G));
        var b = source.B == 255 ? 255 : Math.Min(255, target.B * 255 / (255 - source.B));

        return Color.FromArgb(target.A, r, g, b);
    }
    public static Color Additive(Color source, Color target)
    {
        var r = Math.Min(source.R + target.R, 255);
        var g = Math.Min(source.G + target.G, 255);
        var b = Math.Min(source.B + target.B, 255);

        return Color.FromArgb(source.A, r, g, b);
    }
    // -------------------------------------
    public static Color Overlay(Color source, Color target)
    {
        int overlay(int b, int s) => b < 128 ? 2 * b * s / 255 
                                             : 255 - 2 * (255 - b) * (255 - s) / 255;

        var r = overlay(target.R, source.R);
        var g = overlay(target.G, source.G);
        var b = overlay(target.B, source.B);

        return Color.FromArgb(source.A, r, g, b);
    }

    public static Color SoftLight(Color source, Color target)
    {
        int softLight(int baseComp, int overlayComp)
        {
            var b = baseComp / 255.0;
            var s = overlayComp / 255.0;
            var d = b <= 0.25 ? ((16 * b - 12) * b + 4) * b : Math.Sqrt(b);
            var result = s <= 0.5 ? b - (1.0 - 2.0 * s) * b * (1.0 - b) : b + (2.0 * s - 1.0) * (d - b);
            return (int)(result * 255 + 0.5);
        };

        var r = softLight(target.R, source.R);
        var g = softLight(target.G, source.G);
        var b = softLight(target.B, source.B);

        return Color.FromArgb(source.A, r, g, b);
    }

    public static Color HardLight(Color source, Color target)
    {
        int hardLight(int b, int s)
        {
            if (s < 128)
            {
                s <<= 1;
                return MulUn8(b, s);
            }
            s = (s << 1) - 255;
            return b + s - MulUn8(b, s);
        }
        var r = hardLight(target.R, source.R);
        var g = hardLight(target.G, source.G);
        var b = hardLight(target.B, source.B);

        return Color.FromArgb(source.A, r, g, b);
    }
    // --------------------------------
    public static Color Difference(Color source, Color target) =>
        Color.FromArgb(source.A,
            Math.Abs(source.R - target.R),
            Math.Abs(source.G - target.G),
            Math.Abs(source.B - target.B));

    public static Color Exclusion(Color source, Color target)
    {
        int exclusion(int b, int s) => b + s - 2 * MulUn8(b, s);
        var r = exclusion(target.R, source.R);
        var g = exclusion(target.G, source.G);
        var b = exclusion(target.B, source.B);

        return Color.FromArgb(source.A, r, g, b);
    }

    // public static Color Subtract(Color source, Color target)
    // {
    //     
    // }
    // .....................................
    public static Func<Color, Color, Color> GetBlender(BlendMode blendMode)
    {
        var blendFunc = blendMode switch
        {
            // -------------------------
            BlendMode.Darken    => Darken,
            BlendMode.Multiply  => Multiply,
            BlendMode.ColorBurn => Burn,
            // -------------------------
            BlendMode.Lighten => Lighten,
            BlendMode.Screen => Screen,
            BlendMode.ColorDodge => Dodge,
            BlendMode.Addition => new Func<Color, Color, Color>(Additive),
            // -------------------------
            BlendMode.Overlay => Overlay,
            BlendMode.SoftLight => SoftLight,
            BlendMode.HardLight => HardLight,
            // -------------------------
            BlendMode.Difference => Difference,
            BlendMode.Exclusion => Exclusion,
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