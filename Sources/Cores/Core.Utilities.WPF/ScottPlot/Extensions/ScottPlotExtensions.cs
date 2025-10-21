using Core.Utilities.WPF.ScottPlot.Interactivity.UserActionResponses;
using Net.Utilities.Models.Geometries;
using ScottPlot;
using ScottPlot.Interactivity;
using ScottPlot.Interactivity.UserActionResponses;
using ScottPlot.Plottables;

namespace Core.Utilities.WPF.ScottPlot.Extensions;

public static class ScottPlotExtensions
{
    public static string GetTitle(this Plot @this) => @this.Axes.Title.Label.Text;

    public static (string Name, Point[] Points)[] GetScatterPoints(this Plot plot) =>
        plot.PlottableList.OfType<Scatter>().Select<Scatter, (string Name, Point[] Points)>(t =>
        {
            t.Data.MinRenderIndex = 0;
            t.Data.MaxRenderIndex = int.MaxValue;

            return (Name: t.LegendText, Points: t.Data.GetScatterPoints().Select(tt => new Point(tt.X, tt.Y)).ToArray());
        }).ToArray();

    public static Color ToReadableForegroundColor(this Color backgroundColor)
    {
        // 使用加权平方根公式计算亮度（人眼感知模型）
        var luma = (int)Math.Sqrt(backgroundColor.Red * backgroundColor.Red * 0.299 + backgroundColor.Green * backgroundColor.Green * 0.587 + backgroundColor.Blue * backgroundColor.Blue * 0.114);

        return luma > 130 ? Colors.Black : Colors.White;
    }

    public static void ConfigureCommon(this IPlotControl @this, IMultiplotLayout? layout, int totalPlotCount)
    {
        if (layout is not null) @this.Multiplot.Layout = layout;
        @this.Multiplot.AddPlots(totalPlotCount);

        foreach (var plot in @this.Multiplot.GetPlots()) plot.Clear();

        @this.UserInputProcessor.UserActionResponses.Clear();
        @this.UserInputProcessor.IsEnabled = true;

        @this.UserInputProcessor.UserActionResponses.Add(new MouseWheelZoom(StandardKeys.Shift, StandardKeys.Control)); // 滚轮: 缩放

        @this.UserInputProcessor.UserActionResponses.Add(new MouseDragPanOrZoomRectangle(StandardMouseButtons.Middle, StandardKeys.Alt)); // 中键拖动: 平移, 选择缩放
        @this.UserInputProcessor.UserActionResponses.Add(new DoubleClickResponse(StandardMouseButtons.Middle, SingleClickAutoscale.AutoScale)); // 中键双击: 自适应

        @this.UserInputProcessor.UserActionResponses.Add(new SingleClickContextMenu(StandardMouseButtons.Right)); // 右键单击: 菜单

        @this.UserInputProcessor.UserActionResponses.Add(new KeyboardPanAndZoom()); // 上下左右

        @this.Menu?.Reset();
        @this.Menu?.Add("Benchmark", plot =>
        {
            plot.Benchmark.IsVisible = !plot.Benchmark.IsVisible;

            plot.PlotControl?.Refresh();
        });
    }
}

public static class ScottPlotHelper
{
    public static string FormatDouble(double value)
    {
        var result = value.ToString("f3");

        if (value - Math.Truncate(value) == 0) return value.ToString("f0");

        if (result.EndsWith(".000")) result = value.ToString("e3");

        return result;
    }
}