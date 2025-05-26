using System.ComponentModel;

namespace CanvasViewer.Geometry;

public sealed class Segment2DConverter : ExpandableObjectConverter
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

        return parts.Length == 4 && parts.All(part => double.TryParse(part, out _));
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value)
    {
        if (value is string str == false) return base.ConvertFrom(context!, culture!, value!);

        var parts = str.Replace(" ", "").Split(';', ',');
        if (parts.Length != 4) return base.ConvertFrom(context!, culture!, value);

        var x1 = double.Parse(parts[0]);
        var y1 = double.Parse(parts[1]);
        var x2 = double.Parse(parts[2]);
        var y2 = double.Parse(parts[3]);
        return new Segment2D(x1, y1, x2, y2);
    }

    public override object? ConvertTo(ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, Type? destinationType)
    {
        if (destinationType == typeof(string) && value is Segment2D segment2D)
        {
            return segment2D.X1.ToString("F6") + "; " + segment2D.Y1.ToString("F6") + "; " + segment2D.X2.ToString("F6") + "; " +
                   segment2D.Y2.ToString("F6");
        }

        return base.ConvertTo(context, culture, value, destinationType!);
    }
}