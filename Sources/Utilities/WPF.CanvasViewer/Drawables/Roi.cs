using CanvasViewer.Editor.Entity;
using CanvasViewer.Geometry;
using CanvasViewer.Media;
using CanvasViewer.Media.Drawing;
using Style = CanvasViewer.Media.Drawing.Style;

namespace CanvasViewer.Drawables;

public sealed class Roi : AbstractDrawable
{
    #region 属性

    private Extents2D _extents = Extents2D.Empty;
    private Color _extentsDrawColor;

    /// <summary>
    /// 区域
    /// </summary>
    public Extents2D Extents
    {
        get => _extents;
        set => SetField(ref _extents, value);
    }

    /// <summary>
    /// 填充颜色
    /// </summary>
    public Color ExtentsDrawColor
    {
        get => _extentsDrawColor;
        set => SetField(ref _extentsDrawColor, value);
    }

    #endregion 属性

    #region AbstractDrawable

    #region 绘画

    public override void Draw(Renderer renderer)
    {
        if (Extents.IsEmpty) return;

        renderer.DrawRectangle(new Style(ExtentsDrawColor).ApplyLayer(Layer), Extents.Point2DTopLeftCorner, Extents.Point2DBottomRightCorner);
        renderer.DrawLine(new Style(ExtentsDrawColor).ApplyLayer(Layer), Extents.Point2DTopCenter, Extents.Point2DBottomCenter);
        renderer.DrawLine(new Style(ExtentsDrawColor).ApplyLayer(Layer), Extents.Point2DLeftCenter, Extents.Point2DRightCenter);
    }

    public override Extents2D GetExtents() => Extents;

    public override void TransformBy(Matrix2D transformation)
    {
        Extents.TransformBy(transformation);
    }

    #endregion 绘画

    public override ControlPoint[] GetControlPoints()
    {
        return
        [
            new ControlPoint(nameof(Extents2D.Point2DTopLeftCorner), Extents.Point2DTopLeftCorner),
            new ControlPoint(nameof(Extents2D.Point2DTopRightCorner), Extents.Point2DTopRightCorner),
            new ControlPoint(nameof(Extents2D.Point2DBottomLeftCorner), Extents.Point2DBottomLeftCorner),
            new ControlPoint(nameof(Extents2D.Point2DBottomRightCorner), Extents.Point2DBottomRightCorner),
            new ControlPoint(nameof(Extents2D.Point2DTopCenter), Extents.Point2DTopCenter),
            new ControlPoint(nameof(Extents2D.Point2DBottomCenter), Extents.Point2DBottomCenter),
            new ControlPoint(nameof(Extents2D.Point2DLeftCenter), Extents.Point2DLeftCenter),
            new ControlPoint(nameof(Extents2D.Point2DRightCenter), Extents.Point2DRightCenter)
        ];
    }

    public override SnapPoint[] GetSnapPoints()
    {
        return
        [
            new SnapPoint(nameof(Extents2D.Point2DTopLeftCorner), Extents.Point2DTopLeftCorner),
            new SnapPoint(nameof(Extents2D.Point2DTopRightCorner), Extents.Point2DTopRightCorner),
            new SnapPoint(nameof(Extents2D.Point2DBottomLeftCorner), Extents.Point2DBottomLeftCorner),
            new SnapPoint(nameof(Extents2D.Point2DBottomRightCorner), Extents.Point2DBottomRightCorner),
            new SnapPoint(nameof(Extents2D.Point2DTopCenter), Extents.Point2DTopCenter),
            new SnapPoint(nameof(Extents2D.Point2DBottomCenter), Extents.Point2DBottomCenter),
            new SnapPoint(nameof(Extents2D.Point2DLeftCenter), Extents.Point2DLeftCenter),
            new SnapPoint(nameof(Extents2D.Point2DRightCenter), Extents.Point2DRightCenter)
        ];
    }

    #endregion AbstractDrawable
}