using CanvasViewer.Drawables;
using CanvasViewer.Geometry;
using CanvasViewer.Media.Drawing;
using CanvasViewer.Media.Drawing.Enum;
using CanvasViewer.View;
using System.Globalization;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Color = CanvasViewer.Media.Drawing.Color;
using Point = System.Windows.Point;
using Style = CanvasViewer.Media.Drawing.Style;

namespace CanvasViewer.Media;

public sealed class Renderer : IDisposable
{
    #region 属性

    /// <summary>
    /// 控件视图
    /// </summary>
    public CanvasView View { get; }

    /// <summary>
    /// 线宽是否不应用视图缩放
    /// </summary>
    public bool IsNotApplyScaleLineWeights { get; set; }

    /// <summary>
    /// 全局覆盖样式
    /// </summary>
    internal Style? StyleOverride { get; set; }

    #endregion 属性

    #region 绘画

    /// <summary>
    /// 缩放
    /// </summary>
    private readonly ScaleTransform _scale = new(1, 1);

    /// <summary>
    /// 原点平移
    /// </summary>
    private readonly TranslateTransform _translateCameraPosition = new();

    /// <summary>
    /// 平移到控件中心
    /// </summary>
    private readonly TranslateTransform _translateAxis = new();

    /// <summary>
    /// 变换组
    /// </summary>
    private readonly TransformGroup _transformGroup = new();

    /// <summary>
    /// 用于绘画
    /// </summary>
    private readonly DrawingVisual _drawingVisual;

    /// <summary>
    /// gdi缓存的画图对象
    /// </summary>
    private DrawingContext? _drawingContext;

    /// <summary>
    /// 画刷缓存
    /// </summary>
    private readonly Dictionary<Color, Brush> _styleBrushCacheDic = [];

    #endregion 绘画

    public Renderer(CanvasView view)
    {
        View = view;
        _transformGroup.Children.Add(_translateCameraPosition);
        _transformGroup.Children.Add(_scale);
        _transformGroup.Children.Add(_translateAxis);

        Init(View);
        View.RenderTransform = _transformGroup;

        _drawingVisual = new DrawingVisual(); /*{ CacheMode = new BitmapCache() }; // 不适用卡顿;*/
        Init(_drawingVisual);
        View.Visuals.Add(_drawingVisual);
    }

    #region 初始化方法

    private static void Init(DependencyObject uiElement)
    {
        RenderOptions.SetCachingHint(uiElement, CachingHint.Cache); // 相当于 View.CacheMode="BitmapCache"(不能直接使用, 缩放到最小卡顿)
        RenderOptions.SetBitmapScalingMode(uiElement, BitmapScalingMode.LowQuality); // 低质量缩放
        RenderOptions.SetEdgeMode(uiElement, EdgeMode.Aliased); // 禁用抗锯齿处理
        RenderOptions.SetClearTypeHint(uiElement, ClearTypeHint.Auto); // 字体渲染技术，用于提高屏幕上文本的清晰度和可读性
        RenderOptions.ProcessRenderMode = RenderMode.Default; // GPU加速
    }

    /// <summary>
    /// 初始化帧: 开始绘画<br/>
    /// 使用笛卡尔坐标系: 所以画椭圆、矩形、字体都是关于x轴对称的, 所以顶点都是按照笛卡尔左下角, 宽度高度正常<br/>
    /// 不影响曲线、封闭曲线、GraphicsPath
    /// </summary>
    public void InitFrame()
    {
        // 重要: 控件屏幕坐标系按照`控件屏幕坐标系偏移` => 控件屏幕坐标系
        _translateCameraPosition.X = -View.Camera.Position.X;
        _translateCameraPosition.Y = -View.Camera.Position.Y;
        // 缩放相机, -1使y轴换方向
        // 重要: 控件屏幕坐标系按照`控件屏幕坐标系缩放y轴反向`=> 笛卡尔坐标系
        _scale.ScaleX = 1.0d / View.Camera.Zoom;
        _scale.ScaleY = -1.0d / View.Camera.Zoom;
        // 笛卡尔坐标系偏移(原因是因为画布视野获取世界坐标关系)偏移到视图中心
        // 重要: 笛卡尔坐标系按照`控件屏幕坐标系偏移` => 笛卡尔坐标系
        _translateAxis.X = View.Camera.Width / 2d;
        _translateAxis.Y = View.Camera.Height / 2d;

        _drawingVisual.Clip = new RectangleGeometry((Rect)View.GetViewport());
        _drawingContext = _drawingVisual.RenderOpen();
    }

    /// <summary>
    /// 结束帧
    /// </summary>
    public void EndFrame()
    {
        _drawingContext?.Close();
    }

    #endregion 初始化方法

    #region 绘画方法

    /// <summary>
    /// 清除之前绘画
    /// </summary>
    /// <param name="color">颜色</param>
    public void Clear(Color color)
    {
        if (_drawingContext is null) throw new ArgumentException($"{nameof(_drawingContext)} is null");

        _drawingContext.DrawRectangle(new SolidColorBrush((System.Windows.Media.Color)color), null, (Rect)View.GetViewport());
    }

