using CommunityToolkit.Diagnostics;
using Net.Utilities.WPF.Converters.SingleValue;
using System.Globalization;
using System.Windows;

namespace Net.Utilities.WPF.Converters.Calibration.SingleValue;

public sealed class ListToStringFormatConverter : AbstractSingletonConverterBase<ListToStringFormatConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => string.Empty,
            IEnumerable<double> temps when parameter is string format => string.Join(", ", temps.Select(t => t.ToString(format, null))),
            IEnumerable<double> temps => string.Join(", ", temps),
            _ => DependencyProperty.UnsetValue
        };
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object>();
}