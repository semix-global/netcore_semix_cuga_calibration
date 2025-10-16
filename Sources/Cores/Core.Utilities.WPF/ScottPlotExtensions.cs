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
    public static void ConfigureWpfPlotScatter(this WpfPlot wpfPlot, IMultiplotLayout layout, int totalPlotCount)
    {
        wpfPlot.ConfigureWpfPlotCommon(layout, totalPlotCount);

        foreach (var plot in wpfPlot.Multiplot.GetPlots())
        {
            var annotation = plot.Add.Annotation(string.Empty, Alignment.UpperRight);
            annotation.LabelBackgroundColor = Colors.Yellow;
            annotation.IsVisible = false;

            var crossHair = plot.Add.Crosshair(0, 0);
            crossHair.IsVisible = false;
            crossHair.LineColor = Colors.Red;
            crossHair.TextColor = Colors.White;
            crossHair.TextBackgroundColor = Colors.Red;
            crossHair.MarkerShape = MarkerShape.OpenCircle;
            crossHair.MarkerSize = 10;
        }

        // 多播委托后加的, 所以后执行, 移除必须是static方法
        wpfPlot.MouseMove -= OnWpfPlotOnMouseMove;
        wpfPlot.MouseMove += OnWpfPlotOnMouseMove;

        return;

        static void OnWpfPlotOnMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not WpfPlot plot) return;

            var position = e.GetPosition(plot);
            var mousePixel = new Pixel(position.X, position.Y);
#pragma warning disable IDISP001
            var plotPlot = plot.Multiplot.GetPlotAtPixel(mousePixel);
#pragma warning restore IDISP001
            if (plotPlot is null) return;

            var mouseLocation = plotPlot.GetCoordinates(mousePixel);

            var nearestList = plotPlot.PlottableList.OfType<Scatter>()
                .Select(t => (Scatter: t, Result: t.Data.GetNearest(mouseLocation, plotPlot.LastRender)))
                .Where(t => t.Result.IsReal)
                .ToList();

            var crossHair = plotPlot.PlottableList.OfType<Crosshair>().First();
            var annotation = plotPlot.PlottableList.OfType<Annotation>().First();

            if (nearestList.Count > 0 && nearestList[0].Result.IsReal)
            {
                crossHair.IsVisible = true;
                crossHair.Position = nearestList[0].Result.Coordinates;
                annotation.IsVisible = true;
                annotation.LabelText = $"{nearestList[0].Scatter.LegendText}: {FormatDouble(nearestList[0].Result.X)}, {FormatDouble(nearestList[0].Result.Y)}";
            }
            else
            {
                crossHair.IsVisible = false;
                annotation.IsVisible = false;
            }

            e.Handled = true;
            plot.Refresh();
        }

        static string FormatDouble(double value)
        {
            var result = value.ToString("f3");

            if (value - Math.Truncate(value) == 0) return value.ToString("f0");

            if (result.EndsWith(".000")) result = value.ToString("e3");

            return result;
        }
    }

    public static void ConfigureWpfPlotCommon(this WpfPlot wpfPlot, IMultiplotLayout layout, int totalPlotCount)
    {
        wpfPlot.Multiplot.Reset();
        wpfPlot.Multiplot.Layout = layout;
        wpfPlot.Multiplot.AddPlots(totalPlotCount);
        foreach (var plot in wpfPlot.Multiplot.GetPlots()) plot.Clear();

        wpfPlot.UserInputProcessor.UserActionResponses.Clear();
        wpfPlot.UserInputProcessor.IsEnabled = true;

        wpfPlot.UserInputProcessor.UserActionResponses.Add(new MouseWheelZoom(StandardKeys.Shift, StandardKeys.Control)); // 滚轮: 缩放

        wpfPlot.UserInputProcessor.UserActionResponses.Add(new MouseDragPanOrZoomRectangle(StandardMouseButtons.Middle, StandardKeys.Alt)); // 中键拖动: 平移, 选择缩放
        wpfPlot.UserInputProcessor.UserActionResponses.Add(new DoubleClickResponse(StandardMouseButtons.Middle, SingleClickAutoscale.AutoScale)); // 中键双击: 自适应

        wpfPlot.UserInputProcessor.UserActionResponses.Add(new SingleClickContextMenu(StandardMouseButtons.Right)); // 右键单击: 菜单

        wpfPlot.UserInputProcessor.UserActionResponses.Add(new KeyboardPanAndZoom()); // 上下左右

        wpfPlot.Menu?.Reset();
        wpfPlot.Menu?.Add("Benchmark", plot =>
        {
            plot.Benchmark.IsVisible = !plot.Benchmark.IsVisible;
            plot.PlotControl?.Refresh();
        });

        // 多播委托后加的, 所以后执行, 移除必须是static方法
        wpfPlot.MouseWheel -= OnWpfPlotOnMouseWheel;
        wpfPlot.MouseWheel += OnWpfPlotOnMouseWheel;

        return;

        static void OnWpfPlotOnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not WpfPlot) return;

            e.Handled = true;
        }
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