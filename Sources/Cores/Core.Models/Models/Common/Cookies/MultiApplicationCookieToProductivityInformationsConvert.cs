using System.Globalization;
using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Net.Utilities.WPF.Converters.MultiValueConverters;

namespace Core.Models.Models.Common.Cookies;

public sealed class MultiApplicationCookieToProductivityInformationsConvert : AbstractSingletonMultiConverterBase<MultiApplicationCookieToProductivityInformationsConvert>
{
    public override object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
        => values is [ApplicationCookie applicationCookie, OpticsIlluminationModeEnum opticsIlluminationModeEnum]
            ? opticsIlluminationModeEnum switch
            {
                OpticsIlluminationModeEnum.NI => applicationCookie.NIProductivityInformations,
                OpticsIlluminationModeEnum.OI => applicationCookie.OIProductivityInformations,
                _ => ThrowHelper.ThrowNotSupportedException<object>(nameof(opticsIlluminationModeEnum))
            }
            : ThrowHelper.ThrowNotSupportedException<object>();

    public override object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object[]>(nameof(value));
}