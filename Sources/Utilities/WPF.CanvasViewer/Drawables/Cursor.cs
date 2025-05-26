using CanvasViewer.Geometry;
using CanvasViewer.Media;
using CanvasViewer.Media.Drawing;
using CanvasViewer.Media.Drawing.Enum;

namespace CanvasViewer.Drawables;

public sealed class Cursor : AbstractDrawable
{
    /// <summary>
    /// 光标当前位置
    /// </summary>
    public Point2D Location { get; set; }

    /// <summary>
    /// 光标样式
    /// </summary>
    public TextStyle TextStyle { get; set; }

    /// <summary>
    /// 光标提示符高度
    /// </summary>
    public double TextHeight { get; set; }

    /// <summary>
    /// 光标提示符
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// 绘画光标: 在鼠标位置画十字架(中心为矩形)
    /// </summary>
    public Cursor()
    {
        Message = string.Empty;
        // 默认分配默认系统字体
        TextStyle = new TextStyle("_Cursor", "Microsoft YaHei UI", FontStyleEnum.Regular);
        // 默认文本高度(以像素为单位)
        TextHeight = 12;
    }

    public override void Draw(Renderer renderer)
    {
        var view = renderer.View;
        var viewExtents2D = view.GetViewport(); // 相机区域(世界坐标)

        var backColor = renderer.View.Document.Settings.BackColor; // 背景色
        var luma = (int)Math.Sqrt(backColor.R * backColor.R * .299 + backColor.G * backColor.G * .587 + backColor.B * backColor.B * .114); // 背景色亮度luma

        var cursorStyle = new Style(luma > 130 ? Color.Black : Color.White); // 光标样式
        var emptyBoxSize = view.ScreenToWorld(new Vector2D(view.Document.Settings.PickBoxSize + 4, 0)).X / 2; // 矩形大小放大4px(十字架与矩形距离4px)
        var pickBoxSize = view.ScreenToWorld(new Vector2D(view.Document.Settings.PickBoxSize, 0)).X / 2; // 矩形大小
        var pxSize = view.ScreenToWorld(new Vector2D(1, 0)).X / 2;

        // 画十字架
        if (view.Document.Editor.IsInputMode)
        {
            renderer.DrawLine(cursorStyle, new Point2D(viewExtents2D.XMin, Location.Y), new Point2D(Location.X - pxSize, Location.Y));
            renderer.DrawLine(cursorStyle, new Point2D(Location.X + pxSize, Location.Y), new Point2D(viewExtents2D.XMax, Location.Y));
            renderer.DrawLine(cursorStyle, new Point2D(Location.X, viewExtents2D.YMin), new Point2D(Location.X, Location.Y - pxSize));
            renderer.DrawLine(cursorStyle, new Point2D(Location.X, Location.Y + pxSize), new Point2D(Location.X, viewExtents2D.YMax));
        }
        else
        {
            renderer.DrawLine(cursorStyle, new Point2D(viewExtents2D.XMin, Location.Y), new Point2D(Location.X - emptyBoxSize, Location.Y)); // 水平部分左边
            renderer.DrawLine(cursorStyle, new Point2D(Location.X + emptyBoxSize, Location.Y), new Point2D(viewExtents2D.XMax, Location.Y)); // 水平部分右边
            renderer.DrawLine(cursorStyle, new Point2D(Location.X, viewExtents2D.YMin), new Point2D(Location.X, Location.Y - emptyBoxSize)); // 垂直部分下边
            renderer.DrawLine(cursorStyle, new Point2D(Location.X, Location.Y + emptyBoxSize), new Point2D(Location.X, viewExtents2D.YMax)); // 垂直部分上边
            renderer.DrawRectangle(cursorStyle, new Point2D(Location.X - pickBoxSize, Location.Y - pickBoxSize), // rect七点
                new Point2D(Location.X + pickBoxSize, Location.Y + pickBoxSize)); // 中心矩形
        }

        var height = Math.Abs(view.ScreenToWorld(new Vector2D(0, TextHeight)).Y); // 文本高度
        var margin = Math.Abs(view.ScreenToWorld(new Vector2D(4, 0)).X); // 文本边距margin
        var offset = Math.Abs(view.ScreenToWorld(new Vector2D(2, 0)).X); // 文本偏移
        var foreStyle = new Style(view.Document.Settings.CursorPromptForeColor); // 前景样式
        var backStyle = new Style(view.Document.Settings.CursorPromptBackColor); // 背景样式

        // 画坐标 控件右下角
        var locationStr = Location.ToString(view.Document.Settings.NumberFormat);
        var locationX = viewExtents2D.XMin + margin + offset; // 文本位置x
        var locationY = viewExtents2D.YMax - margin - offset; // 文本位置y
        var locationVector2D = renderer.MeasureString(locationStr, foreStyle, TextStyle, height); // 获取文本大小
        renderer.FillRectangle(backStyle, new Point2D(locationX - offset, locationY + offset), new Point2D(locationX + offset + locationVector2D.X, locationY - offset - locationVector2D.Y));
        renderer.DrawRectangle(foreStyle, new Point2D(locationX - offset, locationY + offset), new Point2D(locationX + offset + locationVector2D.X, locationY - offset - locationVector2D.Y));
        renderer.DrawString(locationStr, foreStyle, TextStyle, new Point2D(locationX, locationY), height, TextHorizontalAlignmentEnum.Left, TextVerticalAlignmentEnum.Top); // 画文本

        if (string.IsNullOrEmpty(Message)) return;

        // 画光标提示符
        // 默认情况下，将光标提示符定位到光标的右下角
        var messageX = Location.X + margin + offset; // 文本位置x
        var messageY = Location.Y - margin - offset; // 文本位置y
        var messageVector2D = renderer.MeasureString(Message, foreStyle, TextStyle, height); // 获取文本大小
        var lowerRight = new Point2D(viewExtents2D.XMax, viewExtents2D.YMin); // 右下角
        // 检查提示文本是否水平地插入窗口
        if (messageX + messageVector2D.X + offset > lowerRight.X) messageX = Location.X - margin - offset - messageVector2D.X;

        // 检查提示文本是否垂直适合窗口
        if (messageY - messageVector2D.Y - offset < lowerRight.Y) messageY = Location.Y + margin + offset + messageVector2D.Y;

        renderer.FillRectangle(backStyle, new Point2D(messageX - offset, messageY + offset), new Point2D(messageX + offset + messageVector2D.X, messageY - offset - messageVector2D.Y));
        renderer.DrawRectangle(foreStyle, new Point2D(messageX - offset, messageY + offset), new Point2D(messageX + offset + messageVector2D.X, messageY - offset - messageVector2D.Y));
        renderer.DrawString(Message, foreStyle, TextStyle, new Point2D(messageX, messageY), height, TextHorizontalAlignmentEnum.Left, TextVerticalAlignmentEnum.Top); // 画文本
    }

    public override Extents2D GetExtents()
    {
        return Extents2D.Infinity;
    }

    public override void TransformBy(Matrix2D transformation)
    {
    }
}