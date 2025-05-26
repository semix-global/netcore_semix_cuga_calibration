using CanvasViewer.Geometry;
using CanvasViewer.Media;
using CanvasViewer.Media.Drawing;
using CanvasViewer.Media.Drawing.Enum;

namespace CanvasViewer.Drawables;

public sealed class Fps : AbstractDrawable
{
    private readonly TextStyle _textStyle = new("_Cursor", "Microsoft YaHei UI", FontStyleEnum.Regular);
    private double _fpsValue;

    public double FpsValue
    {
        get => _fpsValue;
        set => SetField(ref _fpsValue, value);
    }

    public override void Draw(Renderer renderer)
    {
        var view = renderer.View;
        var viewExtents2D = view.GetViewport(); // 相机区域(世界坐标)

        var height = Math.Abs(view.ScreenToWorld(new Vector2D(0, 12)).Y); // 文本高度
        var margin = Math.Abs(view.ScreenToWorld(new Vector2D(4, 0)).X); // 文本边距margin
        var offset = Math.Abs(view.ScreenToWorld(new Vector2D(2, 0)).X); // 文本偏移
        var foreStyle = new Style(view.Document.Settings.CursorPromptForeColor); // 前景样式
        var backStyle = new Style(view.Document.Settings.CursorPromptBackColor); // 背景样式

        // 画坐标 控件右下角
        var locationStr = $"FPS: {FpsValue:f3}";
        var locationX = viewExtents2D.XMin + margin + offset; // 文本位置x
        var locationY = viewExtents2D.YMax - margin - offset; // 文本位置y
        var locationVector2D = renderer.MeasureString(locationStr, foreStyle, _textStyle, height); // 获取文本大小
        renderer.FillRectangle(backStyle, new Point2D(locationX - offset, locationY + offset), new Point2D(locationX + offset + locationVector2D.X, locationY - offset - locationVector2D.Y));
        renderer.DrawRectangle(foreStyle, new Point2D(locationX - offset, locationY + offset), new Point2D(locationX + offset + locationVector2D.X, locationY - offset - locationVector2D.Y));
        renderer.DrawString(locationStr, foreStyle, _textStyle, new Point2D(locationX, locationY), height, TextHorizontalAlignmentEnum.Left, TextVerticalAlignmentEnum.Top); // 画文本
    }

    public override Extents2D GetExtents()
    {
        return Extents2D.Empty;
    }

    public override void TransformBy(Matrix2D transformation)
    {
    }
}