using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using ScottPlot;
using ScottPlot.Plottables;
using Range = ScottPlot.Range;

namespace Core.Utilities.WPF.ScottPlot;

public interface IScatterPlotControl
{
    void ConfigureScatter(IMultiplotLayout? layout = null, int totalPlotCount = 1, Action<IReadOnlyList<Plot>>? configurePlotLayoutActions = null);

    string GetTitle(int plotIndex);

    void SetTitle(int plotIndex, string title, float? fontSize = null);

    HtmlPlot2DLinesChart GetHtmlPlot2DLinesChart(int plotIndex);

    IReadOnlyList<HtmlPlot2DLinesChart> GetHtmlPlot2DLinesCharts(int plotIndex);

    HtmlPlot2DLinesChart GetHtmlPlot2DLinesChart();

    IReadOnlyList<HtmlPlot2DLinesChart> GetHtmlPlot2DLinesCharts();

    Scatter UpdateOrAddScatter(int plotIndex, string legendText, IReadOnlyList<Point> points, Color? color = null);

    Scatter UpdateOrAddScatter(int plotIndex, string legendText, IReadOnlyList<Point> points, int position);

    Scatter UpdateOrAddScatter(int plotIndex, string legendText, IReadOnlyList<Point> points, double position, Range range);

    Scatter UpdateOrAddScatter(string legendText, IReadOnlyList<Point> points, Color? color = null);

    Scatter UpdateOrAddScatter(string legendText, IReadOnlyList<Point> points, int position);

    Scatter UpdateOrAddScatter(string legendText, IReadOnlyList<Point> points, double position, Range range);

    void AutoScaleRefresh();
}