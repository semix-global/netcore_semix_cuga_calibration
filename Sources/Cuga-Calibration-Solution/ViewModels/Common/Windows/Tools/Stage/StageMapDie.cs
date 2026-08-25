using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Graphics.Drawables;
using Net.Utilities.Graphics.Primitives.Enums.Medias.Styles;
using Net.Utilities.Graphics.Primitives.Medias.Styles;
using Net.Utilities.Graphics.Renderings;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WaferMap.WPF.Primitives;
using SkiaSharp;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public partial class StageMapDie : AbstractDrawable
{
    [ObservableProperty]
    public partial LineStyle LineStyle { get; set; } = new(new SKColor(100, 100, 100));

    [ObservableProperty]
    public partial WaferMapDieIndex Index { get; set; } = WaferMapDieIndex.Empty;

    [ObservableProperty]
    public partial int Row { get; set; }

    [ObservableProperty]
    public partial int Col { get; set; }

    [ObservableProperty]
    public partial Rect Rect { get; set; }

    public override void Draw(Renderer renderer)
    {
        renderer.DrawRectangle(LineStyle, Rect);
    }

    public override Extents GetExtents() => (Extents)Rect;
}

public partial class StageMapCircle : AbstractDrawable
{
    private static readonly LineStyle CircleLineStyle = new LineStyle(SKColors.Red, 2, DashEnum.Dot);

    [ObservableProperty]
    public partial Circle Circle { get; set; }

    public override void Draw(Renderer renderer)
    {
        renderer.DrawCircle(CircleLineStyle, Circle);
    }

    public override Extents GetExtents() => (Extents)(Rect)Circle;
}