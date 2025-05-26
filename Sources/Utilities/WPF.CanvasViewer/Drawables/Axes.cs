using CanvasViewer.Geometry;
using CanvasViewer.Media;
using CanvasViewer.Media.Drawing;

namespace CanvasViewer.Drawables;

public sealed class Axes : AbstractDrawable
{
    public override void Draw(Renderer renderer)
    {
        var view = renderer.View;

        var bounds = view.GetViewport(); // 相机区域(世界坐标)
        var axisColor = view.Document.Settings.AxisColor;

        renderer.DrawLine(new Style(axisColor), new Point2D(0, bounds.YMin), new Point2D(0, bounds.YMax));
        renderer.DrawLine(new Style(axisColor), new Point2D(bounds.XMin, 0), new Point2D(bounds.XMax, 0));
    }

    public override Extents2D GetExtents()
    {
        return Extents2D.Empty;
    }

    public override void TransformBy(Matrix2D transformation)
    {
    }
}