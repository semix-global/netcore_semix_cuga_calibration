using CanvasViewer.Document;
using CanvasViewer.Drawables;
using CanvasViewer.Editor.Entity;
using CanvasViewer.Editor.Enum;
using CanvasViewer.Geometry;
using CanvasViewer.Helper;
using CanvasViewer.Media;
using CanvasViewer.Media.Drawing.Enum;
using CanvasViewer.Media.EventArg;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Cursor = System.Windows.Input.Cursor;
using Grid = CanvasViewer.Drawables.Grid;

namespace CanvasViewer.View;

public sealed class CanvasView : FrameworkElement, INotifyPropertyChanged, IDisposable
{
    static CanvasView()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CanvasView), new FrameworkPropertyMetadata(typeof(CanvasView)));
    }

    #region 常量

    /// <summary>
    /// 最小比例
    /// </summary>
    private const double MinZoom = 0.03f;

    /// <summary>
    /// 最大比例
    /// </summary>
    public const double MaxZoom = 100;

    #endregion 常量

    #region 属性

    private Point2D _cursorLocation;
    private bool _showAxes = true;
    private bool _showCursor = true;
    private bool _showGrid = true;

    /// <summary>
    /// 相机确定观看位置
    /// </summary>
    public Camera Camera { get; }

    /// <summary>
    /// 光标位置
    /// </summary>
    public Point2D CursorLocation
    {
        get => _cursorLocation;
        private set => SetField(ref _cursorLocation, value);
    }

    /// <summary>
    /// 文档
    /// </summary>
    public CanvasDocument Document { get; }

    /// <summary>
    /// 指示控件是否响应交互式用户输入
    /// </summary>

    [Category("Behavior")]
    [DefaultValue(true)]
    [Description("指示控件是否响应交互式用户输入")]
    public bool Interactive { get; set; } = true;

    /// <summary>
    /// 确定是否显示X和Y轴
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(true)]
    [Description("确定是否显示X和Y轴")]
    public bool ShowAxes
    {
        get => _showAxes;
        set
        {
            _showAxes = value;
            Redraw();
        }
    }

    /// <summary>
    /// 确定是否显示光标
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(true)]
    [Description("Determines whether the cursor is shown.")]
    public bool ShowCursor
    {
        get => _showCursor;
        set
        {
            _showCursor = value;
            Redraw();
        }
    }

    /// <summary>
    /// 确定是否显示笛卡尔网格
    /// </summary>
    [Category("Appearance")]
    [DefaultValue(true)]
    [Description("确定是否显示笛卡尔网格")]
    public bool ShowGrid
    {
        get => _showGrid;
        set
        {
            _showGrid = value;
            Redraw();
        }
    }

    /// <summary>
    /// 背景色
    /// </summary>
    [Category("Appearance")]
    [Description("Canvas背景色")]
    public Color BackgroundColor
    {
        get => (Color)Document.Settings.BackColor;
        set
        {
            Document.Settings.BackColor = new Media.Drawing.Color(value.A, value.R, value.G, value.B);
            Redraw();
        }
    }

    /// <summary>
    /// 轴颜色
    /// </summary>
    [Category("Appearance")]
    [Description("Axis背景色")]
    public Color AxisColor
    {
        get => (Color)Document.Settings.AxisColor;
        set
        {
            Document.Settings.AxisColor = new Media.Drawing.Color(value.A, value.R, value.G, value.B);
            Redraw();
        }
    }

    /// <summary>
    /// 绘画对象集合
    /// </summary>
    internal DrawableList VisibleItems { get; private set; } = [];

    /// <summary>
    /// 图像
    /// </summary>
    internal VisualCollection Visuals { get; }

    #region 依赖属性

    public static readonly DependencyProperty BackgroundImageProperty = DependencyProperty.Register(
        nameof(BackgroundImage),
        typeof(BitmapSource),
        typeof(CanvasView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBackgroundImageChanged)
    );

    /// <summary>
    /// 背景图片
    /// </summary>
    [Category("自定义")]
    [DefaultValue(typeof(BitmapImage), null!)]
    [Description("背景图片")]
    public BitmapSource? BackgroundImage
    {
        get => (BitmapSource?)GetValue(BackgroundImageProperty);
        set => SetValue(BackgroundImageProperty, value);
    }

    private static void OnBackgroundImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CanvasView control) return;

        control._backgroundDrawImage.Image = e.NewValue as BitmapSource;
        if (e.OldValue is null) control.SetViewport();
        else control.Redraw();
    }

    #endregion 依赖属性

    #endregion 属性

    #region 事件

    /// <summary>
    /// 光标点击事件
    /// </summary>
    public event CursorEventHandler? OnCursorClick;

    /// <summary>
    /// 光标Down事件
    /// </summary>
    public event CursorEventHandler? OnCursorDown;

    /// <summary>
    /// 光标Up事件
    /// </summary>
    public event CursorEventHandler? OnCursorUp;

    /// <summary>
    /// 光标Move事件
    /// </summary>
    public event CursorEventHandler? OnCursorMove;

    #endregion 事件

    #region 渲染

    /// <summary>
    /// 背景图片
    /// </summary>
    private readonly DrawImage _backgroundDrawImage = new();

    /// <summary>
    /// 坐标轴 绘画对象
    /// </summary>
    private readonly Axes _viewAxes = new();

    /// <summary>
    /// 光标 绘画对象
    /// </summary>
    private readonly Drawables.Cursor _viewCursor = new() { IsVisible = false };

    /// <summary>
    /// 网格 绘画对象
    /// </summary>
    private readonly Grid _viewGrid = new();

    /// <summary>
    /// 用户是否移动相机
    /// </summary>
    private bool _isMoveCamera;

    /// <summary>
    /// 光标上一次世界坐标
    /// </summary>
    private Point2D _lastMouseLocationWorld;

    /// <summary>
    /// 光标上一次样式
    /// </summary>
    private Cursor _lastCursor = Cursors.Cross;

    /// <summary>
    /// 鼠标按下选中的临时Drawable对象, 松开null
    /// </summary>
    private AbstractDrawable? _mouseDownDrawableItem;

    /// <summary>
    /// 鼠标按下选中的临时控制锚点的Drawable对象, 松开null
    /// </summary>
    private AbstractDrawable? _mouseDownControlPointDrawableItem;

    /// <summary>
    /// 鼠标按下选中的临时控制锚点, 松开null
    /// </summary>
    private ControlPoint? _mouseDownControlPoint;

    /// <summary>
    /// 鼠标按下激活控制锚点, 不会置为null
    /// </summary>
    private ControlPoint? _activeControlPoint;

    /// <summary>
    /// 渲染器
    /// </summary>
    private readonly Renderer _renderer;

    /// <summary>
    /// Viewport缓存
    /// </summary>
    private Extents2D? _viewportExtents2DCache;

    #endregion 渲染

    public CanvasView()
    {
        Visuals = new VisualCollection(this);
        Cursor = Cursors.Cross;

        Document = new CanvasDocument
        {
            ActiveView = this
        };

        // 设置为(0,0)时候, 笛卡尔坐标轴原点在控件屏幕中心
        Camera = new Camera(new Point2D(0, 0), 5.0f / 3.0f, RenderSize.Width, RenderSize.Height, (_, _) => _viewportExtents2DCache = null);
        _renderer = new Renderer(this);
        Redraw();

        SizeChanged += ViewOnResize;
        MouseDown += ViewOnMouseDown;
        MouseUp += ViewOnMouseUp;
        MouseMove += ViewMouseMove;
        MouseWheel += ViewOnMouseWheel;
        MouseEnter += ViewOnMouseEnter;
        MouseLeave += ViewOnMouseLeave;

        KeyDown += ViewOnKeyDown;

        Document.DocumentChanged += DocumentOnChanged;
        Document.TransientsChanged += DocumentOnTransientsChanged;
        Document.SelectionChanged += DocumentOnSelectionChanged;

        Document.Editor.Prompt += EditorOnPrompt;
        Document.Editor.Error += EditorOnError;
    }

    #region 事件

    private void ViewOnResize(object? sender, SizeChangedEventArgs e)
    {
        Resize(e.NewSize.Width, e.NewSize.Height);
    }

    private void ViewOnMouseDown(object? sender, MouseButtonEventArgs e)
    {
        var position = e.GetPosition(this); // 无需转换了, 因为获取的坐标是全局世界坐标
        // WorldToScreen(new Point2D(position)); // 转换为界面坐标
        var button = e.ChangedButton switch
        {
            MouseButton.Middle when e.MiddleButton == MouseButtonState.Pressed => MouseButtonsEnum.Middle,
            MouseButton.Left when e.LeftButton == MouseButtonState.Pressed => MouseButtonsEnum.Left,
            MouseButton.Right when e.RightButton == MouseButtonState.Pressed => MouseButtonsEnum.Right,
            MouseButton.XButton1 when e.XButton1 == MouseButtonState.Pressed => MouseButtonsEnum.XButton1,
            MouseButton.XButton2 when e.XButton2 == MouseButtonState.Pressed => MouseButtonsEnum.XButton2,
            _ => MouseButtonsEnum.None
        };
        var cursorEventArgs = new CursorEventArgs(button, new Point2D(position)) { Clicks = e.ClickCount };
        switch (e.ClickCount)
        {
            case 1:
                ViewOnCursorDown(cursorEventArgs);
                break;

            case 2:
                ViewOnCursorDoubleClick(cursorEventArgs);
                break;
        }
    }

    private void ViewOnMouseUp(object? sender, MouseButtonEventArgs e)
    {
        var position = e.GetPosition(this);
        var button = e.ChangedButton switch
        {
            MouseButton.Middle when e.MiddleButton == MouseButtonState.Released => MouseButtonsEnum.Middle,
            MouseButton.Left when e.LeftButton == MouseButtonState.Released => MouseButtonsEnum.Left,
            MouseButton.Right when e.RightButton == MouseButtonState.Released => MouseButtonsEnum.Right,
            MouseButton.XButton1 when e.XButton1 == MouseButtonState.Released => MouseButtonsEnum.XButton1,
            MouseButton.XButton2 when e.XButton2 == MouseButtonState.Released => MouseButtonsEnum.XButton2,
            _ => MouseButtonsEnum.None
        };
        ViewOnCursorUp(new CursorEventArgs(button, new Point2D(position)) { Clicks = e.ClickCount });
        if (e.ClickCount == 1) ViewOnCursorClick(new CursorEventArgs(button, new Point2D(position)) { Clicks = e.ClickCount });
    }

    private void ViewMouseMove(object? sender, MouseEventArgs e)
    {
        var position = e.GetPosition(this);
        var button = MouseButtonsEnum.None;
        if (e.MiddleButton == MouseButtonState.Pressed) button = MouseButtonsEnum.Middle;
        if (e.LeftButton == MouseButtonState.Pressed) button = MouseButtonsEnum.Left;
        if (e.RightButton == MouseButtonState.Pressed) button = MouseButtonsEnum.Right;
        if (e.XButton1 == MouseButtonState.Pressed) button = MouseButtonsEnum.XButton1;
        if (e.XButton2 == MouseButtonState.Pressed) button = MouseButtonsEnum.XButton2;
        ViewOnCursorMove(new CursorEventArgs(button, new Point2D(position)));
    }

    private void ViewOnMouseWheel(object? sender, MouseWheelEventArgs e)
    {
        var position = e.GetPosition(this);
        ViewOnCursorWheel(new CursorEventArgs(MouseButtonsEnum.None, new Point2D(position)) { Delta = e.Delta });

        e.Handled = true;
    }

    private void ViewOnKeyDown(object? sender, KeyEventArgs e)
    {
        if (Document.Editor.IsInputMode)
        {
            Document.Editor.OnViewKeyDown(this, e);
        }
        else if (e.Key == Key.Escape)
        {
            Document.Editor.PickedSelection.Clear();
            _viewCursor.Message = string.Empty;
        }
    }

    private void ViewOnMouseEnter(object? sender, MouseEventArgs e)
    {
        if (ShowCursor)
        {
            _viewCursor.IsVisible = true;
            _lastCursor = Cursor;
            Cursor = Cursors.None;
            Redraw();
        }

        if (ReferenceEquals(Document.ActiveView, this) == false) Document.ActiveView = this;
    }

    private void ViewOnMouseLeave(object? sender, MouseEventArgs e)
    {
        if (ShowCursor == false) return;

        _viewCursor.IsVisible = false;
        Cursor = _lastCursor;

        Redraw();
    }

    private void DocumentOnChanged(object sender, EventArgs e)
    {
        Redraw();
    }

    private void DocumentOnTransientsChanged(object sender, EventArgs e)
    {
        Redraw();
    }

    private void DocumentOnSelectionChanged(object sender, EventArgs e)
    {
        Redraw();
    }

    private void EditorOnPrompt(object sender, EditorPromptEventArgs e)
    {
        _viewCursor.Message = e.Status;
        Redraw();
    }

    private void EditorOnError(object sender, EditorErrorEventArgs e)
    {
        if (e.Error is null) return;
        _viewCursor.Message = e.Error.Message;
        Redraw();
    }

    #region Visual

    protected override int VisualChildrenCount => Visuals.Count;

    protected override Visual GetVisualChild(int index)
    {
        return Visuals[index];
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        Render();
    }

    #endregion Visual

    #region 事件 wrapper

    private void ViewOnCursorDown(CursorEventArgs e)
    {
        if (Interactive == false) return;

        switch (e.Button)
        {
            case MouseButtonsEnum.Middle:
                _isMoveCamera = true;
                _lastMouseLocationWorld = e.Location;
                break;

            case MouseButtonsEnum.Left when Document.Editor.IsInputMode == false:
                _mouseDownDrawableItem = FindDrawableItem(e.Location, ScreenToWorld(new Vector2D(Document.Settings.PickBoxSize, 0)).X);
                var (drawableItem, controlPoint) = FindControlPoint(e.Location, ScreenToWorld(new Vector2D(Document.Settings.ControlPointSize, 0)).X);
                _mouseDownControlPointDrawableItem = drawableItem;
                _mouseDownControlPoint = controlPoint;
                break;
        }

        OnCursorDown?.Invoke(this, e);
    }

    private void ViewOnCursorUp(CursorEventArgs e)
    {
        if (Interactive == false) return;

        switch (e.Button)
        {
            case MouseButtonsEnum.Middle when _isMoveCamera:
                _isMoveCamera = false;
                Redraw();
                break;

            case MouseButtonsEnum.Left when Document.Editor.IsInputMode == false:
                if (_mouseDownDrawableItem is not null)
                {
                    var mouseUpItem = FindDrawableItem(e.Location, ScreenToWorld(new Vector2D(Document.Settings.PickBoxSize, 0)).X);
                    if (mouseUpItem is not null && ReferenceEquals(_mouseDownDrawableItem, mouseUpItem))
                    {
                        var contains = Document.Editor.PickedSelection.Contains(_mouseDownDrawableItem);
                        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) // Control + 左键: 多选或者取消选择
                        {
                            if (contains)
                                Document.Editor.PickedSelection.Remove(_mouseDownDrawableItem);
                            else
                                Document.Editor.PickedSelection.Add(_mouseDownDrawableItem);
                        }
                        else
                        {
                            Document.Editor.PickedSelection.Clear();
                            Document.Editor.PickedSelection.Add(_mouseDownDrawableItem);
                        }
                    }
                }

                if (_mouseDownControlPointDrawableItem is not null && _mouseDownControlPoint is not null)
                {
                    var (drawableItem, controlPoint) = FindControlPoint(e.Location, ScreenToWorld(new Vector2D(Document.Settings.ControlPointSize, 0)).X);
                    if (ReferenceEquals(_mouseDownControlPointDrawableItem, drawableItem) && _mouseDownControlPoint.Index == controlPoint?.Index)
                        _activeControlPoint = _mouseDownControlPoint;
                }

                _mouseDownDrawableItem = null;
                _mouseDownControlPointDrawableItem = null;
                _mouseDownControlPoint = null;
                break;
        }

        OnCursorUp?.Invoke(this, e);
    }

    private void ViewOnCursorMove(CursorEventArgs e)
    {
        CursorLocation = e.Location;
        _viewCursor.Location = CursorLocation;
        if (ShowCursor) Redraw();

        if (e.Button == MouseButtonsEnum.Middle && _isMoveCamera)
        {
            // 鼠标相对移动
            var screenPoint = WorldToScreen(e.Location);
            // 指向_lastMouseLocationWorld的向量
            Pan(_lastMouseLocationWorld - CursorLocation);
            _lastMouseLocationWorld = ScreenToWorld(screenPoint);
            Redraw();
        }

        OnCursorMove?.Invoke(this, e);
    }

    private void ViewOnCursorClick(CursorEventArgs e)
    {
        if (Document.Editor.IsInputMode) Document.Editor.OnViewMouseClick(this, e);

        OnCursorClick?.Invoke(this, e);
    }

    private void ViewOnCursorDoubleClick(CursorEventArgs e)
    {
        if (e.Button == MouseButtonsEnum.Middle && Interactive) SetViewport();
    }

    private void ViewOnCursorWheel(CursorEventArgs e)
    {
        if (Interactive == false) return;

        if (e.Delta > 0) ZoomIn(e.Location); // 放大
        else ZoomOut(e.Location); // 缩小

        Redraw();
    }

    #endregion 事件 wrapper

    #endregion 事件

    #region 绘画

    /// <summary>
    /// 重新绘画
    /// </summary>
    public void Redraw()
    {
        InvalidateVisual();
    }

    public void Render()
    {
        // 开始绘画
        _renderer.InitFrame();
        _renderer.Clear(Document.Settings.BackColor);

        if (BackgroundImage is not null) _renderer.Draw(_backgroundDrawImage);

        // 网格和坐标轴
        if (ShowGrid && _viewGrid.IsVisible) _renderer.Draw(_viewGrid);
        if (ShowAxes && _viewAxes.IsVisible) _renderer.Draw(_viewAxes);

        // 渲染AbstractDrawable对象
        VisibleItems.Clear();
        _renderer.Draw(Document.Model); // 画文档里面所有的Drawable对象

        // 渲染所有选择的Drawable对象
        DrawSelection(_renderer);

        // 渲染用于显示抖动(用于编辑器显示抖动图形, 编辑的时候添加点可移动就是抖动的[比如polyline中后续的点就是抖动的点])
        DrawJigged(_renderer);

        // 画瞬时的图像, 表示移动旋转产生的瞬时对象
        _renderer.Draw(Document.Transients);

        // 渲染光标
        if (_showCursor && _viewCursor.IsVisible) _renderer.Draw(_viewCursor);

        // 渲染编辑时候捕捉的锚点
        DrawSnapPoint(_renderer);

        // 渲染到控件中
        _renderer.EndFrame();
    }

    public BitmapSource ToBitmapSource()
    {
        var renderTargetBitmap = new RenderTargetBitmap((int)RenderSize.Width, (int)RenderSize.Height, 96, 96, PixelFormats.Pbgra32);
        renderTargetBitmap.Render(this);
        var convertedSource = new FormatConvertedBitmap(renderTargetBitmap, PixelFormats.Bgr24, null, 0);
        return convertedSource;
    }

    private void DrawSelection(Renderer renderer)
    {
        // 临时高亮画选择的对象
        renderer.StyleOverride = new Media.Drawing.Style(Document.Settings.SelectionHighlightColor, 5);
        // 画选择的对象
        foreach (var selected in Document.Editor.PickedSelection) renderer.Draw(selected);
        // 清空临时绘画
        renderer.StyleOverride = null;

        // 画控制锚点
        var controlPointStyle = new Media.Drawing.Style(Document.Settings.ControlPointColor, 2);
        var controlPointActiveStyle = new Media.Drawing.Style(Document.Settings.ActiveControlPointColor, 2);
        var controlPointSize = ScreenToWorld(new Vector2D(Document.Settings.ControlPointSize, 0)).X;
        foreach (var selected in Document.Editor.PickedSelection)
        {
            foreach (var pt in selected.GetControlPoints())
            {
                renderer.DrawRectangle(pt.Equals(_activeControlPoint) ? controlPointActiveStyle : controlPointStyle,
                    new Point2D(pt.Location.X - controlPointSize / 2, pt.Location.Y - controlPointSize / 2),
                    new Point2D(pt.Location.X + controlPointSize / 2, pt.Location.Y + controlPointSize / 2));
            }
        }
    }

    private void DrawSnapPoint(Renderer renderer)
    {
        if (Document.Editor.SnapPoints.IsEmpty) return;

        var pt = Document.Editor.SnapPoints.Current();
        var style = new Media.Drawing.Style(Document.Settings.SnapPointColor, 2);
        var size = ScreenToWorld(new Vector2D(Document.Settings.SnapPointSize, 0)).X;

        switch (pt.Type)
        {
            case SnapPointTypeEnum.End:
                renderer.DrawRectangle(style,
                    new Point2D(pt.Location.X - size / 2, pt.Location.Y - size / 2),
                    new Point2D(pt.Location.X + size / 2, pt.Location.Y + size / 2));
                break;

            case SnapPointTypeEnum.Middle:
                renderer.DrawLine(style,
                    new Point2D(pt.Location.X - size / 2, pt.Location.Y - size / 2),
                    new Point2D(pt.Location.X + size / 2, pt.Location.Y - size / 2));
                renderer.DrawLine(style,
                    new Point2D(pt.Location.X + size / 2, pt.Location.Y - size / 2),
                    new Point2D(pt.Location.X, pt.Location.Y + size / 2));
                renderer.DrawLine(style,
                    new Point2D(pt.Location.X, pt.Location.Y + size / 2),
                    new Point2D(pt.Location.X - size / 2, pt.Location.Y - size / 2));
                break;

            case SnapPointTypeEnum.Point:
                renderer.DrawLine(style,
                    new Point2D(pt.Location.X - size / 2, pt.Location.Y - size / 2),
                    new Point2D(pt.Location.X + size / 2, pt.Location.Y + size / 2));
                renderer.DrawLine(style,
                    new Point2D(pt.Location.X - size / 2, pt.Location.Y + size / 2),
                    new Point2D(pt.Location.X + size / 2, pt.Location.Y - size / 2));
                renderer.DrawLine(style,
                    new Point2D(pt.Location.X, pt.Location.Y - size / 2),
                    new Point2D(pt.Location.X, pt.Location.Y + size / 2));
                renderer.DrawLine(style,
                    new Point2D(pt.Location.X - size / 2, pt.Location.Y),
                    new Point2D(pt.Location.X + size / 2, pt.Location.Y));
                break;

            case SnapPointTypeEnum.Center:
                renderer.DrawCircle(style, pt.Location, size / 2);
                break;

            case SnapPointTypeEnum.Quadrant:
                renderer.DrawLine(style,
                    new Point2D(pt.Location.X, pt.Location.Y - size / 2),
                    new Point2D(pt.Location.X + size / 2, pt.Location.Y));
                renderer.DrawLine(style,
                    new Point2D(pt.Location.X + size / 2, pt.Location.Y),
                    new Point2D(pt.Location.X, pt.Location.Y + size / 2));
                renderer.DrawLine(style,
                    new Point2D(pt.Location.X, pt.Location.Y + size / 2),
                    new Point2D(pt.Location.X - size / 2, pt.Location.Y));
                renderer.DrawLine(style,
                    new Point2D(pt.Location.X - size / 2, pt.Location.Y),
                    new Point2D(pt.Location.X, pt.Location.Y - size / 2));
                break;
        }
    }

    private void DrawJigged(Renderer renderer)
    {
        // 临时高亮画选择的对象
        renderer.StyleOverride = new Media.Drawing.Style(Document.Settings.JigColor, 0, DashStyleEnum.Dash);
        renderer.Draw(Document.Jigged);
        // 清空临时绘画
        renderer.StyleOverride = null;
    }

    #endregion 绘画

    #region 操作相机区域

    /// <summary>
    /// 移动相机画布视野
    /// </summary>
    /// <param name="distance">距离</param>
    public void Pan(Vector2D distance)
    {
        Camera.Position += distance;
    }

    /// <summary>
    /// 修改画布视野尺寸
    /// </summary>
    public void Resize(double width, double height)
    {
        Camera.Width = width;
        Camera.Height = height;
        SetViewport();
    }

    /// <summary>
    /// 将相机移动到区域中心, 并显示当前全部区域
    /// </summary>
    /// <param name="x1">模型坐标中视口左下角的 X 坐标</param>
    /// <param name="y1">模型坐标中视口左下角的 Y 坐标</param>
    /// <param name="x2">模型坐标中视口右上角的 X 坐标</param>
    /// <param name="y2">模型坐标中视口右上角的 Y 坐标</param>
    public void SetViewport(double x1, double y1, double x2, double y2)
    {
        SetViewport(new Extents2D(x1, y1, x2, y2));
    }

    /// <summary>
    /// 将相机移动到区域中心, 并显示当前全部区域
    /// </summary>
    /// <param name="p1">区域一角</param>
    /// <param name="p2">区域其他角</param>
    public void SetViewport(Point2D p1, Point2D p2)
    {
        SetViewport(new Extents2D(p1.X, p1.Y, p2.X, p2.Y));
    }

    /// <summary>
    /// 将相机移动到区域中心, 并显示当前全部区域
    /// </summary>
    /// <param name="limits">区域</param>
    public void SetViewport(Extents2D limits)
    {
        Camera.Position = limits.Center;
        if (Camera.Height != 0 && Camera.Width != 0)
            Camera.Zoom = Math.Max(limits.Height / Camera.Height, limits.Width / Camera.Width);
        else
            Camera.Zoom = 1;
        Redraw();
    }

    /// <summary>
    /// 将相机移动到区域中心, 并显示当前全部区域
    /// </summary>
    public void SetViewport()
    {
        var limits = Document.Model.GetExtents();
        if (limits.IsEmpty) limits = new Extents2D(-250, -250, 250, 250);
        if (BackgroundImage is not null)
        {
            var extents2D = _backgroundDrawImage.GetExtents();
            if (limits.Contains(extents2D)) limits = extents2D;
            limits.Add(extents2D);
        }

        SetViewport(limits);
        ZoomOut();
    }

    /// <summary>
    /// 相机中心缩放
    /// </summary>
    /// <param name="zoomFactor">缩放系数</param>
    public void Zoom(double zoomFactor)
    {
        Zoom(zoomFactor, Camera.Position);
    }

    /// <summary>
    /// 缩放
    /// </summary>
    /// <param name="zoomFactor">缩放系数</param>
    /// <param name="pt">缩放点</param>
    public void Zoom(double zoomFactor, Point2D pt)
    {
        if (zoomFactor < 1) // 放大
        {
            if (MathHelper.IsEqual(Camera.Zoom, MinZoom)) return;
            if (Camera.Zoom < MinZoom)
            {
                Camera.Zoom = MinZoom;
                return;
            }
        }

        if (zoomFactor > 1) // 缩小
        {
            if (MathHelper.IsEqual(Camera.Zoom, MaxZoom)) return;
            if (Camera.Zoom > MaxZoom)
            {
                Camera.Zoom = MaxZoom;
                return;
            }
        }

        Camera.Zoom *= zoomFactor;
        Camera.Position -= (pt - Camera.Position) * (zoomFactor - 1F);
    }

    /// <summary>
    /// 放大
    /// </summary>
    public void ZoomIn()
    {
        Zoom(0.9f);
    }

    /// <summary>
    /// 放大
    /// </summary>
    /// <param name="pt">缩放点</param>
    public void ZoomIn(Point2D pt)
    {
        Zoom(0.9f, pt);
    }

    /// <summary>
    /// 缩小
    /// </summary>
    public void ZoomOut()
    {
        Zoom(1.1f);
    }

    /// <summary>
    /// 缩小
    /// </summary>
    /// <param name="pt">缩放点</param>
    public void ZoomOut(Point2D pt)
    {
        Zoom(1.1f, pt);
    }

    #endregion 操作相机区域

    #region 坐标转换

    /// <summary>
    /// 获取相机区域(世界坐标)
    /// </summary>
    public Extents2D GetViewport()
    {
        if (_viewportExtents2DCache is not null) return _viewportExtents2DCache;

        _viewportExtents2DCache = new Extents2D();
        _viewportExtents2DCache.Add(ScreenToWorld(new Point2D(0, 0)));
        _viewportExtents2DCache.Add(ScreenToWorld(new Point2D(Camera.Width, Camera.Height)));
        return _viewportExtents2DCache;
    }

    /// <summary>
    /// 将屏幕坐标转换为世界坐标
    /// </summary>
    /// <param name="x">屏幕坐标中的 X 坐标</param>
    /// <param name="y">屏幕坐标中的 Y 坐标</param>
    /// <returns>世界坐标中的点</returns>
    public Point2D ScreenToWorld(double x, double y)
    {
        // 世界坐标(0,0)点在Canvas控件屏幕中心位置
        return new Point2D((x - Camera.Width / 2f) * Camera.Zoom + Camera.Position.X,
            -(y - Camera.Height / 2f) * Camera.Zoom + Camera.Position.Y);
    }

    /// <summary>
    /// 将屏幕矢量从屏幕坐标转换为世界矢量
    /// </summary>
    /// <param name="sz">屏幕矢量</param>
    /// <returns>世界矢量</returns>
    public Vector2D ScreenToWorld(Vector2D sz)
    {
        var pt1 = ScreenToWorld(0, 0);
        var pt2 = ScreenToWorld(sz.X, sz.Y);
        return pt2 - pt1;
    }

    /// <summary>
    /// 将给定点从世界坐标转换为屏幕坐标
    /// </summary>
    public Point2D ScreenToWorld(Point2D pt)
    {
        return ScreenToWorld(pt.X, pt.Y);
    }

    /// <summary>
    /// 将给定点从世界坐标转换为屏幕坐标
    /// </summary>
    /// <param name="pt">世界坐标</param>
    /// <returns>屏幕坐标</returns>
    public Point2D WorldToScreen(Point2D pt)
    {
        return WorldToScreen(pt.X, pt.Y);
    }

    /// <summary>
    /// 将给定点从世界坐标转换为屏幕坐标
    /// </summary>
    public Point2D WorldToScreen(double x, double y)
    {
        return new Point2D((x - Camera.Position.X) / Camera.Zoom + Camera.Width / 2f,
            -((y - Camera.Position.Y) / Camera.Zoom) + Camera.Height / 2f);
    }

    /// <summary>
    /// 将给定点从世界坐标转换为屏幕坐标
    /// </summary>
    public Vector2D WorldToScreen(Vector2D sz)
    {
        var pt1 = WorldToScreen(0.0f, 0.0f);
        var pt2 = WorldToScreen(sz.X, sz.Y);
        return pt2 - pt1;
    }

    #endregion 坐标转换

    #region 私有方法

    /// <summary>
    /// 在Drawable对象锚点范围内寻找Drawable对象和控制锚点
    /// </summary>
    /// <param name="pt">世界坐标点</param>
    /// <param name="controlPointSize">世界坐标范围</param>
    /// <returns>Drawable对象和控制锚点</returns>
    private (AbstractDrawable? Drawable, ControlPoint? ControlPoint) FindControlPoint(Point2D pt, double controlPointSize)
    {
        foreach (var item in Document.Editor.PickedSelection)
        {
            var i = 0;
            foreach (var controlPoint in item.GetControlPoints())
            {
                controlPoint.Index = i;
                i++;
                if (pt.X >= controlPoint.Location.X - controlPointSize / 2 && pt.X <= controlPoint.Location.X + controlPointSize / 2 &&
                    pt.Y >= controlPoint.Location.Y - controlPointSize / 2 && pt.Y <= controlPoint.Location.Y + controlPointSize / 2) // 鼠标在控制锚点范围内
                    return (item, controlPoint);
            }
        }

        return (null, null);
    }

    /// <summary>
    /// 在Drawable对象范围内寻找Drawable对象
    /// </summary>
    /// <param name="pt">世界坐标点</param>
    /// <param name="pickBox">世界坐标范围</param>
    /// <returns>Drawable对象</returns>
    private AbstractDrawable? FindDrawableItem(Point2D pt, double pickBox)
    {
        // 光标中心空方框大小转换到世界坐标大小
        // todo: 如果两个图形重叠选择就有问题
        return VisibleItems.FirstOrDefault(d => d.Contains(pt, pickBox));
    }

    #endregion 私有方法

    #region PropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // ReSharper disable once UnusedMethodReturnValue.Local
    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion PropertyChanged

    public void Dispose()
    {
        _renderer.Dispose();
    }
}