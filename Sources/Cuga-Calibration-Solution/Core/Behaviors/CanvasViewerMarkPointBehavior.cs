using CanvasViewer.Drawables;
using CanvasViewer.Geometry;
using CanvasViewer.Media;
using CanvasViewer.Media.Drawing;
using CanvasViewer.Media.Drawing.Enum;
using CanvasViewer.Media.EventArg;
using CanvasViewer.View;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Cursor = System.Windows.Input.Cursor;
using Point = Net.Utilities.Models.Geometries.Point;
using Style = CanvasViewer.Media.Drawing.Style;

namespace CugaCalibration.Core.Behaviors;

/// <summary>
/// 45° 十字架绘制对象（屏幕等边长，与红色 ROI 区分用黄色；中心圆形范围拾取）
/// </summary>
internal sealed class Cross45Drawable : AbstractDrawable
{
    /// <summary>
    /// 十字架屏幕边长（像素，不随缩放变化）
    /// </summary>
    public double ScreenSize { get; set; } = 40d;

    private Point2D _location;

    private bool _isSelected;

    /// <summary>
    /// 十字架中心（世界坐标）
    /// </summary>
    public Point2D Location
    {
        get => _location;
        set => SetField(ref _location, value);
    }

    /// <summary>
    /// 选中态（拖动中）：选中时蓝色，焦点离开（松开）恢复黄色
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public override void Draw(Renderer renderer)
    {
        var half = renderer.View.ScreenToWorld(new Vector2D(ScreenSize, 0)).X / 2d;
        var style = new Style(IsSelected ? Color.DodgerBlue : Color.Yellow).ApplyLayer(Layer);

        // 两条 45° 对角线
        renderer.DrawLine(style, new Point2D(Location.X - half, Location.Y - half), new Point2D(Location.X + half, Location.Y + half));
        renderer.DrawLine(style, new Point2D(Location.X - half, Location.Y + half), new Point2D(Location.X + half, Location.Y - half));
    }

    public override Extents2D GetExtents()
    {
        var extents = new Extents2D();
        extents.Add(Location.X, Location.Y);
        return extents;
    }

    public override void TransformBy(Matrix2D transformation) => Location = Location.Transform(transformation);

    public override bool Contains(Point2D pt, double pickBoxSize) => (pt - Location).Length <= pickBoxSize;
}

