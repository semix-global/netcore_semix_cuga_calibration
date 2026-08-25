using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Graphics.Drawables;
using Net.Utilities.Graphics.Primitives.Editors;
using Net.Utilities.Graphics.Primitives.Medias.Styles;
using Net.Utilities.Graphics.Renderings;
using Net.Utilities.Models.Geometries;
using SkiaSharp;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

public sealed partial class RectROIDrawable : AbstractDrawable
{
    [ObservableProperty]
    public partial FillStyle FillStyle { get; set; } = new(SKColors.Transparent);

    [ObservableProperty]
    public partial LineStyle LineStyle { get; set; } = new(SKColors.Red);

    [ObservableProperty]
    public partial bool IsShowCrossLine { get; set; }

    [ObservableProperty]
    public partial Rect Rect { get; set; }

    public override void Draw(Renderer renderer)
    {
        if (Rect.IsEmpty) return;

        renderer.FillRectangle(FillStyle, Rect);
        renderer.DrawRectangle(LineStyle, Rect);

        if (IsShowCrossLine == false) return;

        renderer.DrawLine(LineStyle, new Point(Rect.Center.X, Rect.YMin), new Point(Rect.Center.X, Rect.YMax));
        renderer.DrawLine(LineStyle, new Point(Rect.XMin, Rect.Center.Y), new Point(Rect.XMax, Rect.Center.Y));
    }

    public override Extents GetExtents() => (Extents)Rect;

    public override ControlPoint[] GetControlPoints() =>
    [
        new(nameof(Rect.XMaxYMax), Rect.XMaxYMax),
        new(nameof(Rect.XMinYMax), Rect.XMinYMax),
        new(nameof(Rect.XMinYMin), Rect.XMinYMin),
        new(nameof(Rect.XMaxYMin), Rect.XMaxYMin),
        new(nameof(Rect.XCenterYMax), Rect.XCenterYMax),
        new(nameof(Rect.XMinYCenter), Rect.XMinYCenter),
        new(nameof(Rect.XCenterYMin), Rect.XCenterYMin),
        new(nameof(Rect.XMaxYCenter), Rect.XMaxYCenter)
    ];
}