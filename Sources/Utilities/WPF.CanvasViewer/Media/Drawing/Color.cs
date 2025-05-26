using CanvasViewer.Media.Drawing.Enum;
using System.ComponentModel;
using System.Globalization;

namespace CanvasViewer.Media.Drawing;

[TypeConverter(typeof(ColorConverter))]
public readonly struct Color
{
    #region 属性

    /// <summary>
    /// 颜色是否使用Layer的颜色
    /// </summary>
    public bool IsByLayer { get; }

    /// <summary>
    /// Argb
    /// </summary>
    public uint Argb { get; }

    /// <summary>
    /// 透明度
    /// </summary>
    public byte A { get; }

    /// <summary>
    /// Red
    /// </summary>
    public byte R { get; }

    /// <summary>
    /// Green
    /// </summary>
    public byte G { get; }

    /// <summary>
    /// Blue
    /// </summary>
    public byte B { get; }

    #endregion 属性

    #region 构造

    private Color(bool byLayer)
    {
        IsByLayer = byLayer;
        Argb = 0;
        A = R = G = B = 0;
    }

    private Color(KnownColorEnum colorName)
    {
        if (colorName == KnownColorEnum.ByLayer)
        {
            IsByLayer = true;
            Argb = 0;
            A = R = G = B = 0;
        }
        else
        {
            IsByLayer = false;
            Argb = KnownColorLookup[colorName];
            A = (byte)((Argb >> 24) & 255);
            R = (byte)((Argb >> 16) & 255);
            G = (byte)((Argb >> 8) & 255);
            B = (byte)(Argb & 255);
        }
    }

    public Color(uint color, bool isByLayer)
    {
        if (isByLayer)
        {
            IsByLayer = true;
            Argb = 0;
            A = R = G = B = 0;
        }
        else
        {
            IsByLayer = false;
            Argb = color;
            A = (byte)((color >> 24) & 255);
            R = (byte)((color >> 16) & 255);
            G = (byte)((color >> 8) & 255);
            B = (byte)(color & 255);
        }
    }

    public Color(byte r, byte g, byte b) : this(255, r, g, b)
    {
    }

    public Color(byte a, byte r, byte g, byte b) : this(false)
    {
        Argb = ((uint)a << 24) + ((uint)r << 16) + ((uint)g << 8) + b;
        A = a;
        R = r;
        G = g;
        B = b;
    }

    public Color(byte alpha, Color color) : this(alpha, color.R, color.G, color.B)
    {
    }

    #endregion 构造

    #region 方法

    public override bool Equals(object? obj)
    {
        if (obj is Color == false) return false;

        var color = (Color)obj;

        return this == color;
    }

    public override int GetHashCode()
    {
        return (int)Argb;
    }

    public string ToHex()
    {
        return "#" + Argb.ToString("X8");
    }

    public bool IsKnownColor()
    {
        if (IsByLayer)
            return true;

        foreach (var argb in KnownColorLookup.Values)
        {
            if (Argb == argb) return true;
        }

        return false;
    }

    public KnownColorEnum ToKnownColor()
    {
        if (IsByLayer) return KnownColorEnum.ByLayer;

        foreach (var pair in KnownColorLookup)
        {
            if (Argb == pair.Value) return pair.Key;
        }

        return KnownColorEnum.Transparent;
    }

    #endregion 方法

    #region 操作符重构

    public static bool operator ==(Color a, Color b)
    {
        if (a.IsByLayer && b.IsByLayer)
            return true;
        else
            return a.Argb == b.Argb;
    }

    public static bool operator !=(Color a, Color b)
    {
        if (a.IsByLayer && b.IsByLayer)
            return false;
        else
            return a.Argb != b.Argb;
    }

    public static explicit operator System.Windows.Media.Color(Color a)
    {
        return System.Windows.Media.Color.FromArgb(a.A, a.R, a.G, a.B);
    }

    #endregion 操作符重构

    #region 静态构造

    public static Color FromHex(string hex)
    {
        var argb = uint.Parse(hex.Replace("#", ""), NumberStyles.HexNumber);
        return new Color(argb, false);
    }

    public static Color FromArgb(uint argb)
    {
        return new Color(argb, false);
    }

    public static Color FromArgb(byte a, byte r, byte g, byte b)
    {
        return new Color(a, r, g, b);
    }

    public static Color FromArgb(byte r, byte g, byte b)
    {
        return new Color(r, g, b);
    }

    public static Color FromArgb(byte alpha, Color color)
    {
        return new Color(alpha, color.R, color.G, color.B);
    }

    public static Color FromKnownColor(KnownColorEnum colorName)
    {
        return new Color(colorName);
    }

    public static Color Random()
    {
        var rnd = new Random();
        return new Color((byte)rnd.Next(255), (byte)rnd.Next(255), (byte)rnd.Next(255));
    }

    #endregion 静态构造

    #region 预定义颜色

    public static Color ByLayer => new(true);

    public static Color Transparent => FromHex("0");

    public static Color AliceBlue => FromHex("FFF0F8FF");

    public static Color AntiqueWhite => FromHex("FFFAEBD7");

    public static Color Aqua => FromHex("FF00FFFF");

    public static Color Aquamarine => FromHex("FF7FFFD4");

    public static Color Azure => FromHex("FFF0FFFF");

    public static Color Beige => FromHex("FFF5F5DC");

    public static Color Bisque => FromHex("FFFFE4C4");

    public static Color Black => FromHex("FF000000");

    public static Color BlanchedAlmond => FromHex("FFFFEBCD");

    public static Color Blue => FromHex("FF0000FF");

    public static Color BlueViolet => FromHex("FF8A2BE2");

    public static Color Brown => FromHex("FFA52A2A");

    public static Color BurlyWood => FromHex("FFDEB887");

    public static Color CadetBlue => FromHex("FF5F9EA0");

    public static Color Chartreuse => FromHex("FF7FFF00");

    public static Color Chocolate => FromHex("FFD2691E");

    public static Color Coral => FromHex("FFFF7F50");

    public static Color CornflowerBlue => FromHex("FF6495ED");

    public static Color Cornsilk => FromHex("FFFFF8DC");

    public static Color Crimson => FromHex("FFDC143C");

    public static Color Cyan => FromHex("FF00FFFF");

    public static Color DarkBlue => FromHex("FF00008B");

    public static Color DarkCyan => FromHex("FF008B8B");

    public static Color DarkGoldenrod => FromHex("FFB8860B");

    public static Color DarkGray => FromHex("FFA9A9A9");

    public static Color DarkGreen => FromHex("FF006400");

    public static Color DarkKhaki => FromHex("FFBDB76B");

    public static Color DarkMagenta => FromHex("FF8B008B");

    public static Color DarkOliveGreen => FromHex("FF556B2F");

    public static Color DarkOrange => FromHex("FFFF8C00");

    public static Color DarkOrchid => FromHex("FF9932CC");

    public static Color DarkRed => FromHex("FF8B0000");

    public static Color DarkSalmon => FromHex("FFE9967A");

    public static Color DarkSeaGreen => FromHex("FF8FBC8F");

    public static Color DarkSlateBlue => FromHex("FF483D8B");

    public static Color DarkSlateGray => FromHex("FF2F4F4F");

    public static Color DarkTurquoise => FromHex("FF00CED1");

    public static Color DarkViolet => FromHex("FF9400D3");

    public static Color DeepPink => FromHex("FFFF1493");

    public static Color DeepSkyBlue => FromHex("FF00BFFF");

    public static Color DimGray => FromHex("FF696969");

    public static Color DodgerBlue => FromHex("FF1E90FF");

    public static Color Firebrick => FromHex("FFB22222");

    public static Color FloralWhite => FromHex("FFFFFAF0");

    public static Color ForestGreen => FromHex("FF228B22");

    public static Color Fuchsia => FromHex("FFFF00FF");

    public static Color Gainsboro => FromHex("FFDCDCDC");

    public static Color GhostWhite => FromHex("FFF8F8FF");

    public static Color Gold => FromHex("FFFFD700");

    public static Color Goldenrod => FromHex("FFDAA520");

    public static Color Gray => FromHex("FF808080");

    public static Color Green => FromHex("FF008000");

    public static Color GreenYellow => FromHex("FFADFF2F");

    public static Color Honeydew => FromHex("FFF0FFF0");

    public static Color HotPink => FromHex("FFFF69B4");

    public static Color IndianRed => FromHex("FFCD5C5C");

    public static Color Indigo => FromHex("FF4B0082");

    public static Color Ivory => FromHex("FFFFFFF0");

    public static Color Khaki => FromHex("FFF0E68C");

    public static Color Lavender => FromHex("FFE6E6FA");

    public static Color LavenderBlush => FromHex("FFFFF0F5");

    public static Color LawnGreen => FromHex("FF7CFC00");

    public static Color LemonChiffon => FromHex("FFFFFACD");

    public static Color LightBlue => FromHex("FFADD8E6");

    public static Color LightCoral => FromHex("FFF08080");

    public static Color LightCyan => FromHex("FFE0FFFF");

    public static Color LightGoldenrodYellow => FromHex("FFFAFAD2");

    public static Color LightGray => FromHex("FFD3D3D3");

    public static Color LightGreen => FromHex("FF90EE90");

    public static Color LightPink => FromHex("FFFFB6C1");

    public static Color LightSalmon => FromHex("FFFFA07A");

    public static Color LightSeaGreen => FromHex("FF20B2AA");

    public static Color LightSkyBlue => FromHex("FF87CEFA");

    public static Color LightSlateGray => FromHex("FF778899");

    public static Color LightSteelBlue => FromHex("FFB0C4DE");

    public static Color LightYellow => FromHex("FFFFFFE0");

    public static Color Lime => FromHex("FF00FF00");

    public static Color LimeGreen => FromHex("FF32CD32");

    public static Color Linen => FromHex("FFFAF0E6");

    public static Color Magenta => FromHex("FFFF00FF");

    public static Color Maroon => FromHex("FF800000");

    public static Color MediumAquamarine => FromHex("FF66CDAA");

    public static Color MediumBlue => FromHex("FF0000CD");

    public static Color MediumOrchid => FromHex("FFBA55D3");

    public static Color MediumPurple => FromHex("FF9370DB");

    public static Color MediumSeaGreen => FromHex("FF3CB371");

    public static Color MediumSlateBlue => FromHex("FF7B68EE");

    public static Color MediumSpringGreen => FromHex("FF00FA9A");

    public static Color MediumTurquoise => FromHex("FF48D1CC");

    public static Color MediumVioletRed => FromHex("FFC71585");

    public static Color MidnightBlue => FromHex("FF191970");

    public static Color MintCream => FromHex("FFF5FFFA");

    public static Color MistyRose => FromHex("FFFFE4E1");

    public static Color Moccasin => FromHex("FFFFE4B5");

    public static Color NavajoWhite => FromHex("FFFFDEAD");

    public static Color Navy => FromHex("FF000080");

    public static Color OldLace => FromHex("FFFDF5E6");

    public static Color Olive => FromHex("FF808000");

    public static Color OliveDrab => FromHex("FF6B8E23");

    public static Color Orange => FromHex("FFFFA500");

    public static Color OrangeRed => FromHex("FFFF4500");

    public static Color Orchid => FromHex("FFDA70D6");

    public static Color PaleGoldenrod => FromHex("FFEEE8AA");

    public static Color PaleGreen => FromHex("FF98FB98");

    public static Color PaleTurquoise => FromHex("FFAFEEEE");

    public static Color PaleVioletRed => FromHex("FFDB7093");

    public static Color PapayaWhip => FromHex("FFFFEFD5");

    public static Color PeachPuff => FromHex("FFFFDAB9");

    public static Color Peru => FromHex("FFCD853F");

    public static Color Pink => FromHex("FFFFC0CB");

    public static Color Plum => FromHex("FFDDA0DD");

    public static Color PowderBlue => FromHex("FFB0E0E6");

    public static Color Purple => FromHex("FF800080");

    public static Color Red => FromHex("FFFF0000");

    public static Color RosyBrown => FromHex("FFBC8F8F");

    public static Color RoyalBlue => FromHex("FF4169E1");

    public static Color SaddleBrown => FromHex("FF8B4513");

    public static Color Salmon => FromHex("FFFA8072");

    public static Color SandyBrown => FromHex("FFF4A460");

    public static Color SeaGreen => FromHex("FF2E8B57");

    public static Color SeaShell => FromHex("FFFFF5EE");

    public static Color Sienna => FromHex("FFA0522D");

    public static Color Silver => FromHex("FFC0C0C0");

    public static Color SkyBlue => FromHex("FF87CEEB");

    public static Color SlateBlue => FromHex("FF6A5ACD");

    public static Color SlateGray => FromHex("FF708090");

    public static Color Snow => FromHex("FFFFFAFA");

    public static Color SpringGreen => FromHex("FF00FF7F");

    public static Color SteelBlue => FromHex("FF4682B4");

    public static Color Tan => FromHex("FFD2B48C");

    public static Color Teal => FromHex("FF008080");

    public static Color Thistle => FromHex("FFD8BFD8");

    public static Color Tomato => FromHex("FFFF6347");

    public static Color Turquoise => FromHex("FF40E0D0");

    public static Color Violet => FromHex("FFEE82EE");

    public static Color Wheat => FromHex("FFF5DEB3");

    public static Color White => FromHex("FFFFFFFF");

    public static Color WhiteSmoke => FromHex("FFF5F5F5");

    public static Color Yellow => FromHex("FFFFFF00");

    public static Color YellowGreen => FromHex("FF9ACD32");

    #endregion 预定义颜色

    #region 已知颜色 Values

    public static readonly Dictionary<KnownColorEnum, uint> KnownColorLookup = new()
    {
        { KnownColorEnum.ByLayer, 0x0 },
        { KnownColorEnum.Transparent, 0x0 },
        { KnownColorEnum.AliceBlue, 0xFFF0F8FF },
        { KnownColorEnum.AntiqueWhite, 0xFFFAEBD7 },
        { KnownColorEnum.Aqua, 0xFF00FFFF },
        { KnownColorEnum.Aquamarine, 0xFF7FFFD4 },
        { KnownColorEnum.Azure, 0xFFF0FFFF },
        { KnownColorEnum.Beige, 0xFFF5F5DC },
        { KnownColorEnum.Bisque, 0xFFFFE4C4 },
        { KnownColorEnum.Black, 0xFF000000 },
        { KnownColorEnum.BlanchedAlmond, 0xFFFFEBCD },
        { KnownColorEnum.Blue, 0xFF0000FF },
        { KnownColorEnum.BlueViolet, 0xFF8A2BE2 },
        { KnownColorEnum.Brown, 0xFFA52A2A },
        { KnownColorEnum.BurlyWood, 0xFFDEB887 },
        { KnownColorEnum.CadetBlue, 0xFF5F9EA0 },
        { KnownColorEnum.Chartreuse, 0xFF7FFF00 },
        { KnownColorEnum.Chocolate, 0xFFD2691E },
        { KnownColorEnum.Coral, 0xFFFF7F50 },
        { KnownColorEnum.CornflowerBlue, 0xFF6495ED },
        { KnownColorEnum.Cornsilk, 0xFFFFF8DC },
        { KnownColorEnum.Crimson, 0xFFDC143C },
        { KnownColorEnum.Cyan, 0xFF00FFFF },
        { KnownColorEnum.DarkBlue, 0xFF00008B },
        { KnownColorEnum.DarkCyan, 0xFF008B8B },
        { KnownColorEnum.DarkGoldenrod, 0xFFB8860B },
        { KnownColorEnum.DarkGray, 0xFFA9A9A9 },
        { KnownColorEnum.DarkGreen, 0xFF006400 },
        { KnownColorEnum.DarkKhaki, 0xFFBDB76B },
        { KnownColorEnum.DarkMagenta, 0xFF8B008B },
        { KnownColorEnum.DarkOliveGreen, 0xFF556B2F },
        { KnownColorEnum.DarkOrange, 0xFFFF8C00 },
        { KnownColorEnum.DarkOrchid, 0xFF9932CC },
        { KnownColorEnum.DarkRed, 0xFF8B0000 },
        { KnownColorEnum.DarkSalmon, 0xFFE9967A },
        { KnownColorEnum.DarkSeaGreen, 0xFF8FBC8F },
        { KnownColorEnum.DarkSlateBlue, 0xFF483D8B },
        { KnownColorEnum.DarkSlateGray, 0xFF2F4F4F },
        { KnownColorEnum.DarkTurquoise, 0xFF00CED1 },
        { KnownColorEnum.DarkViolet, 0xFF9400D3 },
        { KnownColorEnum.DeepPink, 0xFFFF1493 },
        { KnownColorEnum.DeepSkyBlue, 0xFF00BFFF },
        { KnownColorEnum.DimGray, 0xFF696969 },
        { KnownColorEnum.DodgerBlue, 0xFF1E90FF },
        { KnownColorEnum.Firebrick, 0xFFB22222 },
        { KnownColorEnum.FloralWhite, 0xFFFFFAF0 },
        { KnownColorEnum.ForestGreen, 0xFF228B22 },
        { KnownColorEnum.Fuchsia, 0xFFFF00FF },
        { KnownColorEnum.Gainsboro, 0xFFDCDCDC },
        { KnownColorEnum.GhostWhite, 0xFFF8F8FF },
        { KnownColorEnum.Gold, 0xFFFFD700 },
        { KnownColorEnum.Goldenrod, 0xFFDAA520 },
        { KnownColorEnum.Gray, 0xFF808080 },
        { KnownColorEnum.Green, 0xFF008000 },
        { KnownColorEnum.GreenYellow, 0xFFADFF2F },
        { KnownColorEnum.Honeydew, 0xFFF0FFF0 },
        { KnownColorEnum.HotPink, 0xFFFF69B4 },
        { KnownColorEnum.IndianRed, 0xFFCD5C5C },
        { KnownColorEnum.Indigo, 0xFF4B0082 },
        { KnownColorEnum.Ivory, 0xFFFFFFF0 },
        { KnownColorEnum.Khaki, 0xFFF0E68C },
        { KnownColorEnum.Lavender, 0xFFE6E6FA },
        { KnownColorEnum.LavenderBlush, 0xFFFFF0F5 },
        { KnownColorEnum.LawnGreen, 0xFF7CFC00 },
        { KnownColorEnum.LemonChiffon, 0xFFFFFACD },
        { KnownColorEnum.LightBlue, 0xFFADD8E6 },
        { KnownColorEnum.LightCoral, 0xFFF08080 },
        { KnownColorEnum.LightCyan, 0xFFE0FFFF },
        { KnownColorEnum.LightGoldenrodYellow, 0xFFFAFAD2 },
        { KnownColorEnum.LightGray, 0xFFD3D3D3 },
        { KnownColorEnum.LightGreen, 0xFF90EE90 },
        { KnownColorEnum.LightPink, 0xFFFFB6C1 },
        { KnownColorEnum.LightSalmon, 0xFFFFA07A },
        { KnownColorEnum.LightSeaGreen, 0xFF20B2AA },
        { KnownColorEnum.LightSkyBlue, 0xFF87CEFA },
        { KnownColorEnum.LightSlateGray, 0xFF778899 },
        { KnownColorEnum.LightSteelBlue, 0xFFB0C4DE },
        { KnownColorEnum.LightYellow, 0xFFFFFFE0 },
        { KnownColorEnum.Lime, 0xFF00FF00 },
        { KnownColorEnum.LimeGreen, 0xFF32CD32 },
        { KnownColorEnum.Linen, 0xFFFAF0E6 },
        { KnownColorEnum.Magenta, 0xFFFF00FF },
        { KnownColorEnum.Maroon, 0xFF800000 },
        { KnownColorEnum.MediumAquamarine, 0xFF66CDAA },
        { KnownColorEnum.MediumBlue, 0xFF0000CD },
        { KnownColorEnum.MediumOrchid, 0xFFBA55D3 },
        { KnownColorEnum.MediumPurple, 0xFF9370DB },
        { KnownColorEnum.MediumSeaGreen, 0xFF3CB371 },
        { KnownColorEnum.MediumSlateBlue, 0xFF7B68EE },
        { KnownColorEnum.MediumSpringGreen, 0xFF00FA9A },
        { KnownColorEnum.MediumTurquoise, 0xFF48D1CC },
        { KnownColorEnum.MediumVioletRed, 0xFFC71585 },
        { KnownColorEnum.MidnightBlue, 0xFF191970 },
        { KnownColorEnum.MintCream, 0xFFF5FFFA },
        { KnownColorEnum.MistyRose, 0xFFFFE4E1 },
        { KnownColorEnum.Moccasin, 0xFFFFE4B5 },
        { KnownColorEnum.NavajoWhite, 0xFFFFDEAD },
        { KnownColorEnum.Navy, 0xFF000080 },
        { KnownColorEnum.OldLace, 0xFFFDF5E6 },
        { KnownColorEnum.Olive, 0xFF808000 },
        { KnownColorEnum.OliveDrab, 0xFF6B8E23 },
        { KnownColorEnum.Orange, 0xFFFFA500 },
        { KnownColorEnum.OrangeRed, 0xFFFF4500 },
        { KnownColorEnum.Orchid, 0xFFDA70D6 },
        { KnownColorEnum.PaleGoldenrod, 0xFFEEE8AA },
        { KnownColorEnum.PaleGreen, 0xFF98FB98 },
        { KnownColorEnum.PaleTurquoise, 0xFFAFEEEE },
        { KnownColorEnum.PaleVioletRed, 0xFFDB7093 },
        { KnownColorEnum.PapayaWhip, 0xFFFFEFD5 },
        { KnownColorEnum.PeachPuff, 0xFFFFDAB9 },
        { KnownColorEnum.Peru, 0xFFCD853F },
        { KnownColorEnum.Pink, 0xFFFFC0CB },
        { KnownColorEnum.Plum, 0xFFDDA0DD },
        { KnownColorEnum.PowderBlue, 0xFFB0E0E6 },
        { KnownColorEnum.Purple, 0xFF800080 },
        { KnownColorEnum.Red, 0xFFFF0000 },
        { KnownColorEnum.RosyBrown, 0xFFBC8F8F },
        { KnownColorEnum.RoyalBlue, 0xFF4169E1 },
        { KnownColorEnum.SaddleBrown, 0xFF8B4513 },
        { KnownColorEnum.Salmon, 0xFFFA8072 },
        { KnownColorEnum.SandyBrown, 0xFFF4A460 },
        { KnownColorEnum.SeaGreen, 0xFF2E8B57 },
        { KnownColorEnum.SeaShell, 0xFFFFF5EE },
        { KnownColorEnum.Sienna, 0xFFA0522D },
        { KnownColorEnum.Silver, 0xFFC0C0C0 },
        { KnownColorEnum.SkyBlue, 0xFF87CEEB },
        { KnownColorEnum.SlateBlue, 0xFF6A5ACD },
        { KnownColorEnum.SlateGray, 0xFF708090 },
        { KnownColorEnum.Snow, 0xFFFFFAFA },
        { KnownColorEnum.SpringGreen, 0xFF00FF7F },
        { KnownColorEnum.SteelBlue, 0xFF4682B4 },
        { KnownColorEnum.Tan, 0xFFD2B48C },
        { KnownColorEnum.Teal, 0xFF008080 },
        { KnownColorEnum.Thistle, 0xFFD8BFD8 },
        { KnownColorEnum.Tomato, 0xFFFF6347 },
        { KnownColorEnum.Turquoise, 0xFF40E0D0 },
        { KnownColorEnum.Violet, 0xFFEE82EE },
        { KnownColorEnum.Wheat, 0xFFF5DEB3 },
        { KnownColorEnum.White, 0xFFFFFFFF },
        { KnownColorEnum.WhiteSmoke, 0xFFF5F5F5 },
        { KnownColorEnum.Yellow, 0xFFFFFF00 },
        { KnownColorEnum.YellowGreen, 0xFF9ACD32 }
    };

    #endregion 已知颜色 Values
}