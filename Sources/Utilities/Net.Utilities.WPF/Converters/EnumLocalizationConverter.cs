using Net.Utilities.Helper.Enum;
using System.Globalization;

namespace Net.Utilities.WPF.Converters;

public sealed class EnumLocalizationConverter : AbstractSingletonConverterBase<EnumLocalizationConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) throw new NotSupportedException();

        return EnumHelper.ToLocalizationResourceValue(value);
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}