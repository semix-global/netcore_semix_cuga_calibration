using System.ComponentModel;

namespace CanvasViewer.Geometry;

public sealed class Point2DConverter : ExpandableObjectConverter
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

        var parts = str.Replace(" ", "").Split(';', ',');

        return parts.Length == 2 && parts.All(part => double.TryParse(part, out _));
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value)
    {
        if (value is string str == false) return base.ConvertFrom(context!, culture!, value!);

        var parts = str.Replace(" ", "").Split(';', ',');
        if (parts.Length != 2) return base.ConvertFrom(context!, culture!, value);

        var x = double.Parse(parts[0]);
        var y = double.Parse(parts[1]);
        return new Point2D(x, y);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, Type? destinationType)
    {
        if (destinationType == typeof(string) && value is Point2D pt)
        {
            return pt.X.ToString("F6") + "; " + pt.Y.ToString("F6");
        }

        return base.ConvertTo(context, culture, value, destinationType!);
    }
}