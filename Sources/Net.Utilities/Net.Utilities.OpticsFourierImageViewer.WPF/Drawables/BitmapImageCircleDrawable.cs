using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Graphics.Drawables;
using Net.Utilities.Graphics.Primitives.Medias.Styles;
using Net.Utilities.Graphics.Renderings;
using Net.Utilities.Models.Geometries;
using SkiaSharp;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

public sealed partial class BitmapImageCircleDrawable(BitmapImageDrawable bitmapImageDrawable) : AbstractDrawable
{
    /// <summary>
    /// 该圆形所依附的图片. 圆心坐标使用图片坐标系.
    /// </summary>
    public readonly BitmapImageDrawable BitmapImageDrawable = bitmapImageDrawable;

    /// <summary>
    /// 笛卡尔坐标系: 当前绘制的圆形.
    /// </summary>
    [ObservableProperty]
    public partial Circle Circle { get; set; } = Circle.Empty;

    /// <summary>
    /// 圆形的轮廓线样式.
    /// </summary>
    [ObservableProperty]
    public partial LineStyle LineStyle { get; set; } = new(SKColors.Red, 2d);

    public override void Draw(Renderer renderer)
    {
        if (Circle.IsEmpty) return;

        renderer.DrawCircle(LineStyle, Circle);
    }

    public override Extents GetExtents() => Circle.IsEmpty ? Extents.Empty : (Extents)(Rect)Circle;
}