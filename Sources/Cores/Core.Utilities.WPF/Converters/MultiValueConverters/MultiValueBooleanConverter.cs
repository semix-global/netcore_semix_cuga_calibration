using System.Globalization;

namespace Net.Utilities.WPF.Converters.MultiValueConverters;

public sealed class MultiValueBooleanConverter : AbstractSingletonMultiConverterBase<MultiValueBooleanConverter>
{
    public override object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is null || values?.All(t => t is bool) == false) return false;

        return values!.All(t => (bool)t!);
    }

    public override object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}