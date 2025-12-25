using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.WPF.Converters.MultiValueConverters;
using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace Core.Models.Models.Common.Status.Converts;

public sealed class MultiOpticsIlluminationModeToProductivityInformationAndApodizationsCalibrationStatusesConvert
    : AbstractSingletonMultiConverterBase<MultiOpticsIlluminationModeToProductivityInformationAndApodizationsCalibrationStatusesConvert>
{
    public override object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
        => values switch
        {
            [IReadOnlyList<OpticsIlluminationModeAndProductivityInformationAndApodizationCalibrationStatus> calibrationStatuses, OpticsIlluminationModeEnum opticsIlluminationModeEnum] => opticsIlluminationModeEnum switch
            {
                OpticsIlluminationModeEnum.NI => calibrationStatuses.SingleOrDefault(t => t.SelectedItem == OpticsIlluminationModeEnum.NI)?.ProductivityInformationAndApodizationCalibrationStatusList ?? [],
                OpticsIlluminationModeEnum.OI => calibrationStatuses.SingleOrDefault(t => t.SelectedItem == OpticsIlluminationModeEnum.OI)?.ProductivityInformationAndApodizationCalibrationStatusList ?? [],
                _ => ThrowHelper.ThrowNotSupportedException<object>(nameof(opticsIlluminationModeEnum))
            },
            [IReadOnlyList<OpticsIlluminationModeAndProductivityInformationAndApodizationCalibrationStatus>, { } o] => o == DependencyProperty.UnsetValue ? (BindingList<ProductivityInformationAndApodizationCalibrationStatus>)[] : ThrowHelper.ThrowNotSupportedException<object>(),
            _ => ThrowHelper.ThrowNotSupportedException<object>()
        };

    public override object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object[]>(nameof(value));
}

public sealed class MultiProductivityInformationToOpticsApodizationsCalibrationStatusesConvert
    : AbstractSingletonMultiConverterBase<MultiProductivityInformationToOpticsApodizationsCalibrationStatusesConvert>
{
    public override object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
        => values switch
        {
            [IReadOnlyList<OpticsIlluminationModeAndProductivityInformationAndApodizationCalibrationStatus> calibrationStatuses, OpticsIlluminationModeEnum opticsIlluminationModeEnum, ProductivityInformation productivityInformation] =>
                calibrationStatuses.SingleOrDefault(t => t.SelectedItem == opticsIlluminationModeEnum)?
                    .ProductivityInformationAndApodizationCalibrationStatusList
                    .SingleOrDefault(t => t.SelectedItem == productivityInformation)?
                    .OpticsApodizationModeCalibrationStatusList ?? [],
            [IReadOnlyList<OpticsIlluminationModeAndProductivityInformationAndApodizationCalibrationStatus>, { } o1, { } o2] => o1 == DependencyProperty.UnsetValue || o2 == DependencyProperty.UnsetValue ? (BindingList<OpticsApodizationModeCalibrationStatus>)[] : ThrowHelper.ThrowNotSupportedException<object>(),
            _ => ThrowHelper.ThrowNotSupportedException<object>()
        };

    public override object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object[]>(nameof(value));
}