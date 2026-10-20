using CommunityToolkit.Diagnostics;
using Net.Utilities.WPF.Converters.SingleValue;
using System.Globalization;
using System.Windows;
using Ellipse = Net.Utilities.Models.Geometries.Ellipse;
using Rect = Net.Utilities.Models.Geometries.Rect;

namespace Net.Utilities.WPF.Converters.Calibration.SingleValue;

public sealed class RectToEllipseStringConverter : AbstractSingletonConverterBase<RectToEllipseStringConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => DependencyProperty.UnsetValue,
            Rect rect when parameter is string format => ((Ellipse)rect).ToString(format, culture),
            _ => ThrowHelper.ThrowNotSupportedException<object>()
        };
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object>();
}