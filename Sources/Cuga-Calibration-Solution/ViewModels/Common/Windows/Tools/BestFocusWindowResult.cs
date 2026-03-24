using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.DarkField;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.ScottPlot.WPF.Extensions;

namespace CugaCalibration.ViewModels.Common.Windows.Tools;

public sealed partial class BestFocusWindowResult : ObservableObject
{
    [ObservableProperty]
    private DarkFieldRawScanImageDTO _darkFieldRawScanImage = new();

    [ObservableProperty]
    private BestFocus _bestFocus = new();

    public object ToHtmlAnonymous() => new
    {
        DarkFieldRawScanImageDTO = new HtmlQuote(DarkFieldRawScanImage.ToHtmlAnonymous()),
        XStrehlRatioScatterPlotControl = new HtmlContainer([.. BestFocus.XStrehlRatioScatterPlotControl.GetAllHtmlPlot2DLinesCharts()]),
        YStrehlRatioScatterPlotControl = new HtmlContainer([.. BestFocus.YStrehlRatioScatterPlotControl.GetAllHtmlPlot2DLinesCharts()])
    };
}