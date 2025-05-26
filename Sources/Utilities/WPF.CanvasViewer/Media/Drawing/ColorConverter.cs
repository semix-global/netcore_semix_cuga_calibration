using CanvasViewer.Media.Drawing.Enum;
using System.ComponentModel;
using System.Globalization;

namespace CanvasViewer.Media.Drawing;

public sealed class ColorConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type? sourceType)
    {
        return sourceType == typeof(string);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string);
    }

    public override bool IsValid(ITypeDescriptorContext? context, object? value)
    {
        if (value is string str == false) return false;
#if NET
        if (str.StartsWith('#')) str = str[1..];
#else
        if (str.StartsWith("#")) str = str[1..];
#endif

        return uint.TryParse(str, out _) || System.Enum.TryParse(str, out KnownColorEnum _);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object? value)
    {
        if (value is string str == false) return base.ConvertFrom(context!, culture!, value!);

#if NET
        if (str.StartsWith('#')) str = str[1..];
#else
        if (str.StartsWith("#")) str = str[1..];
#endif

        if (uint.TryParse(str, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out _))
        {
            byte a = 0xff;
            byte r;
            byte g = 0;
            byte b = 0;
#if NET
            if (str.Length > 8) str = str[..8];
            switch (str.Length)
            {
                case > 6:
                    a = byte.Parse(str[..^6], NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    r = byte.Parse(str.AsSpan(str.Length - 6, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    g = byte.Parse(str.AsSpan(str.Length - 4, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    b = byte.Parse(str.AsSpan(str.Length - 2, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    break;

                case > 4:
                    r = byte.Parse(str[..^4], NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    g = byte.Parse(str.AsSpan(str.Length - 4, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    b = byte.Parse(str.AsSpan(str.Length - 2, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    break;

                case > 2:
                    r = byte.Parse(str[..^2], NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    g = byte.Parse(str.AsSpan(str.Length - 2, 1), NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    b = byte.Parse(str.AsSpan(str.Length - 1, 1), NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    break;

                default:
                    r = byte.Parse(str, NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    break;
            }
#else
            if (str.Length > 8) str = str[..8];
            switch (str.Length)
            {
                case > 6:
                    a = byte.Parse(str[..^6], NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    r = byte.Parse(str.Substring(str.Length - 6, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    g = byte.Parse(str.Substring(str.Length - 4, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    b = byte.Parse(str.Substring(str.Length - 2, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    break;

                case > 4:
                    r = byte.Parse(str[..^4], NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    g = byte.Parse(str.Substring(str.Length - 4, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    b = byte.Parse(str.Substring(str.Length - 2, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    break;

                case > 2:
                    r = byte.Parse(str[..^2], NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    g = byte.Parse(str.Substring(str.Length - 2, 1), NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    b = byte.Parse(str.Substring(str.Length - 1, 1), NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    break;

                default:
                    r = byte.Parse(str, NumberStyles.HexNumber, CultureInfo.CurrentCulture);
                    break;
            }
#endif

            return Color.FromArgb(a, r, g, b);
        }

        if (System.Enum.TryParse(str, true, out KnownColorEnum knownColor)) return Color.FromKnownColor(knownColor);

        return base.ConvertFrom(context!, culture!, value);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type? destinationType)
    {
        if (destinationType == typeof(string) && value is Color color)
        {
            return color.IsKnownColor() ? color.ToKnownColor().ToString() : color.ToHex();
        }

        return base.ConvertTo(context, culture, value, destinationType!);
    }
}