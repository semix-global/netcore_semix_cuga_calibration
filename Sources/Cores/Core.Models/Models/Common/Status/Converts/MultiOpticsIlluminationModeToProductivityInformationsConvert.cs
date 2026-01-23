using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Net.Utilities.WPF.Converters.MultiValueConverters;
using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace Core.Models.Models.Common.Status.Converts;

public sealed class MultiOpticsIlluminationModeAndProductivityInformationStatusesToProductivityInformationsCalibrationStatusesConvert : AbstractSingletonMultiConverterBase<MultiOpticsIlluminationModeAndProductivityInformationStatusesToProductivityInformationsCalibrationStatusesConvert>
{
    public override object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
        => values switch
        {
            [IReadOnlyList<OpticsIlluminationModeAndProductivityInformationStatus> calibrationStatuses, OpticsIlluminationModeEnum opticsIlluminationModeEnum] => opticsIlluminationModeEnum switch
            {
                OpticsIlluminationModeEnum.NI => calibrationStatuses.SingleOrDefault(t => t.SelectedItem == OpticsIlluminationModeEnum.NI)?.ProductivityInformationStatusList ?? [],
                OpticsIlluminationModeEnum.OI => calibrationStatuses.SingleOrDefault(t => t.SelectedItem == OpticsIlluminationModeEnum.OI)?.ProductivityInformationStatusList ?? [],
                _ => ThrowHelper.ThrowNotSupportedException<object>(nameof(opticsIlluminationModeEnum))
            },
            [IReadOnlyList<OpticsIlluminationModeAndProductivityInformationStatus>, { } o] => o == DependencyProperty.UnsetValue ? (BindingList<ProductivityInformationStatus>)[] : ThrowHelper.ThrowNotSupportedException<object>(),
            _ => ThrowHelper.ThrowNotSupportedException<object>()
        };

    public override object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object[]>(nameof(value));
}