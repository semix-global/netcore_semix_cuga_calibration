using CanvasViewer.Drawables;
using CanvasViewer.Geometry;
using CanvasViewer.Media.Drawing;
using CanvasViewer.Media.Drawing.Enum;
using CanvasViewer.Media.EventArg;
using CanvasViewer.View;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Cursor = System.Windows.Input.Cursor;
using Point = Net.Utilities.Models.Point;
using Rect = Net.Utilities.Models.Rect;

namespace CanvasViewer.Behaviors;

public sealed class CanvasViewerRoiBehavior : Behavior<CanvasView>
{
    public static readonly DependencyProperty RectProperty = DependencyProperty.Register(
        nameof(Rect),
        typeof(Rect),
        typeof(CanvasViewerRoiBehavior),
        new FrameworkPropertyMetadata(Rect.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPropertyChangedCallback)
    );

    public Rect Rect
    {
        get => (Rect)GetValue(RectProperty);
        set => SetValue(RectProperty, value);
    }

    private static void OnPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not CanvasViewerRoiBehavior behavior) return;
        var rect = (Rect)e.NewValue;

        if (behavior.AssociatedObject.BackgroundImage is not null)
        {
            // 1. 笛卡尔坐标系坐标: 获取左上角顶点
            var pointLeftTopCorner = new Point(rect.X, rect.Y + rect.Height);

            // 2. 笛卡尔坐标系: 平移到图片的左上角顶点
            var pointLeftTopCornerTranslate = new Point(pointLeftTopCorner.X - behavior.AssociatedObject.BackgroundImage.PixelWidth / 2d, pointLeftTopCorner.Y + behavior.AssociatedObject.BackgroundImage.PixelHeight / 2d);

            // 3. 笛卡尔坐标系: 关于 y = behavior.AssociatedObject.BackgroundImage.PixelHeight / 2d 轴对称的点 =>获取(左下角)
            rect.Point = new Point(pointLeftTopCornerTranslate.X, 2d * behavior.AssociatedObject.BackgroundImage.PixelHeight / 2d - pointLeftTopCornerTranslate.Y);
        }

        behavior._roi.Extents = new Extents2D(rect.X, rect.Y, rect.X + rect.Width, rect.Y + rect.Height);
    }

    /// <summary>
    /// ROI对象
    /// </summary>
    private readonly Roi _roi = new() { Extents = new Extents2D(-500, -500, +500, +500), ExtentsDrawColor = Color.Red };

    /// <summary>
    /// 默认光斑样式
    /// </summary>
    private Cursor? _defaultCursor;

    /// <summary>
    /// 鼠标最后一次位置
    /// </summary>
    private Point2D _lastMouseLocationWorld;

    /// <summary>
    /// 移动状态和调整大小状态
    /// </summary>
    private EditorState _editorState;

    /// <summary>
    /// 拖动柄状态
    /// </summary>
    private TransformState _transformState;

    protected override void OnAttached()
    {
        base.OnAttached();

        _defaultCursor ??= AssociatedObject.Cursor;

        AssociatedObject.Document.Model.Add(_roi);
        AssociatedObject.OnCursorDown -= OnCursorDown;
        AssociatedObject.OnCursorDown += OnCursorDown;
        AssociatedObject.OnCursorUp -= OnCursorUp;
        AssociatedObject.OnCursorUp += OnCursorUp;
        AssociatedObject.OnCursorMove -= OnCursorMove;
        AssociatedObject.OnCursorMove += OnCursorMove;

        UpdateSource();
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        AssociatedObject.Document.Model.Remove(_roi);
        AssociatedObject.OnCursorDown -= OnCursorDown;
        AssociatedObject.OnCursorUp -= OnCursorUp;
        AssociatedObject.OnCursorMove -= OnCursorMove;
    }

    private void OnCursorDown(object sender, CursorEventArgs e)
    {
        if (e.Button != MouseButtonsEnum.Left) return;
        if (_roi.Extents.Width == 0d || _roi.Extents.Height == 0d) return;
        if (AssociatedObject.Document.Editor.PickedSelection.Contains(_roi) == false) return;

        _lastMouseLocationWorld = e.Location;

        var controlPointSize = AssociatedObject.ScreenToWorld(new Vector2D(AssociatedObject.Document.Settings.ControlPointSize, 0)).X;
        var controlPoint = _roi.GetControlPoints()
            .Select(t => (ControlPoint: t, Contains: (t.BasePoint - e.Location).Length <= controlPointSize))
            .Where(t => t.Contains)
            .Select(t => t.ControlPoint)
            .FirstOrDefault();
        if (_roi.Extents.Contains(e.Location) == false && controlPoint is null)
        {
            AssociatedObject.Document.ClearSelect();
            return;
        }

        switch (controlPoint?.Name)
        {
            case nameof(Extents2D.Point2DTopLeftCorner):
                _editorState = EditorState.Transform;
                _transformState = TransformState.LeftTop;
                AssociatedObject.Cursor = Cursors.SizeNWSE;
                break;

            case nameof(Extents2D.Point2DTopRightCorner):
                _editorState = EditorState.Transform;
                _transformState = TransformState.RightTop;
                AssociatedObject.Cursor = Cursors.SizeNESW;
                break;

            case nameof(Extents2D.Point2DBottomRightCorner):
                _editorState = EditorState.Transform;
                _transformState = TransformState.RightBottom;
                AssociatedObject.Cursor = Cursors.SizeNWSE;
                break;

            case nameof(Extents2D.Point2DBottomLeftCorner):
                _editorState = EditorState.Transform;
                _transformState = TransformState.LeftBottom;
                AssociatedObject.Cursor = Cursors.SizeNESW;
                break;

            case nameof(Extents2D.Point2DTopCenter):
                _editorState = EditorState.Transform;
                _transformState = TransformState.Top;
                AssociatedObject.Cursor = Cursors.SizeNS;
                break;

            case nameof(Extents2D.Point2DBottomCenter):
                _editorState = EditorState.Transform;
                _transformState = TransformState.Bottom;
                AssociatedObject.Cursor = Cursors.SizeNS;
                break;

            case nameof(Extents2D.Point2DLeftCenter):
                _editorState = EditorState.Transform;
                _transformState = TransformState.Left;
                AssociatedObject.Cursor = Cursors.SizeWE;
                break;

            case nameof(Extents2D.Point2DRightCenter):
                _editorState = EditorState.Transform;
                _transformState = TransformState.Right;
                AssociatedObject.Cursor = Cursors.SizeWE;
                break;

            case null:
                _editorState = EditorState.Move;
                AssociatedObject.Cursor = Cursors.SizeAll;
                break;
        }
    }

    private void OnCursorUp(object sender, CursorEventArgs e)
    {
        if (_defaultCursor is not null) AssociatedObject.Cursor = _defaultCursor;
        _editorState = EditorState.None;
        _transformState = TransformState.None;
    }

    private void OnCursorMove(object sender, CursorEventArgs e)
    {
        if (_editorState is EditorState.None) return;

        var xMove = e.Location.X - _lastMouseLocationWorld.X;
        var yMove = e.Location.Y - _lastMouseLocationWorld.Y;
        _lastMouseLocationWorld = e.Location;
        Extents2D extents2D;
        switch (_editorState)
        {
            case EditorState.Move: //移动模式，设定对象位置
                extents2D = new Extents2D();
                extents2D.Add(_roi.Extents.XMin + xMove, _roi.Extents.YMin + yMove);
                extents2D.Add(_roi.Extents.XMax + xMove, _roi.Extents.YMax + yMove);
                _roi.Extents = extents2D;
                break;

            case EditorState.Transform: //调整大小
                switch (_transformState)
                {
                    case TransformState.LeftTop:
                        extents2D = new Extents2D();
                        extents2D.Add(_roi.Extents.XMin + xMove, _roi.Extents.YMin);
                        extents2D.Add(_roi.Extents.XMax, _roi.Extents.YMax + yMove);
                        if (extents2D.Width < 32 || extents2D.Height < 32) return;

                        _roi.Extents = extents2D;
                        break;

                    case TransformState.Top:
                        extents2D = new Extents2D();
                        extents2D.Add(_roi.Extents.XMax, _roi.Extents.YMax + yMove);
                        extents2D.Add(_roi.Extents.XMin, _roi.Extents.YMin);
                        if (extents2D.Width < 32 || extents2D.Height < 32) return;

                        _roi.Extents = extents2D;
                        break;

                    case TransformState.RightTop:
                        extents2D = new Extents2D();
                        extents2D.Add(_roi.Extents.XMax + xMove, _roi.Extents.YMax + yMove);
                        extents2D.Add(_roi.Extents.XMin, _roi.Extents.YMin);
                        if (extents2D.Width < 32 || extents2D.Height < 32) return;

                        _roi.Extents = extents2D;

                        break;

                    case TransformState.Right:
                        extents2D = new Extents2D();
                        extents2D.Add(_roi.Extents.XMax + xMove, _roi.Extents.YMax);
                        extents2D.Add(_roi.Extents.XMin, _roi.Extents.YMin);
                        if (extents2D.Width < 32 || extents2D.Height < 32) return;

                        _roi.Extents = extents2D;
                        break;

                    case TransformState.RightBottom:
                        extents2D = new Extents2D();
                        extents2D.Add(_roi.Extents.XMax + xMove, _roi.Extents.YMax);
                        extents2D.Add(_roi.Extents.XMin, _roi.Extents.YMin + yMove);
                        if (extents2D.Width < 32 || extents2D.Height < 32) return;

                        _roi.Extents = extents2D;
                        break;

                    case TransformState.Bottom:
                        extents2D = new Extents2D();
                        extents2D.Add(_roi.Extents.XMax, _roi.Extents.YMax);
                        extents2D.Add(_roi.Extents.XMin, _roi.Extents.YMin + yMove);
                        if (extents2D.Width < 32 || extents2D.Height < 32) return;

                        _roi.Extents = extents2D;
                        break;

                    case TransformState.LeftBottom:
                        extents2D = new Extents2D();
                        extents2D.Add(_roi.Extents.XMax, _roi.Extents.YMax);
                        extents2D.Add(_roi.Extents.XMin + xMove, _roi.Extents.YMin + yMove);
                        if (extents2D.Width < 32 || extents2D.Height < 32) return;

                        _roi.Extents = extents2D;
                        break;

                    case TransformState.Left:
                        extents2D = new Extents2D();
                        extents2D.Add(_roi.Extents.XMax, _roi.Extents.YMax);
                        extents2D.Add(_roi.Extents.XMin + xMove, _roi.Extents.YMin);
                        if (extents2D.Width < 32 || extents2D.Height < 32) return;

                        _roi.Extents = extents2D;
                        break;
                }

                break;
        }

        UpdateSource();
    }

    private void UpdateSource()
    {
        var rect = new Rect(_roi.Extents.XMin, _roi.Extents.YMin, _roi.Extents.XMax - _roi.Extents.XMin, _roi.Extents.YMax - _roi.Extents.YMin);

        if (AssociatedObject.BackgroundImage is not null)
        {
            // 1. 笛卡尔坐标系(左下角): 关于 y轴对称, 为Gdi+坐标系
            var point = new Point(rect.X, -rect.Y);

            // 2. Gdi+坐标系(左下角): 获取Gdi+坐标系(左下角)顶点
            var pointLeftTopCorner = new Point(point.X, point.Y - rect.Height);

            // 3. 笛卡尔坐标系: 平移
            rect.Point = new Point(pointLeftTopCorner.X + AssociatedObject.BackgroundImage.PixelWidth / 2d, pointLeftTopCorner.Y + AssociatedObject.BackgroundImage.PixelHeight / 2d);
        }

        Rect = rect;
        BindingOperations.GetBindingExpression(this, RectProperty)!.UpdateSource();
    }

    private enum EditorState
    {
        /// <summary>
        /// 没有任何操作
        /// </summary>
        None,

        /// <summary>
        /// 移动状态
        /// </summary>
        Move,

        /// <summary>
        /// 调整大小状态
        /// </summary>
        Transform
    }

    private enum TransformState
    {
        /// <summary>
        /// 没有任何操作
        /// </summary>
        None,

        /// <summary>
        /// 鼠标在操纵柄左上角
        /// </summary>
        LeftTop,

        /// <summary>
        /// 鼠标在操纵柄上边
        /// </summary>
        Top,

        /// <summary>
        /// 鼠标在操纵柄右上角
        /// </summary>
        RightTop,

        /// <summary>
        /// 鼠标在操纵柄右边
        /// </summary>
        Right,

        /// <summary>
        /// 鼠标在操纵柄右下角
        /// </summary>
        RightBottom,

        /// <summary>
        /// 鼠标在操纵柄下边
        /// </summary>
        Bottom,

        /// <summary>
        /// 鼠标在操纵柄左下角
        /// </summary>
        LeftBottom,

        /// <summary>
        /// 鼠标在操纵柄左边
        /// </summary>
        Left
    }
}