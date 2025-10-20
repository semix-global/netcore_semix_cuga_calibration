using Core.Utilities.WPF.ScottPlot.Interactivity.UserActionResponses;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using ScottPlot;
using ScottPlot.Colormaps;
using ScottPlot.DataSources;
using ScottPlot.Palettes;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using SkiaSharp.Views.WPF;
using System.Windows.Input;
using Range = ScottPlot.Range;

namespace Core.Utilities.WPF.ScottPlot.WPF;

public sealed class ScatterPlotControl : WpfPlot, IScatterPlotControl
{
    private static readonly Turbo Turbo = new();
    private static readonly Category10 Category10 = new();

    private readonly ScatterPlotControl? _originalControl;

    public ScatterPlotControl()
    {
        var wpfPlotMenu = GuardUtils.IsNotNullAndAssignableToType<WpfPlotMenu>(Menu);
        wpfPlotMenu.Clear();
        ObjectHelper.SetPropertyValue(wpfPlotMenu, "ThisControl", null);

        Menu = new ScatterPlotControlMenu(this);
    }

    internal ScatterPlotControl(ScatterPlotControl originalControl) : this() => _originalControl = originalControl;

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        e.Handled = true;
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        var skElement = GuardUtils.IsAssignableToType<SKElement>(PlotFrameworkElement);

        var position = e.GetPosition(this);
        var pixel = new Pixel(position.X, position.Y);

#pragma warning disable IDE0079
#pragma warning disable IDISP001
#pragma warning disable IDISP004

        var plotAtPixel = Multiplot.GetPlotAtPixel(pixel);
        if (plotAtPixel?.PlotControl is null) return;

        var items = plotAtPixel.Legend.GetItems();
        if (items.Length == 0) return;

        var subplotRectangles = plotAtPixel.PlotControl.Multiplot.Layout.GetSubplotRectangles(
            plotAtPixel.PlotControl.Multiplot.Subplots,
            new PixelRect(0, skElement.CanvasSize.Width, skElement.CanvasSize.Height, 0));

        var index = 0;
        for (var i = 0; i < plotAtPixel.PlotControl.Multiplot.Subplots.Count; i++)
        {
            if (ReferenceEquals(plotAtPixel.PlotControl.Multiplot.GetPlot(i), plotAtPixel) == false) continue;

            index = i;
            break;
        }

#pragma warning restore IDISP004
#pragma warning restore IDISP001
#pragma warning restore IDE0079

        using var paint = Paint.NewDisposablePaint();
        var dataRect = plotAtPixel.Layout.LayoutEngine.GetLayout(new PixelRect(
            left: subplotRectangles[index].Left / (float)plotAtPixel.ScaleFactor,
            right: subplotRectangles[index].Right / (float)plotAtPixel.ScaleFactor,
            bottom: subplotRectangles[index].Bottom / (float)plotAtPixel.ScaleFactor,
            top: subplotRectangles[index].Top / (float)plotAtPixel.ScaleFactor), plotAtPixel, paint).DataRect;

        var dataRectAfterMargin = dataRect.Contract(plotAtPixel.Legend.Margin);
        var tightLayout = plotAtPixel.Legend.Layout.GetLayout(plotAtPixel.Legend, items, dataRectAfterMargin.Size, paint);
        var standaloneLegendRect = tightLayout.LegendRect.AlignedInside(dataRectAfterMargin, plotAtPixel.Legend.Alignment);
        var legendOffset = new PixelOffset(standaloneLegendRect.Left, standaloneLegendRect.Top);
        var layout = new LegendLayout { LegendItems = tightLayout.LegendItems, LegendRect = tightLayout.LegendRect.WithOffset(legendOffset), LabelRects = tightLayout.LabelRects.Select(x => x.WithOffset(legendOffset)).ToArray(), SymbolRects = tightLayout.SymbolRects.Select(x => x.WithOffset(legendOffset)).ToArray() };

        var scalePixel = pixel / (float)plotAtPixel.ScaleFactor;
        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            var labelRect = layout.LabelRects[i];
            var symbolRect = layout.SymbolRects[i];

            if (labelRect.Contains(scalePixel) == false && symbolRect.Contains(scalePixel) == false) continue;

            if (item.Plottable is null) continue;

            item.Plottable.IsVisible = !item.Plottable.IsVisible;

            plotAtPixel.PlotControl.Refresh();
            Refresh();

