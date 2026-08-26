using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Graphics.Drawables;
using Net.Utilities.Graphics.Primitives.Enums.Medias.Styles;
using Net.Utilities.Graphics.Renderings;
using Net.Utilities.Models.Geometries;
using Net.Utilities.ScottPlot.Helper;
using Net.Utilities.WaferMap.WPF.Primitives;
using ScottPlot;
using SkiaSharp;
using FillStyle = Net.Utilities.Graphics.Primitives.Medias.Styles.FillStyle;
using LineStyle = Net.Utilities.Graphics.Primitives.Medias.Styles.LineStyle;
using Range = ScottPlot.Range;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Stage;

public partial class StageMapDie : AbstractDrawable
{
    private static readonly LineStyle DieBorderLineStyle = new(new SKColor(100, 100, 100));
    private static readonly FillStyle DieBackgroundFillStyle = new(new SKColor(240, 240, 240));
    private static readonly FillStyle DieSelectionBackgroundFillStyle = new(new SKColor(240, 240, 240), HatchEnum.DiagonalUp, new SKColor(100, 100, 100));
    private static readonly LineStyle OriginalDieBorderLineStyle = new(new SKColor(0, 120, 215), 2);

    [ObservableProperty]
    public partial WaferMapDieIndex Index { get; set; } = WaferMapDieIndex.Empty;

    [ObservableProperty]
    public partial int Row { get; set; }

    [ObservableProperty]
    public partial int Col { get; set; }

    [ObservableProperty]
    public partial Rect Rect { get; set; }

    [ObservableProperty]
    public partial bool IsInWafer { get; set; }

    [ObservableProperty]
    public partial Vector[] Markers { get; set; } = [];

    public override void Draw(Renderer renderer)
    {
        if (IsInWafer) renderer.FillRectangle(DieBackgroundFillStyle, Rect);
        if (IsSelected) renderer.FillRectangle(DieSelectionBackgroundFillStyle, Rect);

        renderer.DrawRectangle(DieBorderLineStyle, Rect);
        if (Index == WaferMapDieIndex.Empty) renderer.DrawRectangle(OriginalDieBorderLineStyle, Rect);

        if (IsInWafer == false) return;

        foreach (var (index, marker) in Markers.Index())
        {
            var (centerX, centerY) = Rect.Point + marker;

            var defectSelectionCrossDistance = renderer.View.ScreenToWorldDistance(10d);

            var markerBorderLineStyle = new LineStyle(Constants.Turbo.GetColor(index, new Range(0, Markers.Length - 1)).ToSKColor(), index == 0 ? 3d : 1d);
            renderer.DrawLine(markerBorderLineStyle, new Point(centerX - defectSelectionCrossDistance, centerY), new Point(centerX + defectSelectionCrossDistance, centerY));
            renderer.DrawLine(markerBorderLineStyle, new Point(centerX, centerY - defectSelectionCrossDistance), new Point(centerX, centerY + defectSelectionCrossDistance));
        }
    }

    public override Extents GetExtents() => (Extents)Rect;
}