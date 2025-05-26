using System.Globalization;
using System.Windows;

namespace Net.Utilities.WPF.Converters;

public sealed class BoolToBoolConverter : AbstractSingletonConverterBase<BoolToBoolConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return DependencyProperty.UnsetValue;
        if (value is not bool bl) throw new NotSupportedException();
        return !bl;
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}