            return;
        }

        e.Handled = true;
    }

    public void ConfigureScatter(IMultiplotLayout? layout = null, int totalPlotCount = 1, Action<IReadOnlyList<Plot>>? configurePlotLayoutActions = null)
    {
        this.ConfigureCommon(layout, totalPlotCount);

        var plots = Multiplot.GetPlots();
        configurePlotLayoutActions?.Invoke(plots);

        foreach (var plot in plots)
        {
            var annotation = plot.Add.Annotation(string.Empty, Alignment.UpperRight);
            annotation.IsVisible = false;

            var crossHair = plot.Add.Crosshair(0, 0);
            crossHair.IsVisible = false;
            crossHair.LineColor = Colors.Red;

            plot.Legend.ShowItemsFromHiddenPlottables = true;
            plot.ShowLegend(Alignment.UpperLeft, Orientation.Vertical);
        }

        UserInputProcessor.UserActionResponses.Add(new ScatterMouseMove()); // 显示位置
    }

    public string GetTitle(int plotIndex)
    {
#pragma warning disable IDE0079
#pragma warning disable IDISP001

        var plot = Multiplot.GetPlot(plotIndex);

#pragma warning restore IDISP001
#pragma warning restore IDE0079

        return plot.GetTitle();
    }

    public void SetTitle(int plotIndex, string title, float? fontSize = null)
    {
#pragma warning disable IDE0079
#pragma warning disable IDISP001

        var plot = Multiplot.GetPlot(plotIndex);

#pragma warning restore IDISP001
#pragma warning restore IDE0079

        plot.Title(title, fontSize);
    }

    public HtmlPlot2DLinesChart GetHtmlPlot2DLinesChart(int plotIndex)
    {
#pragma warning disable IDE0079
#pragma warning disable IDISP001

        var plot = Multiplot.GetPlot(plotIndex);

#pragma warning restore IDISP001
#pragma warning restore IDE0079

        var plots = plot.GetScatterPoints();

        return new HtmlPlot2DLinesChart(plots, plot.GetTitle());
    }

    public IReadOnlyList<HtmlPlot2DLinesChart> GetHtmlPlot2DLinesCharts(int plotIndex)
    {
#pragma warning disable IDE0079
#pragma warning disable IDISP001

        var plot = Multiplot.GetPlot(plotIndex);

#pragma warning restore IDISP001
#pragma warning restore IDE0079

        var plots = plot.GetScatterPoints();

        return [.. plots.Select(t => new HtmlPlot2DLinesChart([(t.Name, t.Points)], t.Name))];
    }

    public HtmlPlot2DLinesChart GetHtmlPlot2DLinesChart() => GetHtmlPlot2DLinesChart(0);

    public IReadOnlyList<HtmlPlot2DLinesChart> GetHtmlPlot2DLinesCharts() => GetHtmlPlot2DLinesCharts(0);

    public void UpdateOrAddScatter(int plotIndex, string legendText, IReadOnlyList<Point> points, Color? color = null)
    {
#pragma warning disable IDE0079
#pragma warning disable IDISP001

        var plot = Multiplot.GetPlot(plotIndex);

#pragma warning restore IDISP001
#pragma warning restore IDE0079

        var scatter = plot.PlottableList.OfType<Scatter>().SingleOrDefault(t => t.LegendText == legendText);
        if (scatter is null)
        {
            scatter = plot.Add.Scatter(points.Select(t => new Coordinates(t.X, t.Y)).ToArray(), color);
            scatter.LegendText = legendText;
        }
        else
        {
            ObjectHelper.SetFieldValue(
                scatter,
                "<Data>k__BackingField",
                new ScatterSourceCoordinatesArray(points.Select(t => new Coordinates(t.X, t.Y)).ToArray()));
        }
    }

    public void UpdateOrAddScatter(int plotIndex, string legendText, IReadOnlyList<Point> points, int position)
        => UpdateOrAddScatter(plotIndex, legendText, points, Category10.GetColor(position));

    public void UpdateOrAddScatter(int plotIndex, string legendText, IReadOnlyList<Point> points, double position, Range range)
        => UpdateOrAddScatter(plotIndex, legendText, points, Turbo.GetColor(position, range));

    public void UpdateOrAddScatter(string legendText, IReadOnlyList<Point> points, Color? color = null)
        => UpdateOrAddScatter(0, legendText, points, color);

    public void UpdateOrAddScatter(string legendText, IReadOnlyList<Point> points, int position)
        => UpdateOrAddScatter(0, legendText, points, position);

    public void UpdateOrAddScatter(string legendText, IReadOnlyList<Point> points, double position, Range range)
        => UpdateOrAddScatter(0, legendText, points, position, range);

    public new void Refresh()
    {
        foreach (var plot in Multiplot.GetPlots())
        {
            plot.Axes.AutoScale();
            plot.PlotControl?.Refresh();
        }

        _originalControl?.Refresh();
        base.Refresh();
    }
}