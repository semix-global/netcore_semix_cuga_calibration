using Core.Utilities.WPF.ScottPlot.Interactivity.UserActionResponses;
using Net.Utilities.Models.Geometries;
using ScottPlot;
using ScottPlot.Interactivity;
using ScottPlot.Interactivity.UserActionResponses;
using ScottPlot.Plottables;

namespace Core.Utilities.WPF.ScottPlot;

public static class ScottPlotExtensions
{
    public static string GetTitle(this Plot plot) => plot.Axes.Title.Label.Text;

    public static (string Name, Point[] Points)[] GetScatterPoints(this Plot plot) =>
        plot.PlottableList.OfType<Scatter>().Select<Scatter, (string Name, Point[] Points)>(t =>
        {
            t.Data.MinRenderIndex = 0;
            t.Data.MaxRenderIndex = int.MaxValue;

            return (Name: t.LegendText, Points: t.Data.GetScatterPoints().Select(tt => new Point(tt.X, tt.Y)).ToArray());
        }).ToArray();

    public static void ConfigureCommon(this IPlotControl plotControl, IMultiplotLayout? layout, int totalPlotCount)
    {
        if (layout is not null) plotControl.Multiplot.Layout = layout;
        plotControl.Multiplot.AddPlots(totalPlotCount);

        foreach (var plot in plotControl.Multiplot.GetPlots()) plot.Clear();

        plotControl.UserInputProcessor.UserActionResponses.Clear();
        plotControl.UserInputProcessor.IsEnabled = true;

        plotControl.UserInputProcessor.UserActionResponses.Add(new MouseWheelZoom(StandardKeys.Shift, StandardKeys.Control)); // 滚轮: 缩放

        plotControl.UserInputProcessor.UserActionResponses.Add(new MouseDragPanOrZoomRectangle(StandardMouseButtons.Middle, StandardKeys.Alt)); // 中键拖动: 平移, 选择缩放
        plotControl.UserInputProcessor.UserActionResponses.Add(new DoubleClickResponse(StandardMouseButtons.Middle, SingleClickAutoscale.AutoScale)); // 中键双击: 自适应

        plotControl.UserInputProcessor.UserActionResponses.Add(new SingleClickContextMenu(StandardMouseButtons.Right)); // 右键单击: 菜单

        plotControl.UserInputProcessor.UserActionResponses.Add(new KeyboardPanAndZoom()); // 上下左右

        plotControl.Menu?.Reset();
        plotControl.Menu?.Add("Benchmark", plot =>
        {
            plot.Benchmark.IsVisible = !plot.Benchmark.IsVisible;

            plot.PlotControl?.Refresh();
        });
    }
}