using CommunityToolkit.Diagnostics;
using Net.Utilities.WPF.Converters;
using System.Globalization;
using System.Windows;

namespace Core.Utilities.WPF.Converters;

public sealed class ListToStringFormatConverter : AbstractSingletonConverterBase<ListToStringFormatConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => string.Empty,
            IEnumerable<IFormattable> temps when parameter is string format => string.Join(", ", temps.Select(t => t.ToString(format, null))),
            IEnumerable<IFormattable> temps => string.Join(", ", temps),
            _ => DependencyProperty.UnsetValue
        };
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object>();
}