using Net.Utilities.Helper.Enum;
using System.Globalization;
using System.Windows;

namespace Net.Utilities.WPF.Converters;

public sealed class EnumDescriptionConverter : AbstractSingletonConverterBase<EnumDescriptionConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is null ? DependencyProperty.UnsetValue : EnumHelper.ToDescriptionString(value);
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}