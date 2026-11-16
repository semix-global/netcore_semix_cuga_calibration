using CommunityToolkit.Diagnostics;
using Net.Utilities.WPF.Converters.SingleValue;
using System.Globalization;
using System.Windows;
using Vector = Net.Utilities.Models.Geometries.Vector;

namespace Net.Utilities.WPF.Converters.Calibration.SingleValue;

public sealed class VectorToStringConverter : AbstractSingletonConverterBase<VectorToStringConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => DependencyProperty.UnsetValue,
            Vector vector when parameter is string format => vector.ToString(format, culture),
            _ => ThrowHelper.ThrowNotSupportedException<object>()
        };
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object>();
}