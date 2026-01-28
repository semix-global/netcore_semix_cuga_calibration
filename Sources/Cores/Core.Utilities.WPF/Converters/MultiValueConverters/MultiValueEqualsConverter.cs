using System.Globalization;
using CommunityToolkit.Diagnostics;
using Net.Utilities.WPF.Converters.MultiValueConverters;

namespace Core.Utilities.WPF.Converters.MultiValueConverters;

public sealed class MultiValueEqualsConverter : AbstractSingletonMultiConverterBase<MultiValueEqualsConverter>
{
    public override object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture) => values is not null && values.Length == 2
        ? Equals(values[0], values[1])
        : ThrowHelper.ThrowNotSupportedException<object>(nameof(values));

    public override object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object[]>(nameof(value));
}