    /// <summary>
    /// 画直线
    /// </summary>
    /// <param name="style">样式</param>
    /// <param name="p1">直线起点</param>
    /// <param name="p2">直线终点</param>
    public void DrawLine(Style style, Point2D p1, Point2D p2)
    {
        if (_drawingContext is null) throw new ArgumentException($"{nameof(_drawingContext)} is null");

        var pen = CreatePen(style);
        _drawingContext.DrawLine(pen, (Point)p1, (Point)p2);
    }

    /// <summary>
    /// 画矩形
    /// </summary>
    /// <param name="style">样式</param>
    /// <param name="p1">矩形左上角点</param>
    /// <param name="p2">矩形右下角点</param>
    public void DrawRectangle(Style style, Point2D p1, Point2D p2)
    {
        if (_drawingContext is null) throw new ArgumentException($"{nameof(_drawingContext)} is null");

        var pen = CreatePen(style);
        // 因为创建GDI缓存的时候, y转向了, 所以这里面RectangleF是以左下角定义的(笛卡尔定义的)
        _drawingContext.DrawRectangle(null, pen, new Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y)));
    }

    /// <summary>
    /// 画实心矩形
    /// </summary>
    /// <param name="style">样式</param>
    /// <param name="p1">矩形左上角点</param>
    /// <param name="p2">矩形右下角点</param>
    public void FillRectangle(Style style, Point2D p1, Point2D p2)
    {
        if (_drawingContext is null) throw new ArgumentException($"{nameof(_drawingContext)} is null");

        var brush = CreateBrush(style);

        // 因为创建GDI缓存的时候, y转向了, 所以这里面RectangleF是以左下角定义的(笛卡尔定义的)
        _drawingContext.DrawRectangle(brush, null, new Rect(Math.Min(p1.X, p2.X), Math.Min(p1.Y, p2.Y), Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y)));
    }

    /// <summary>
    /// 画圆
    /// </summary>
    /// <param name="style">样式</param>
    /// <param name="center">圆心</param>
    /// <param name="radius">半径</param>
    public void DrawCircle(Style style, Point2D center, double radius)
    {
        if (_drawingContext is null) throw new ArgumentException($"{nameof(_drawingContext)} is null");

        var pen = CreatePen(style);
        // 因为创建GDI缓存的时候, y转向了, 所以这里面RectangleF是以左下角定义的(笛卡尔定义的)
        _drawingContext.DrawEllipse(null, pen, new Point(center.X - radius, center.Y - radius), 2 * radius, 2 * radius);
    }

    /// <summary>
    /// 画实心圆
    /// </summary>
    /// <param name="style">样式</param>
    /// <param name="center">圆心</param>
    /// <param name="radius">半径</param>
    public void FillCircle(Style style, Point2D center, double radius)
    {
        if (_drawingContext is null) throw new ArgumentException($"{nameof(_drawingContext)} is null");

        var brush = CreateBrush(style);
        // 因为创建GDI缓存的时候, y转向了, 所以这里面RectangleF是以左下角定义的(笛卡尔定义的)
        _drawingContext.DrawEllipse(brush, null, new Point(center.X - radius, center.Y - radius), 2 * radius, 2 * radius);
    }

    /// <summary>
    /// 绘制图片
    /// </summary>
    /// <param name="image">图片</param>
    /// <param name="rect">图片绘制区域</param>
    public void DrawImage(ImageSource image, Rect rect)
    {
        if (_drawingContext is null) throw new ArgumentException($"{nameof(_drawingContext)} is null");

        var transform = new Matrix();
        transform.Scale(1, -1); // 反转y轴以匹配GDI的行为
        _drawingContext.PushTransform(new MatrixTransform(transform));
        _drawingContext.DrawImage(image, rect);
        _drawingContext.Pop();
    }

    /// <summary>
    /// 测量文本大小
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="textColorStyle">字体颜色样式</param>
    /// <param name="textStyle">字体样式</param>
    /// <param name="fontSize">文本大小</param>
    /// <param name="maxWidth">最大文本宽度</param>
    /// <returns>文本向量</returns>
    public Vector2D MeasureString(string text, Style textColorStyle, TextStyle textStyle, double fontSize, double maxWidth = 1920)
    {
        var brush = CreateBrush(textColorStyle);
        var typeface = CreateFont(textStyle);
        return MeasureString(text, typeface, brush, fontSize, maxWidth);
    }

    /// <summary>
    /// 画文本
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="textColorStyle">样式</param>
    /// <param name="textStyle">文本样式</param>
    /// <param name="pt">点</param>
    /// <param name="fontSize">文本大小</param>
    /// <param name="hAlign">水平对齐</param>
    /// <param name="vAlign">垂直对齐</param>
    /// <param name="rotation">旋转, 只针对本次</param>
    /// <param name="maxWidth">最大文本宽度</param>
    public void DrawString(string text, Style textColorStyle, TextStyle textStyle, Point2D pt, double fontSize,
        TextHorizontalAlignmentEnum hAlign, TextVerticalAlignmentEnum vAlign, double rotation = 0, double maxWidth = 1920)
    {
        if (_drawingContext is null) throw new ArgumentException($"{nameof(_drawingContext)} is null");

        var typeface = CreateFont(textStyle);
        var brush = CreateBrush(textColorStyle);

#pragma warning disable CS0618 // Type or member is obsolete
        var formattedText = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, fontSize, brush)
        {
            Trimming = TextTrimming.CharacterEllipsis
        };
