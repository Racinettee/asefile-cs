using System.Drawing;
using Asefile.Common.ColorExtension;

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
    private static byte DivUn8(int a, int b) => (byte)(((ushort)a * 0xFF + b / 2) / b);
    private static double Saturation(double r, double g, double b) => Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));
    private static double Lumnance(double r, double g, double b) => 0.3 * r + 0.59 * g + 0.11 * b;
    
    private static (double, double, double) Saturated(double r, double g, double b, double s)
    {
        ref double Min(ref double x, ref double y) => ref (x < y ? ref x : ref y);
        ref double Max(ref double x, ref double y) => ref (x > y ? ref x : ref y);
        ref double Mid(ref double x, ref double y, ref double z) =>
            ref x > y ? ref y > z ? ref y : ref x > z ? ref z : ref x : ref y > z ? ref z > x ? ref z : ref x : ref y;
        ref var min = ref Min(ref r, ref Min(ref g, ref b));
        ref var mid = ref Mid(ref r, ref g, ref b);
        ref var max = ref Max(ref r, ref Max(ref g, ref b));

        if (max > min)
        {
            mid = (mid - min) * s / (max - min);
            max = s;
        }
        else mid = max = 0;
        min = 0;
        return (r, g, b);
    }

    private static (double, double, double) Luminated(double r, double g, double b, double l)
    {
        var d = l - Lumnance(r, g, b);
        r += d;
        g += d;
        b += d;
        return ClipColor(r, g, b);
    }
    
    private static (double, double, double) ClipColor(double r, double g, double b)
    {
        var l = Lumnance(r, g, b);
        var n = Math.Min(r, Math.Min(g, b));
        var x = Math.Max(r, Math.Max(g, b));

        if (n < 0)
        {
            r = l + (r - l) * l / (l - n);
            g = l + (g - l) * l / (l - n);
            b = l + (b - l) * l / (l - n);
        }

        if (x > 1)
        {
            r = l + (r - l) * (1 - l) / (x - l);
            g = l + (g - l) * (1 - l) / (x - l);
            b = l + (b - l) * (1 - l) / (x - l);
        }

        return (r, g, b);
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
    public static Color Lighten(Color source, Color target) => Color.FromArgb(source.A, source.Lighter(target));

    public static Color Screen(Color source, Color target)
    {
        var r = 255 - (255 - source.R) * (255 - target.R) / 255;
        var g = 255 - (255 - source.G) * (255 - target.G) / 255;
        var b = 255 - (255 - source.B) * (255 - target.B) / 255;

        return Color.FromArgb(source.A, r, g, b); 
    }
    public static Color Dodge(Color source, Color target)
    {
        int dodge(int b, int s) => s == 255 ? 255 : Math.Min(255, b * 255 / (255 - s));
        return Color.FromArgb(target.A, dodge(target.R, source.R), 
            dodge(target.G, source.G), dodge(target.B, source.B));
    }

    public static Color Additive(Color source, Color target) => Color.FromArgb(source.A, target.Add(source));
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
                return MulUn8(b, s << 1);
            
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
        Color.FromArgb(source.A, Math.Abs(source.R - target.R), Math.Abs(source.G - target.G), Math.Abs(source.B - target.B));

    public static Color Exclusion(Color source, Color target)
    {
        int exclusion(int b, int s) => b + s - 2 * MulUn8(b, s);
        var r = exclusion(target.R, source.R);
        var g = exclusion(target.G, source.G);
        var b = exclusion(target.B, source.B);

        return Color.FromArgb(source.A, r, g, b);
    }

    public static Color Subtract(Color source, Color target) => Color.FromArgb(source.A, target.Subtract(source));

    public static Color Divide(Color source, Color target)
    {
        int divide(int b, int s) => b == 0 ? 0 : b >= s ? 255 : DivUn8(b, s);
        var r = divide(target.R, source.R);
        var g = divide(target.G, source.G);
        var b = divide(target.B, source.B);
        return Color.FromArgb(source.A, r, g, b);
    }
    // -----------------------------------
    public static Color Hue(Color source, Color target)
    {
        var (tr, tg, tb, _) = target.DivideComponents(255.0);
        var saturation = Saturation(tr, tg, tb);
        var lumnance   = Lumnance(tr, tg, tb);
        var (sr, sg, sb, _) = source.DivideComponents(255.0);
        (sr, sg, sb) = Saturated(sr, sg, sb, saturation);
        (sr, sg, sb) = Luminated(sr, sg, sb, lumnance);
        return Color.FromArgb(source.A, (int)(sr * 255.0), (int)(sg * 255.0), (int)(sb * 255.0));
    }
    // .....................................
    public static Func<Color, Color, int, Color> GetBlender(BlendMode blendMode)
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
            BlendMode.Subtract => Subtract,
            BlendMode.Divide => Divide,
            // -------------------------
            BlendMode.Hue => Hue,
            // *************************
            _ => Normal,
        };
        return (source, target, opacity) =>
        {
            if (opacity == 0)
                return target;
            
            if (target.A == 0 && source.A == 0)
                return Color.Transparent;
            
            if (target.A == 0)
                return source;
            
            if (source.A == 0)
                return target;

            var blendResult = blendFunc(source, target);

            int Sa = MulUn8(blendResult.A, opacity);
            int Ra = Sa + target.A - MulUn8(Sa, target.A);

            int Rr = target.R + (blendResult.R - target.R) * Sa / Ra;
            int Rg = target.G + (blendResult.G - target.G) * Sa / Ra;
            int Rb = target.B + (blendResult.B - target.B) * Sa / Ra;

            return Color.FromArgb(Ra, Rr, Rg, Rb);
        };
    }
}