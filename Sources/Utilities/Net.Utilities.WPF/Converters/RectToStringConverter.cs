using System.Globalization;
using System.Windows;
using Rect = Net.Utilities.Models.Rect;

namespace Net.Utilities.WPF.Converters;

public sealed class RectToStringConverter : AbstractSingletonConverterBase<RectToStringConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return DependencyProperty.UnsetValue;
        if (value is not Rect rect) throw new NotSupportedException();
        return rect.ToShortString();
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string str) throw new NotSupportedException();
        var parts = str.Replace(" ", "").Split(';', ',');

        if (parts.Length != 4 || parts.All(part => double.TryParse(part, out _)) == false) return Rect.Empty;

        var x = double.Parse(parts[0]);
        var y = double.Parse(parts[1]);
        var width = double.Parse(parts[2]);
        var height = double.Parse(parts[3]);
        return new Rect(x, y, width, height);
    }
}