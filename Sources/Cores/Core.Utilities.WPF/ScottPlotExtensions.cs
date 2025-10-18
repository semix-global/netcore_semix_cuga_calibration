using System.Windows;
using System.Windows.Input;
using Net.Utilities.Models;
using ScottPlot;
using ScottPlot.Interactivity;
using ScottPlot.Interactivity.UserActionResponses;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using SkiaSharp.Views.WPF;
using Key = ScottPlot.Interactivity.Key;
using MouseButton = ScottPlot.Interactivity.MouseButton;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace Core.Utilities.WPF;

public static class ScottPlotExtensions
{
    public static void ConfigureWpfPlotScatter(this IPlotControl plotControl, IMultiplotLayout? layout = null, int totalPlotCount = 1)
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

    public static void ConfigureWpfPlotCommon(this IPlotControl plotControl, IMultiplotLayout? layout, int totalPlotCount)
    {
        plotControl.Multiplot.Reset();
        if (layout is not null) plotControl.Multiplot.Layout = layout;
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

        plotControl.Menu?.Clear();

        const string showLegend = "Show Legend Item";
        const string hideLegend = "High Legend Item";
        plotControl.Menu?.Add(hideLegend, plot =>
        {
            var contextMenuItems = GuardUtils.IsAssignableToType<WpfPlotMenu>(plotControl.Menu).ContextMenuItems;
            var index = contextMenuItems.FindIndex(t => t.Label == (plot.Legend.ShowItemsFromHiddenPlottables ? hideLegend : showLegend));
            plot.Legend.ShowItemsFromHiddenPlottables = !plot.Legend.ShowItemsFromHiddenPlottables;

            var contextMenuItem = contextMenuItems[index];
            contextMenuItem.Label = plot.Legend.ShowItemsFromHiddenPlottables ? hideLegend : showLegend;
            contextMenuItems[index] = contextMenuItem;

            plot.PlotControl?.Refresh();
        });

        plotControl.Menu?.Add("Open in New Window", plot =>
        {
            var originalControl = plot.PlotControl;
            var plotControlTemp = new PlotControl();
            plotControlTemp.ConfigureWpfPlotScatter();
            plotControlTemp.Reset(plot);

            Window win = new()
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Width = 600,
                Height = 400,
                Title = "Interactive Plot",
                Content = plotControlTemp,
                Topmost = true
            };
            win.Closed += (_, _) => plot.PlotControl = originalControl;

            win.Show();
        });

        plotControl.Menu?.Add("Save Image", plot =>
        {
            if (plotControl.Menu is null) return;

            GuardUtils.IsAssignableToType<WpfPlotMenu>(plotControl.Menu).OpenSaveImageDialog(plot);
        });
        plotControl.Menu?.Add("Copy to Clipboard", WpfPlotMenu.CopyImageToClipboard);

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
        if (plotPlot is null) return ResponseInfo.NoActionRequired;

#pragma warning restore IDISP001
#pragma warning restore IDE0079

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
        var luma = (int)Math.Sqrt(backgroundColor.Red * backgroundColor.Red * 0.299 + backgroundColor.Green * backgroundColor.Green * 0.587 + backgroundColor.Blue * backgroundColor.Blue * 0.114);

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

            return;
        }


        e.Handled = true;
    }
}