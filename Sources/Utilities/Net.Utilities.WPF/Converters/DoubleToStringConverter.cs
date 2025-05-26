using System.Globalization;
using System.Windows;

namespace Net.Utilities.WPF.Converters;

public sealed class DoubleToStringConverter : AbstractSingletonConverterBase<DoubleToStringConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not double doubleValue ? DependencyProperty.UnsetValue : $"{doubleValue}";
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string stringValue) return DependencyProperty.UnsetValue;

        return double.TryParse(stringValue, out var result) == false ? 0 : result;
    }
}