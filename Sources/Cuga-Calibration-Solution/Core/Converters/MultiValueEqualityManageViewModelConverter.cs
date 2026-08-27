using CugaCalibration.ViewModels.Common.Windows.Management;
using Net.Utilities.WPF.Converters.MultiValue;
using System.Globalization;
using System.Windows;

namespace CugaCalibration.Core.Converters;

public sealed class MultiValueEqualityManageViewModelConverter : AbstractSingletonMultiConverterBase<MultiValueEqualityManageViewModelConverter>
{
    public override object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values?.Length != 2) throw new NotSupportedException();
        if (values[0] is not string viewModel) throw new NotSupportedException();
        if (values[1] is null) return DependencyProperty.UnsetValue;

        if (values[1] is not ManagementViewModelBase calibrationViewModel) throw new NotSupportedException();

        return viewModel == calibrationViewModel.GetType().FullName;
    }

    public override object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}