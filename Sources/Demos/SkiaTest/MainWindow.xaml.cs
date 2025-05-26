using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SkiaTest;

public partial class MainWindow
{
    private static SKMatrix _viewMatrix = SKMatrix.CreateIdentity();
    private static readonly SKPaint Paint = new();

    private SKPoint _lastPoint;
    private int _rowAndColumnCount;
    private bool _isPanning;
    private float _scaleFactor = 1.0f;

    public MainWindow()
    {
        InitializeComponent();
        SkElement.MouseUp += SkElementOnMouseUp;
        SkElement.MouseDown += SkElementOnMouseDown;
        SkElement.MouseMove += SkElementOnMouseMove;
        SkElement.MouseWheel += SkElementOnMouseWheel;

        Paint.Color = SKColors.LightBlue;
        Paint.IsAntialias = false;
    }

    private void SkElementOnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isPanning = false;
    }

    private void SkElementOnMouseDown(object sender, MouseButtonEventArgs e)
    {
        // 记录鼠标按下时的位置
        _lastPoint = e.GetPosition(SkElement).ToSKPoint();
        _isPanning = true;
    }

    private void SkElementOnMouseMove(object sender, MouseEventArgs e)
    {
        // 如果鼠标左键被按下，则进行移动操作
        if (_isPanning == false) return;

        var point = e.GetPosition(SkElement).ToSKPoint();

        var deltaX = (point - _lastPoint).X / _scaleFactor;
        var deltaY = (point - _lastPoint).Y / _scaleFactor;
        var makeTranslation = SKMatrix.CreateTranslation(deltaX, deltaY);
        _viewMatrix = SKMatrix.Concat(_viewMatrix, makeTranslation);
        _lastPoint = point;

        SkElement.InvalidateVisual();
    }

    private void SkElementOnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // 获取滚轮滚动的增量
        float delta = e.Delta;

        // 计算缩放因子
        var scaleFactor = delta > 0 ? 1.1f : 0.9f;
        _scaleFactor *= scaleFactor;

        // 获取当前鼠标位置
        var point = e.GetPosition(SkElement);

        // 更新视图矩阵
        var makeScale = SKMatrix.CreateScale(scaleFactor, scaleFactor, (float)point.X, (float)point.Y);
        _viewMatrix = SKMatrix.Concat(_viewMatrix, makeScale);

        SkElement.InvalidateVisual();
    }

    private void SKElement_OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
#if NET
        var timestamp = Stopwatch.GetTimestamp();
#else
        var stopWatch = Stopwatch.StartNew();
#endif
        var canvas = e.Surface.Canvas;
        // 应用视图矩阵
        canvas.SetMatrix(_viewMatrix);
        canvas.Clear(SKColors.White); // 清空画布

        // 芯片中间的间隔
        const int dX = 15;
        const int dY = 15;

        // 视野显示宽度高度
        const int viewShowWidthPx = 30;
        const int viewShowHeightPx = 30;
        // 创建绘制参数和矩形对象

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
                var rect = new SKRect
                {
                    // 设置矩形的位置
                    Left = xCell1,
                    Top = yCell1,
                    Right = xCell2,
                    Bottom = yCell2
                };

                // 绘制矩形
                canvas.DrawRect(rect, Paint);
            }
        }

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

        SkElement.InvalidateVisual();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        _rowAndColumnCount = int.Parse(TextBox.Text ?? throw new NullReferenceException());

        SkElement.InvalidateVisual();
    }
}