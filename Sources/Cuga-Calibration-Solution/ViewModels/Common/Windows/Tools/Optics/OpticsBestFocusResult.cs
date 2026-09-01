using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.DarkField;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.ScottPlot.Extensions;
using Net.Utilities.ScottPlot.WPF.Extensions;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Optics;

public sealed partial class OpticsBestFocusResult : ObservableObject
{
    [ObservableProperty]
    public partial DarkFieldImageDTO DarkFieldImage { get; set; } = new();

    [ObservableProperty]
    public partial BestFocus BestFocus { get; set; } = new();

    public object ToHtmlAnonymous() => new
    {
        DarkFieldRawScanImageDTO = new HtmlQuote(DarkFieldImage.ToHtmlAnonymous()),
        XStrehlRatioScatterPlotControl = new HtmlContainer([.. BestFocus.XStrehlRatioPlotDataSource.GetAllHtmlPlot2DLinesCharts()]),
        YStrehlRatioScatterPlotControl = new HtmlContainer([.. BestFocus.YStrehlRatioPlotDataSource.GetAllHtmlPlot2DLinesCharts()])
    };
}