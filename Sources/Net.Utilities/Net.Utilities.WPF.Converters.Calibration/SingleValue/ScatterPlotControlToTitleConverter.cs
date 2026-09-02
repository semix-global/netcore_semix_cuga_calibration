using CommunityToolkit.Diagnostics;
using Net.Utilities.ScottPlot.WPF.Interfaces;
using Net.Utilities.ScottPlot.WPF.WPF;
using Net.Utilities.WPF.Converters.SingleValue;
using ScottPlot;
using System.Globalization;

namespace Net.Utilities.WPF.Converters.Calibration.SingleValue;

public sealed class ScatterPlotControlToTitleConverter : AbstractSingletonConverterBase<ScatterPlotControlToTitleConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IScatterPlotControl scatterPlotControl) return ThrowHelper.ThrowNotSupportedException<object>(nameof(value));

        return parameter switch
        {
            int plotIndex => scatterPlotControl.GetTitle(plotIndex),
            _ => string.Join(Environment.NewLine, Enumerable.Range(0, ((ScatterPlotControl)scatterPlotControl).Multiplot.Count()).Select(t => scatterPlotControl.GetTitle(t)))
        };
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => ThrowHelper.ThrowNotSupportedException<object>(nameof(value));
}