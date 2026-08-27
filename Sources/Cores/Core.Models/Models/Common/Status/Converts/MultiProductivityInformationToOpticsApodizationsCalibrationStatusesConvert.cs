using CommunityToolkit.Diagnostics;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.WPF.Converters.MultiValue;
using System.ComponentModel;
using System.Globalization;
using System.Windows;

namespace Core.Models.Models.Common.Status.Converts;

public sealed class MultiProductivityInformationToOpticsApodizationsCalibrationStatusesConvert
    : AbstractSingletonMultiConverterBase<MultiProductivityInformationToOpticsApodizationsCalibrationStatusesConvert>
{
    public override object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
        => values switch
        {
            [IReadOnlyList<ProductivityInformationAndApodizationStatus> calibrationStatuses, ProductivityInformation productivityInformation] => calibrationStatuses.SingleOrDefault(t => t.SelectedItem == productivityInformation)?.Items ?? [],
            [IReadOnlyList<ProductivityInformationAndApodizationStatus>, { } o1] => o1 == DependencyProperty.UnsetValue ? (BindingList<ProductivityInformationAndApodizationStatus>)[] : ThrowHelper.ThrowNotSupportedException<object>(),
            _ => ThrowHelper.ThrowNotSupportedException<object>()
        };

    public override object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object[]>(nameof(value));
}