using System.Globalization;
using System.Windows;

namespace Net.Utilities.WPF.Converters;

public sealed class OrdinalIndexConverter : AbstractSingletonConverterBase<OrdinalIndexConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => DependencyProperty.UnsetValue,
            int v => v + 1,
            _ => throw new NotSupportedException()
        };
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}