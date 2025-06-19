using CommunityToolkit.Diagnostics;
using Net.Utilities.Extensions;
using Net.Utilities.WPF.Behaviors;
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
            IEnumerable<double> list => list.ToPoints(),
            IEnumerable<IEnumerable<double>> listOfLists => (List<WpfPlotModel>)[.. listOfLists.Select((t, i) => new WpfPlotModel($"{i + 1}", t.ToPoints()))],
            _ => ThrowHelper.ThrowNotSupportedException<object>(nameof(value))
        };
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}