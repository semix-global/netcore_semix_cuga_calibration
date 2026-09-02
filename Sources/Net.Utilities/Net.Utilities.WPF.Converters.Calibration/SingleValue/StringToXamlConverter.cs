using CommunityToolkit.Diagnostics;
using Net.Utilities.WPF.Converters.SingleValue;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace Net.Utilities.WPF.Converters.Calibration.SingleValue;

public sealed class StringToXamlConverter : AbstractSingletonConverterBase<StringToXamlConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string xaml || string.IsNullOrWhiteSpace(xaml)) return DependencyProperty.UnsetValue;

        return XamlReader.Parse(xaml);
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object>();
}