using CommunityToolkit.Diagnostics;
using Net.Utilities.WPF.Converters.SingleValue;
using System.Globalization;
using System.Windows;
using Rect = Net.Utilities.Models.Geometries.Rect;

namespace Net.Utilities.WPF.Converters.Calibration.SingleValue;

public sealed class RectHeightConverter : AbstractSingletonConverterBase<RectHeightConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => DependencyProperty.UnsetValue,
            Rect rect => rect.Height,
            _ => ThrowHelper.ThrowNotSupportedException<object>()
        };
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object>();
}
