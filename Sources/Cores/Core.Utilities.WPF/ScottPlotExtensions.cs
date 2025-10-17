using System.Windows.Input;
using ScottPlot;
using ScottPlot.Interactivity;
using ScottPlot.Interactivity.UserActionResponses;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using Key = ScottPlot.Interactivity.Key;
using MouseButton = ScottPlot.Interactivity.MouseButton;

namespace Core.Utilities.WPF;

public static class ScottPlotExtensions
{
    public static void ConfigureWpfPlotScatter(this IPlotControl plotControl, IMultiplotLayout layout, int totalPlotCount)
    {
        plotControl.ConfigureWpfPlotCommon(layout, totalPlotCount);

        foreach (var plot in plotControl.Multiplot.GetPlots())
        {
            var annotation = plot.Add.Annotation(string.Empty, Alignment.UpperRight);
            annotation.IsVisible = false;

            var crossHair = plot.Add.Crosshair(0, 0);
            crossHair.IsVisible = false;
            crossHair.LineColor = Colors.Red;
        }

        plotControl.UserInputProcessor.UserActionResponses.Add(new MouseMove()); // 显示位置
    }

    public static void ConfigureWpfPlotCommon(this IPlotControl plotControl, IMultiplotLayout layout, int totalPlotCount)
    {
        plotControl.Multiplot.Reset();
        plotControl.Multiplot.Layout = layout;
        plotControl.Multiplot.AddPlots(totalPlotCount);
        foreach (var plot in plotControl.Multiplot.GetPlots())
        {
            plot.Legend.ShowItemsFromHiddenPlottables = true;
            plot.Clear();
        }

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

internal sealed class MouseDragPanOrZoomRectangle(MouseButton button, Key dragZoomRectangleKey) : IUserActionResponse
{
    private readonly MouseDragPan _mouseDragPan = new(button)
    {
        LockY = false,
        LockX = false,
        ChangeOpposingAxesTogether = false
    };

    private readonly MouseDragZoomRectangle _mouseDragZoomRectangle = new(button)
    {
        SecondaryMouseButton = new MouseButton(string.Empty),
        SecondaryKey = new Key(string.Empty)
    };

    public MouseButton MouseButton { get; } = button;

    public Key DragZoomRectangleKey { get; } = dragZoomRectangleKey;

    #region MouseDragPan

    public bool LockY
    {
        get => _mouseDragPan.LockY;
        set => _mouseDragPan.LockY = value;
    }

    public bool LockX
    {
        get => _mouseDragPan.LockX;
        set => _mouseDragPan.LockX = value;
    }

    public bool ChangeOpposingAxesTogether
    {
        get => _mouseDragPan.ChangeOpposingAxesTogether;
        set => _mouseDragPan.ChangeOpposingAxesTogether = value;
    }

    #endregion MouseDragPan

    #region MouseDragZoomRectangle

    public Key MouseDragZoomRectangleHorizontalLockKey
    {
        get => _mouseDragZoomRectangle.HorizontalLockKey;
        set => _mouseDragZoomRectangle.HorizontalLockKey = value;
    }

    public Key MouseDragZoomRectangleVerticalLockKey
    {
        get => _mouseDragZoomRectangle.VerticalLockKey;
        set => _mouseDragZoomRectangle.VerticalLockKey = value;
    }

    #endregion MouseDragZoomRectangle

    public void ResetState(IPlotControl plotControl)
    {
        _mouseDragPan.ResetState(plotControl);
        _mouseDragZoomRectangle.ResetState(plotControl);
    }

    public ResponseInfo Execute(IPlotControl plotControl, IUserAction userInput, KeyboardState keys)
    {
        return keys.IsPressed(DragZoomRectangleKey)
            ? _mouseDragZoomRectangle.Execute(plotControl, userInput, keys)
            : _mouseDragPan.Execute(plotControl, userInput, keys);
    }
}

internal sealed class MouseMove : IUserActionResponse
{
    public void ResetState(IPlotControl plotControl)
    {
    }

    public ResponseInfo Execute(IPlotControl plotControl, IUserAction userInput, KeyboardState keys)
    {
        if (userInput is not IMouseAction mouseAction) return ResponseInfo.NoActionRequired;

        var mousePixel = mouseAction.Pixel;

#pragma warning disable IDE0079
#pragma warning disable IDISP001

        var plotPlot = plotControl.Multiplot.GetPlotAtPixel(mousePixel);

#pragma warning restore IDISP001
#pragma warning restore IDE0079

        if (plotPlot is null) return ResponseInfo.NoActionRequired;

        var mouseLocation = plotPlot.GetCoordinates(mousePixel);

        var nearestList = plotPlot.PlottableList.OfType<Scatter>()
            .Select(t => (Scatter: t, Result: t.Data.GetNearest(mouseLocation, plotPlot.LastRender)))
            .Where(t => t.Result.IsReal)
            .ToList();

        var crossHair = plotPlot.PlottableList.OfType<Crosshair>().First();
        var annotation = plotPlot.PlottableList.OfType<Annotation>().First();

        if (nearestList.Count > 0 && nearestList[^1].Result.IsReal)
        {
            crossHair.IsVisible = true;
            crossHair.Position = nearestList[^1].Result.Coordinates;
            crossHair.MarkerShape = MarkerShape.OpenCircle;
            crossHair.MarkerSize = 10;

            annotation.IsVisible = true;
            annotation.LabelText = $"{nearestList[^1].Scatter.LegendText}: {FormatDouble(nearestList[^1].Result.X)}, {FormatDouble(nearestList[^1].Result.Y)}";
            annotation.LabelBackgroundColor = nearestList[^1].Scatter.LineColor;
            annotation.LabelBold = true;
        }
        else
        {
            crossHair.IsVisible = true;
            crossHair.Position = mouseLocation;
            crossHair.MarkerShape = MarkerShape.None;
            crossHair.MarkerSize = 0;

            annotation.IsVisible = true;
            annotation.LabelText = $"{FormatDouble(mouseLocation.X)}, {FormatDouble(mouseLocation.Y)}";
            annotation.LabelBackgroundColor = Colors.Yellow;
            annotation.LabelBold = false;
        }

        annotation.LabelFontColor = ToForegroundColor(annotation.LabelBackgroundColor);

        return ResponseInfo.Refresh;
    }

    private static string FormatDouble(double value)
    {
        var result = value.ToString("f3");

        if (value - Math.Truncate(value) == 0) return value.ToString("f0");

        if (result.EndsWith(".000")) result = value.ToString("e3");

        return result;
    }

    private static Color ToForegroundColor(Color backgroundColor)
    {
        // 使用加权平方根公式计算亮度（人眼感知模型）
        var luma = (int)Math.Sqrt(backgroundColor.Red * backgroundColor.Red * 0.299 + backgroundColor.Green * backgroundColor.Green * 0.587 + backgroundColor.Blue * backgroundColor.Blue * 0.114); // todo: 背景色亮度luma

        return luma > 130 ? Colors.Black : Colors.White;
    }
}

internal sealed class PlotControl : WpfPlot
{
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
}