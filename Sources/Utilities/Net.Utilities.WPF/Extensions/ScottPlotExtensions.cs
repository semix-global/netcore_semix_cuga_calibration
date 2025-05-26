using ScottPlot;
using ScottPlot.Interactivity;
using ScottPlot.Interactivity.UserActionResponses;
using ScottPlot.Plottables;
using ScottPlot.WPF;
using System.Windows.Input;
using Key = ScottPlot.Interactivity.Key;
using MouseButton = ScottPlot.Interactivity.MouseButton;

namespace Net.Utilities.WPF.Extensions;

public static class ScottPlotExtensions
{
    public static void ConfigureWpfPlotScatter(this WpfPlot wpfPlot)
    {
        wpfPlot.ConfigureWpfPlotCommon();

        var annotation = wpfPlot.Plot.Add.Annotation(string.Empty, Alignment.UpperRight);
        annotation.LabelBackgroundColor = Colors.Yellow;
        annotation.IsVisible = false;

        var crossHair = wpfPlot.Plot.Add.Crosshair(0, 0);
        crossHair.IsVisible = false;
        crossHair.LineColor = Colors.Red;
        crossHair.TextColor = Colors.White;
        crossHair.TextBackgroundColor = Colors.Red;
        crossHair.MarkerShape = MarkerShape.OpenCircle;
        crossHair.MarkerSize = 10;

        // 多播委托后加的, 所以后执行, 移除必须是static方法
        wpfPlot.MouseMove -= OnWpfPlotOnMouseMove;
        wpfPlot.MouseMove += OnWpfPlotOnMouseMove;

        return;

        static void OnWpfPlotOnMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not WpfPlot plot) return;

            var position = e.GetPosition(plot);
            var mousePixel = new Pixel(position.X, position.Y);
            var mouseLocation = plot.Plot.GetCoordinates(mousePixel);

            var nearestList = plot.Plot.PlottableList.OfType<Scatter>()
                .Select(t => t.Data.GetNearest(mouseLocation, plot.Plot.LastRender))
                .Where(t => t.IsReal)
                .ToList();

            var crossHair = plot.Plot.PlottableList.OfType<Crosshair>().First();
            var annotation = plot.Plot.PlottableList.OfType<Annotation>().First();

            if (nearestList.Count > 0 && nearestList[0].IsReal)
            {
                crossHair.IsVisible = true;
                crossHair.Position = nearestList[0].Coordinates;
                annotation.IsVisible = true;
                annotation.LabelText = $"{FormatDouble(nearestList[0].X)}, {FormatDouble(nearestList[0].Y)}";
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

    public static void ConfigureWpfPlotCommon(this WpfPlot wpfPlot)
    {
        wpfPlot.Plot.Clear();

        wpfPlot.UserInputProcessor.UserActionResponses.Clear();
        wpfPlot.UserInputProcessor.IsEnabled = true;

        wpfPlot.UserInputProcessor.UserActionResponses.Add(new MouseWheelZoom(StandardKeys.Shift, StandardKeys.Control)); // 滚轮: 缩放

        wpfPlot.UserInputProcessor.UserActionResponses.Add(new MouseDragZoomRectangle(StandardMouseButtons.Middle) { SecondaryMouseButton = new MouseButton(string.Empty), SecondaryKey = new Key(string.Empty) }); // 中间单击拖动: 矩形选择缩放
        wpfPlot.UserInputProcessor.UserActionResponses.Add(new SingleClickAutoscale(StandardMouseButtons.Middle)); // 中键单击: 自适应
        wpfPlot.UserInputProcessor.UserActionResponses.Add(new DoubleClickBenchmark(StandardMouseButtons.Middle)); // 中键双击: 性能测试

        wpfPlot.UserInputProcessor.UserActionResponses.Add(new SingleClickContextMenu(StandardMouseButtons.Right)); // 右键单击: 菜单
        wpfPlot.UserInputProcessor.UserActionResponses.Add(new MouseDragZoom(StandardMouseButtons.Right)); // 右键单击拖动: X or Y缩放

        wpfPlot.UserInputProcessor.UserActionResponses.Add(new MouseDragPan(StandardMouseButtons.Left)); // 左键拖动: 平移

        wpfPlot.UserInputProcessor.UserActionResponses.Add(new KeyboardPanAndZoom()); // 上下左右

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