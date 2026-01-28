using System.Globalization;
using System.Windows;
using CommunityToolkit.Diagnostics;
using Net.Utilities.WPF.Converters;
using Point = Net.Utilities.Models.Geometries.Point;

namespace Core.Utilities.WPF.Converters;

public sealed class PointYToStringConverter : AbstractSingletonConverterBase<PointYToStringConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => DependencyProperty.UnsetValue,
            Point pt when parameter is string format => pt.Y.ToString(format),
            _ => ThrowHelper.ThrowNotSupportedException<object>()
        };
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object>();
}

public sealed class PointXToStringConverter : AbstractSingletonConverterBase<PointXToStringConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => DependencyProperty.UnsetValue,
            Point pt when parameter is string format => pt.X.ToString(format),
            _ => ThrowHelper.ThrowNotSupportedException<object>()
        };
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object>();
}