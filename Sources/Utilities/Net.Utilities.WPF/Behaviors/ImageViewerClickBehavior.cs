using HandyControl.Controls;
using Microsoft.Xaml.Behaviors;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = Net.Utilities.Models.Point;

namespace Net.Utilities.WPF.Behaviors;

public sealed class ImageViewerClickBehavior : Behavior<ImageViewer>
{
    #region 依赖属性

    private readonly PropertyInfo _imageScalePropertyInfo = typeof(ImageViewer).GetProperty("ImageScale", BindingFlags.NonPublic | BindingFlags.Instance)!;
    private readonly PropertyInfo _imageMarginPropertyInfo = typeof(ImageViewer).GetProperty("ImageMargin", BindingFlags.NonPublic | BindingFlags.Instance)!;

    public static readonly DependencyProperty ClickPositionProperty = DependencyProperty.Register(
        nameof(ClickPosition),
        typeof(Point),
        typeof(ImageViewerClickBehavior),
        new FrameworkPropertyMetadata(Point.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPropertyChangedCallback));

    private static void OnPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ImageViewerClickBehavior behavior) return;
        if (behavior.AssociatedObject.ImageSource is null) return;

        var imageScale = (double)behavior._imageScalePropertyInfo.GetValue(behavior.AssociatedObject)!;
        var imageMargin = (Thickness)behavior._imageMarginPropertyInfo.GetValue(behavior.AssociatedObject)!;

        if (e.NewValue is Point point)
        {
            if (new Rect(0, 0, behavior.AssociatedObject.ImageSource.PixelWidth, behavior.AssociatedObject.ImageSource.PixelHeight).Contains(new System.Windows.Point(point.X, point.Y)) == false)
            {
                behavior.ClickPosition = Point.Empty;
                return;
            }
        }
        else if (e.NewValue is BitmapSource)
        {
            behavior.Paint(imageScale, imageMargin);
        }
    }

    public static readonly DependencyProperty IsShowCrossProperty = DependencyProperty.Register(
        nameof(IsShowCross),
        typeof(bool),
        typeof(ImageViewerClickBehavior),
        new PropertyMetadata(true));

    public static readonly DependencyProperty CrossLengthProperty = DependencyProperty.Register(
        nameof(CrossLength),
        typeof(int),
        typeof(ImageViewerClickBehavior),
        new PropertyMetadata(30));

    public static readonly DependencyProperty BitmapSourceProperty = DependencyProperty.Register(
        nameof(BitmapSource),
        typeof(BitmapSource),
        typeof(ImageViewerClickBehavior),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPropertyChangedCallback));

    public Point ClickPosition
    {
        get => (Point)GetValue(ClickPositionProperty);
        set => SetValue(ClickPositionProperty, value);
    }

    public bool IsShowCross
    {
        get => (bool)GetValue(IsShowCrossProperty);
        set => SetValue(IsShowCrossProperty, value);
    }

    public int CrossLength
    {
        get => (int)GetValue(CrossLengthProperty);
        set => SetValue(CrossLengthProperty, value);
    }

    public required BitmapSource BitmapSource
    {
        get => (BitmapSource)GetValue(BitmapSourceProperty);
        set => SetValue(BitmapSourceProperty, value);
    }

    #endregion 依赖属性

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.MouseRightButtonUp -= OnMouseRightButtonUp;
        AssociatedObject.MouseRightButtonUp += OnMouseRightButtonUp;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        AssociatedObject.MouseRightButtonUp -= OnMouseRightButtonUp;
    }

    private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (AssociatedObject.ImageSource is null) return;

        var point = e.GetPosition(AssociatedObject);

        var imageScale = (double)_imageScalePropertyInfo.GetValue(AssociatedObject)!;
        var imageMargin = (Thickness)_imageMarginPropertyInfo.GetValue(AssociatedObject)!;

        point.X = (point.X - imageMargin.Left) / imageScale;
        point.Y = (point.Y - imageMargin.Top) / imageScale;

        if (new Rect(0, 0, AssociatedObject.ImageSource.PixelWidth, AssociatedObject.ImageSource.PixelHeight).Contains(point) == false)
        {
            ClickPosition = Point.Empty;
            return;
        }

        ClickPosition = new Point(point.X, point.Y);

        Paint(imageScale, imageMargin);
    }

    private void Paint(double imageScale, Thickness imageMargin)
    {
        if (AssociatedObject.ImageSource is null) return;

        if (IsShowCross == false) return;

        var drawingVisual = new DrawingVisual();
        using (var drawingContext = drawingVisual.RenderOpen())
        {
            drawingContext.DrawImage(BitmapSource, new Rect(0, 0, AssociatedObject.ImageSource.PixelWidth, AssociatedObject.ImageSource.PixelHeight));
            var redPen = new Pen(Brushes.Red, 5);
            drawingContext.DrawLine(redPen, new System.Windows.Point(ClickPosition.X - CrossLength, ClickPosition.Y), new System.Windows.Point(ClickPosition.X + CrossLength, ClickPosition.Y));
            drawingContext.DrawLine(redPen, new System.Windows.Point(ClickPosition.X, ClickPosition.Y - CrossLength), new System.Windows.Point(ClickPosition.X, ClickPosition.Y + CrossLength));
        }

        var renderTargetBitmap = new RenderTargetBitmap(AssociatedObject.ImageSource.PixelWidth, AssociatedObject.ImageSource.PixelHeight, AssociatedObject.ImageSource.DpiX, AssociatedObject.ImageSource.DpiY, PixelFormats.Pbgra32);
        renderTargetBitmap.Render(drawingVisual);
        renderTargetBitmap.Freeze();

        AssociatedObject.ImageSource = BitmapFrame.Create(renderTargetBitmap);

        _imageScalePropertyInfo.SetValue(AssociatedObject, imageScale);
        _imageMarginPropertyInfo.SetValue(AssociatedObject, imageMargin);
    }
}