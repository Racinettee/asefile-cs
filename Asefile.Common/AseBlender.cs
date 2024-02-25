using System.Drawing;

namespace Asefile.Common;

public static class AseBlender
{
    public static Color Additive(Color source, Color target)
    {
        int r = Math.Min(source.R + target.R, 255);
        int g = Math.Min(source.G + target.G, 255);
        int b = Math.Min(source.B + target.B, 255);

        return Color.FromArgb(source.A, r, g, b);
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
    
    public static Color Normal(Color source, Color target) => source;

    public static Func<Color, Color, Color> GetBlender(BlendMode blendMode)
    {
        var blendFunc = blendMode switch
        {
            BlendMode.Addition => new Func<Color, Color, Color>(Additive),
            BlendMode.Multiply => Multiply,
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