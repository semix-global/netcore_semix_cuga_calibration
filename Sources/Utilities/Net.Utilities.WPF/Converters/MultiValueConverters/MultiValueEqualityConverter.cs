using System.Globalization;

namespace Net.Utilities.WPF.Converters.MultiValueConverters;

public sealed class MultiValueEqualityConverter : AbstractSingletonMultiConverterBase<MultiValueEqualityConverter>
{
    public override object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values?.Length != 2) throw new NotSupportedException();

        return ReferenceEquals(values[0], values[1]) || (values[0]?.Equals(values[1]) ?? false);
    }

    public override object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}