using CanvasViewer.Editor.Entity;
using CanvasViewer.Geometry;
using CanvasViewer.Media;
using CanvasViewer.Media.Drawing;

namespace CanvasViewer.Drawables;

public sealed class Rectangle : AbstractDrawable
{
    #region 属性

    private Extents2D _extents = Extents2D.Empty;
    private Color _extentsFillColor;

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
    public Color ExtentsFillColor
    {
        get => _extentsFillColor;
        set => SetField(ref _extentsFillColor, value);
    }

    #endregion 属性

    #region AbstractDrawable

    #region 绘画

    public override void Draw(Renderer renderer)
    {
        if (Extents.IsEmpty) return;

        renderer.FillRectangle(new Style(ExtentsFillColor).ApplyLayer(Layer), Extents.Point2DTopLeftCorner, Extents.Point2DBottomRightCorner);
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
            new ControlPoint(nameof(Extents2D.Point2DBottomRightCorner), Extents.Point2DBottomRightCorner)
        ];
    }

    public override SnapPoint[] GetSnapPoints()
    {
        return
        [
            new SnapPoint(nameof(Extents2D.Point2DTopLeftCorner), Extents.Point2DTopLeftCorner),
            new SnapPoint(nameof(Extents2D.Point2DTopRightCorner), Extents.Point2DTopRightCorner),
            new SnapPoint(nameof(Extents2D.Point2DBottomLeftCorner), Extents.Point2DBottomLeftCorner),
            new SnapPoint(nameof(Extents2D.Point2DBottomRightCorner), Extents.Point2DBottomRightCorner)
        ];
    }

    #endregion AbstractDrawable
}