using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Graphics.Drawables;
using Net.Utilities.Graphics.Primitives.Medias.Styles;
using Net.Utilities.Graphics.Renderings;
using Net.Utilities.Models.Geometries;
using SkiaSharp;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public partial class StageMapWafer : AbstractDrawable
{
    private static readonly LineStyle WaferBackgroundFillStyle = new(SKColors.Blue, 2);

    [ObservableProperty]
    public partial Circle Circle { get; set; }

    public override void Draw(Renderer renderer)
    {
        renderer.DrawCircle(WaferBackgroundFillStyle, Circle);
    }

    public override Extents GetExtents() => (Extents)(Rect)Circle;
}