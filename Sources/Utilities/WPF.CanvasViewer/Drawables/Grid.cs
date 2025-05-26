using CanvasViewer.Geometry;
using CanvasViewer.Media;
using CanvasViewer.Media.Drawing;

namespace CanvasViewer.Drawables;

/// <summary>
/// 网格
/// </summary>
public sealed class Grid : AbstractDrawable
{
    public override void Draw(Renderer renderer)
    {
        var view = renderer.View;

        double spacing = 1;

        // 动态网格间距
        while (view.WorldToScreen(new Vector2D(spacing, 0)).X > 12) spacing /= 10;
        while (view.WorldToScreen(new Vector2D(spacing, 0)).X < 4) spacing *= 10;

        var bounds = view.GetViewport(); // 相机区域(世界坐标)
        var majorStyle = new Style(view.Document.Settings.MajorGridColor); // 主要样式
        var minorStyle = new Style(view.Document.Settings.MinorGridColor); // 次要样式

        var k = 0;
        for (double i = 0; i > bounds.XMin; i -= spacing)
        {
            var style = k == 0 ? majorStyle : minorStyle;
            k = (k + 1) % 10;
            renderer.DrawLine(style, new Point2D(i, bounds.YMax), new Point2D(i, bounds.YMin));
        }

        k = 0;
        for (double i = 0; i < bounds.XMax; i += spacing)
        {
            var style = k == 0 ? majorStyle : minorStyle;
            k = (k + 1) % 10;
            renderer.DrawLine(style, new Point2D(i, bounds.YMax), new Point2D(i, bounds.YMin));
        }

        k = 0;
        for (double i = 0; i < bounds.YMax; i += spacing)
        {
            var style = k == 0 ? majorStyle : minorStyle;
            k = (k + 1) % 10;
            renderer.DrawLine(style, new Point2D(bounds.XMin, i), new Point2D(bounds.XMax, i));
        }

        k = 0;
        for (double i = 0; i > bounds.YMin; i -= spacing)
        {
            var style = k == 0 ? majorStyle : minorStyle;
            k = (k + 1) % 10;
            renderer.DrawLine(style, new Point2D(bounds.XMin, i), new Point2D(bounds.XMax, i));
        }
    }

    public override Extents2D GetExtents()
    {
        return Extents2D.Infinity;
    }

    public override void TransformBy(Matrix2D transformation)
    {
    }
}