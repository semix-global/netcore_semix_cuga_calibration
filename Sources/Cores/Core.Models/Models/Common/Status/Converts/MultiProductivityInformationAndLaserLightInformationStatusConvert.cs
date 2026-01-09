using CommunityToolkit.Diagnostics;
using Net.Utilities.WPF.Converters.MultiValueConverters;
using System.Globalization;
using System.Windows;
using Core.Models.Models.Common.Pattern;

namespace Core.Models.Models.Common.Status.Converts;

public sealed class MultiProductivityInformationAndLaserLightInformationStatusConvert : AbstractSingletonMultiConverterBase<MultiProductivityInformationAndLaserLightInformationStatusConvert>
{
    public override object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
        => values switch
        {
            [IReadOnlyList<ProductivityInformationAndLaserLightInformationStatus> statuses, ProductivityInformation productivityInformation] => statuses.SingleOrDefault(t => t.SelectedItem == productivityInformation)?.Items ?? [],
            [IReadOnlyList<OpticsIlluminationModeAndProductivityInformationStatus>, { } o] => o == DependencyProperty.UnsetValue ? (IReadOnlyList<LaserLightInformationStatus>)[] : ThrowHelper.ThrowNotSupportedException<object>(),
            _ => ThrowHelper.ThrowNotSupportedException<object>()
        };

    public override object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object[]>(nameof(value));
}