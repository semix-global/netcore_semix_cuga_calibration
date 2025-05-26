using Net.Utilities.Extensions;
using System.Globalization;
using System.Windows;

namespace Net.Utilities.WPF.Converters;

public sealed class DoublesToPointsConverter : AbstractSingletonConverterBase<DoublesToPointsConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => DependencyProperty.UnsetValue,
            List<double> list => list.ToPoints(),
            double[] array => array.ToPoints(),
            _ => throw new NotSupportedException()
        };
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}