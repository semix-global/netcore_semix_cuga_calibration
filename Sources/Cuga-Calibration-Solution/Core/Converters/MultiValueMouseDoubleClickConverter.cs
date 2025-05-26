using Net.Utilities.WPF.Converters.MultiValueConverters;
using System.Globalization;
using System.Windows;

namespace CugaCalibration.Core.Converters;

public sealed class MultiValueMouseDoubleClickConverter : AbstractSingletonMultiConverterBase<MultiValueMouseDoubleClickConverter>
{
    public override object Convert(object?[]? values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values?.Length != 2) throw new NotSupportedException();
        if (values[0] is not int CalibrationCount) throw new NotSupportedException();
        if (values[1] is null) return DependencyProperty.UnsetValue;
        if (values[1] is not bool isAuto) throw new NotSupportedException();
        return !isAuto && CalibrationCount == 0;
    }

    public override object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}