#pragma warning restore CS0618 // Type or member is obsolete

        // Calculate alignment offset
        double dx = 0;
        double dy = 0;
        var textWidth = formattedText.Width;
        formattedText.MaxTextWidth = maxWidth;
        var textHeight = formattedText.Height;

        switch (hAlign)
        {
            case TextHorizontalAlignmentEnum.Right:
                dx = -textWidth;
                break;

            case TextHorizontalAlignmentEnum.Center:
                dx = -textWidth / 2;
                break;
        }

        switch (vAlign)
        {
            case TextVerticalAlignmentEnum.Bottom:
                dy = -textHeight;
                break;

            case TextVerticalAlignmentEnum.Middle:
                dy = -textHeight / 2;
                break;
        }

        var transform = new Matrix();
        transform.Translate(pt.X, -pt.Y);
        transform.Rotate(rotation * 180 / Math.PI);
        transform.Scale(1, -1); // 反转y轴以匹配GDI的行为
        transform.Translate(dx, dy);
        _drawingContext.PushTransform(new MatrixTransform(transform));

        _drawingContext.DrawText(formattedText, new Point(0, 0));

        _drawingContext.Pop();
    }

    /// <summary>
    /// 画Drawable对象
    /// </summary>
    /// <param name="item">Drawable对象</param>
    public void Draw(AbstractDrawable item)
    {
        // 增加了刷新条件，尝试减少刷新来优化系统性
        if (View.GetViewport().IntersectsWith(item.GetExtents()) == false) return; // 相机区域(世界坐标)是否包含该对象区域
        item.Draw(this);

        // 加入List
        if (item.IsInModel) View.VisibleItems.Add(item);
    }

    #endregion 绘画方法

    #region 创建绘画材料

    /// <summary>
    /// 创建画笔
    /// </summary>
    private Pen CreatePen(Style style)
    {
        var appliedStyle = StyleOverride ?? style;
        if (_styleBrushCacheDic.TryGetValue(appliedStyle.Color, out var solidBrush) == false)
        {
            solidBrush = new SolidColorBrush(appliedStyle.Color.IsByLayer ? Colors.White : (System.Windows.Media.Color)appliedStyle.Color);
            _styleBrushCacheDic.Add(appliedStyle.Color, solidBrush);
        }

        var thickness = GetScaledLineWeight(appliedStyle.LineWeight - Style.ByLayer == 0 ? 1 : appliedStyle.LineWeight);
        var pen = new Pen(solidBrush, thickness);

        return pen;
    }

    /// <summary>
    /// 创建画刷
    /// </summary>
    private Brush CreateBrush(Style style)
    {
        var appliedStyle = StyleOverride ?? style;

        if (_styleBrushCacheDic.TryGetValue(appliedStyle.Color, out var brush)) return brush;
        var solidBrush = new SolidColorBrush(appliedStyle.Color.IsByLayer ? Colors.White : (System.Windows.Media.Color)appliedStyle.Color);
        _styleBrushCacheDic.Add(appliedStyle.Color, solidBrush);
        return solidBrush;
    }

    /// <summary>
    /// 创建字体
    /// </summary>
    private static Typeface CreateFont(TextStyle textStyle)
    {
        var (fontStyle, fontWeight) = textStyle.GetFontStyle();
        return new Typeface(new FontFamily(textStyle.FontFamily), fontStyle, fontWeight, FontStretches.Normal);
    }

    /// <summary>
    /// 测量文本大小
    /// </summary>
    /// <param name="text">文本</param>
    /// <param name="typeface">字体</param>
    /// <param name="fontBrush">字体颜色</param>
    /// <param name="fontSize">字体大小</param>
    /// <param name="maxWidth">最大宽度</param>
    /// <returns>文本向量</returns>
    // ReSharper disable once UnusedParameter.Global
    private static Vector2D MeasureString(string text, Typeface typeface, Brush fontBrush, double fontSize, double maxWidth)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        var ft = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, fontSize, fontBrush)
        {
            Trimming = TextTrimming.CharacterEllipsis
        };
#pragma warning restore CS0618 // Type or member is obsolete
        var width = ft.Width;
        ft.MaxTextWidth = maxWidth;
        var height = ft.Height;

        return new Vector2D(width, height);
    }

    /// <summary>
    /// 获取画笔宽度
    /// </summary>
    public double GetScaledLineWeight(double lineWeight)
    {
        return IsNotApplyScaleLineWeights
            ? lineWeight
            : View.ScreenToWorld(new Vector2D(lineWeight, 0)).X;
    }

    #endregion 创建绘画材料

    public void Dispose()
    {
        (_drawingContext as IDisposable)?.Dispose();
    }
}