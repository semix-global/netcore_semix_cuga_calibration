using System.Globalization;
using System.Windows;
using Point = Net.Utilities.Models.Point;

namespace Net.Utilities.WPF.Converters;

public sealed class PointToStringConverter : AbstractSingletonConverterBase<PointToStringConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return DependencyProperty.UnsetValue;
        if (value is not Point pt) throw new NotSupportedException();
        return pt.ToShortString();
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string str) throw new NotSupportedException();
        var parts = str.Replace(" ", "").Split(';', ',');

        if (parts.Length != 2 || parts.All(part => double.TryParse(part, out _)) == false) return Point.Empty;

        var x = double.Parse(parts[0]);
        var y = double.Parse(parts[1]);
        return new Point(x, y);
    }
}