/// <summary>
/// 45° 十字架标记行为：Point 绑定原图像素坐标（左上原点，Y 向下），null 时隐藏；显示后可在画布上拖动，拖动实时回写
/// </summary>
/// <remarks>
/// 必须置于 CanvasViewerRoiBehavior 之前：事件按订阅顺序执行，本行为命中十字架时先清除拾取集合，避免点击十字架（位于 ROI 内）时 ROI 同时进入移动状态。
/// 十字架显示期间（Point 非 null，即 Step 2）ROI 锁定不可编辑：RoiBehavior 的移动/缩放均以 ROI 被选中为前提，本行为在每次光标抬起时清空拾取集合使选中永不成立。
/// ShowRoi 为 false 时从画布移除 RoiBehavior 的 ROI 绘制对象（首次打开不显示），为 true 时恢复显示
/// </remarks>
public sealed class CanvasViewerMarkPointBehavior : Behavior<CanvasView>
{
    public static readonly DependencyProperty PointProperty = DependencyProperty.Register(
        nameof(Point), typeof(Point?), typeof(CanvasViewerMarkPointBehavior),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPointChanged));

    public Point? Point
    {
        get => (Point?)GetValue(PointProperty);
        set => SetValue(PointProperty, value);
    }

    public static readonly DependencyProperty ShowRoiProperty = DependencyProperty.Register(
        nameof(ShowRoi), typeof(bool), typeof(CanvasViewerMarkPointBehavior),
        new PropertyMetadata(false, OnShowRoiChanged));

    /// <summary>
    /// 是否显示 RoiBehavior 的 ROI（false = 从画布移除隐藏，true = 恢复显示）
    /// </summary>
    public bool ShowRoi
    {
        get => (bool)GetValue(ShowRoiProperty);
        set => SetValue(ShowRoiProperty, value);
    }

    /// <summary>
    /// 隐藏期间暂存的 ROI 绘制对象（恢复显示时重新加入模型）
    /// </summary>
    private Roi? _hiddenRoi;

    private static void OnShowRoiChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CanvasViewerMarkPointBehavior behavior) return;
        behavior.UpdateRoiVisibility();
    }

    private readonly Cross45Drawable _cross = new();

    private bool _isDragging;

    private Cursor? _defaultCursor;

    private static void OnPointChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CanvasViewerMarkPointBehavior behavior) return;
        behavior.UpdateDrawable();
    }

    protected override void OnAttached()
    {
        base.OnAttached();

        _defaultCursor ??= AssociatedObject.Cursor;

        AssociatedObject.OnCursorDown -= OnCursorDown;
        AssociatedObject.OnCursorDown += OnCursorDown;
        AssociatedObject.OnCursorUp -= OnCursorUp;
        AssociatedObject.OnCursorUp += OnCursorUp;
        AssociatedObject.OnCursorMove -= OnCursorMove;
        AssociatedObject.OnCursorMove += OnCursorMove;

        // 延迟到全部行为附加完成后执行初始 ROI 隐藏（本行为先于 RoiBehavior 附加，此刻 ROI 尚未加入模型）
        AssociatedObject.Dispatcher.BeginInvoke((Action)UpdateRoiVisibility);

        UpdateDrawable();
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        if (AssociatedObject.Document.Model.Contains(_cross)) AssociatedObject.Document.Model.Remove(_cross);
        AssociatedObject.OnCursorDown -= OnCursorDown;
        AssociatedObject.OnCursorUp -= OnCursorUp;
        AssociatedObject.OnCursorMove -= OnCursorMove;
    }

    private void OnCursorDown(object sender, CursorEventArgs e)
    {
        if (e.Button != MouseButtonsEnum.Left) return;
        if (AssociatedObject.Document.Model.Contains(_cross) == false) return;

        // 拾取半径：不小于十字架半臂长且至少为拾取框 3 倍（扩大可聚焦范围）
        var pickRadiusScreen = Math.Max(_cross.ScreenSize / 2d, AssociatedObject.Document.Settings.PickBoxSize * 3d);
        var pickBoxSize = AssociatedObject.ScreenToWorld(new Vector2D(pickRadiusScreen, 0)).X;
        if (_cross.Contains(e.Location, pickBoxSize) == false) return;

        // 命中十字架：清除拾取集合（先于 RoiBehavior 的处理器执行），避免 ROI 跟随拖动
        AssociatedObject.Document.Editor.PickedSelection.Clear();
        _isDragging = true;
        _cross.IsSelected = true;
        AssociatedObject.Cursor = Cursors.SizeAll;
    }

    private void OnCursorUp(object sender, CursorEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            _cross.IsSelected = false;
            if (_defaultCursor is not null) AssociatedObject.Cursor = _defaultCursor;
            UpdateSource();
        }

        // 十字架显示期间锁定 ROI 编辑：清除拾取集合（含本次光标抬起刚建立的选中），RoiBehavior 的移动/缩放入口即被阻断
        if (AssociatedObject.Document.Model.Contains(_cross)) AssociatedObject.Document.Editor.PickedSelection.Clear();
    }

    private void OnCursorMove(object sender, CursorEventArgs e)
    {
        if (_isDragging == false) return;

        _cross.Location = e.Location;
        UpdateSource();
    }

    private void UpdateRoiVisibility()
    {
        if (AssociatedObject is null) return;

        var model = AssociatedObject.Document.Model;

        if (ShowRoi)
        {
            if (_hiddenRoi is not null && model.Contains(_hiddenRoi) == false) model.Add(_hiddenRoi);
            return;
        }

        if (model.OfType<Roi>().FirstOrDefault() is not { } roi) return;

        _hiddenRoi = roi;
        model.Remove(roi);
        AssociatedObject.Document.Editor.PickedSelection.Clear();
    }

    private void UpdateDrawable()
    {
        if (AssociatedObject is null) return;

        if (Point is not { } point || AssociatedObject.BackgroundImage is null)
        {
            if (AssociatedObject.Document.Model.Contains(_cross)) AssociatedObject.Document.Model.Remove(_cross);
            return;
        }

        var image = AssociatedObject.BackgroundImage;

        // 原图像素坐标（左上原点，Y 向下）→ 世界坐标（图像中心原点，Y 向上），同 CanvasViewerRoiBehavior 口径
        _cross.Location = new Point2D(point.X - image.PixelWidth / 2d, image.PixelHeight / 2d - point.Y);

        // 追加到模型列表尾部：绘制在 ROI 之上；拾取不依赖 FindDrawableItem（OnCursorDown 直接命中测试）
        if (AssociatedObject.Document.Model.Contains(_cross) == false)
        {
            AssociatedObject.Document.Model.Add(_cross);

            // 进入锁定态：清除已有选中（Step 1 期间可能已选中 ROI）
            AssociatedObject.Document.Editor.PickedSelection.Clear();
        }
    }

    private void UpdateSource()
    {
        if (AssociatedObject.BackgroundImage is null) return;

        var image = AssociatedObject.BackgroundImage;

        // 世界坐标 → 原图像素坐标
        Point = new Point(_cross.Location.X + image.PixelWidth / 2d, image.PixelHeight / 2d - _cross.Location.Y);
        BindingOperations.GetBindingExpression(this, PointProperty)?.UpdateSource();
    }
}
