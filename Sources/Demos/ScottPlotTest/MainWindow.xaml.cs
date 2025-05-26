using ScottPlot;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace ScottPlotTest;

public partial class MainWindow
{
    private int _rowAndColumnCount;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Paint()
    {
#if NET
        var timestamp = Stopwatch.GetTimestamp();
#else
        var stopWatch = Stopwatch.StartNew();
#endif

        WpfPlot.Plot.Clear();
        // 芯片中间的间隔
        const int dX = 15;
        const int dY = 15;

        // 视野显示宽度高度
        const int viewShowWidthPx = 30;
        const int viewShowHeightPx = 30;
        // 创建绘制参数和矩形对象

        var list = new List<Coordinates>();
        // 开始绘制
        for (var row = 0; row < _rowAndColumnCount; row++)
        {
            for (var column = 0; column < _rowAndColumnCount; column++)
            {
                // 计算矩形的坐标
                var xView = column * viewShowWidthPx;
                var yView = row * viewShowHeightPx;
                var xCell1 = xView + dX;
                var yCell1 = yView + dY;
                var xCell2 = xView + viewShowWidthPx;
                var yCell2 = yView + viewShowHeightPx;
                var location = new Coordinates(xCell1, yCell1);
                var size = new CoordinateSize(Math.Abs(xCell2 - xCell1), Math.Abs(yCell2 - yCell1));
                var rect = new CoordinateRect(location, size);
                // WpfPlot.Plot.Add.Rectangle(rect);
                // WpfPlot.Plot.Add.Marker(location);
                list.Add(location);
            }
        }

        WpfPlot.Plot.Add.ScatterPoints(list);

        WpfPlot.Plot.Axes.AutoScale();
        WpfPlot.Refresh();

#if NET
        var elapsedTime = Stopwatch.GetElapsedTime(timestamp);
        TextBlockTime.Text = $"running mean: {elapsedTime.Milliseconds:0.000} ms";
        TextBlockFps.Text = $"Fps: {1000d / elapsedTime.Milliseconds:0.000}";
#else
        stopWatch.Stop();
        TextBlockTime.Text = $"running mean: {stopWatch.Elapsed.Milliseconds:0.000} ms";
        TextBlockFps.Text = $"Fps: {1000d / stopWatch.Elapsed.Milliseconds:0.000}";
#endif
    }

    private void ButtonBaseOnClick(object sender, RoutedEventArgs e)
    {
        _rowAndColumnCount = int.Parse(((Button)sender).Tag.ToString() ?? throw new NullReferenceException());

        Paint();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        _rowAndColumnCount = int.Parse(TextBox.Text ?? throw new NullReferenceException());

        Paint();
    }
}