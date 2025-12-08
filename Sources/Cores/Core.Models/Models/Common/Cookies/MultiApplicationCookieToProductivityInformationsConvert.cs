using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.WPF.Converters.MultiValueConverters;
using System.Globalization;
using System.Windows;

namespace Core.Models.Models.Common.Cookies;

public sealed class MultiApplicationCookieToProductivityInformationsConvert : AbstractSingletonMultiConverterBase<MultiApplicationCookieToProductivityInformationsConvert>
{
    public override object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
        => values switch
        {
            [ApplicationCookie applicationCookie, OpticsIlluminationModeEnum opticsIlluminationModeEnum] => opticsIlluminationModeEnum switch
            {
                OpticsIlluminationModeEnum.NI => applicationCookie.NIProductivityInformations,
                OpticsIlluminationModeEnum.OI => applicationCookie.OIProductivityInformations,
                _ => ThrowHelper.ThrowNotSupportedException<object>(nameof(opticsIlluminationModeEnum))
            },
            [ApplicationCookie, { } o] => o == DependencyProperty.UnsetValue ? (IReadOnlyList<ProductivityInformation>)[] : ThrowHelper.ThrowNotSupportedException<object>(),
            _ => ThrowHelper.ThrowNotSupportedException<object>()
        };

    public override object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object[]>(nameof(value));
}