using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Graphics.Drawables;
using Net.Utilities.Graphics.Primitives.Editors;
using Net.Utilities.Graphics.Primitives.Medias.Styles;
using Net.Utilities.Graphics.Renderings;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;
using SkiaSharp;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

public sealed partial class BitmapImageROIDrawable : AbstractDrawable
{
    private static readonly (BitmapImageROIControlPointTypeEnum Type, string Name, Func<Rect, Point> GetPoint)[] ControlPointDefinitions =
    [
        (BitmapImageROIControlPointTypeEnum.XMaxYMax, nameof(Rect.XMaxYMax), rect => rect.XMaxYMax),
        (BitmapImageROIControlPointTypeEnum.XMinYMax, nameof(Rect.XMinYMax), rect => rect.XMinYMax),
        (BitmapImageROIControlPointTypeEnum.XMinYMin, nameof(Rect.XMinYMin), rect => rect.XMinYMin),
        (BitmapImageROIControlPointTypeEnum.XMaxYMin, nameof(Rect.XMaxYMin), rect => rect.XMaxYMin),
        (BitmapImageROIControlPointTypeEnum.XCenterYMax, nameof(Rect.XCenterYMax), rect => rect.XCenterYMax),
        (BitmapImageROIControlPointTypeEnum.XMinYCenter, nameof(Rect.XMinYCenter), rect => rect.XMinYCenter),
        (BitmapImageROIControlPointTypeEnum.XCenterYMin, nameof(Rect.XCenterYMin), rect => rect.XCenterYMin),
        (BitmapImageROIControlPointTypeEnum.XMaxYCenter, nameof(Rect.XMaxYCenter), rect => rect.XMaxYCenter)
    ];

    [ObservableProperty]
    public partial FillStyle FillStyle { get; set; } = new(SKColors.Transparent);

    [ObservableProperty]
    public partial LineStyle LineStyle { get; set; } = new(SKColors.Red);

    [ObservableProperty]
    public partial Rect Rect { get; set; }

    [ObservableProperty]
    public partial bool IsShowCrossLine { get; set; }

    [ObservableProperty]
    public partial BitmapImageROIControlPointTypeEnum ControlPointTypeEnum { get; set; } = BitmapImageROIControlPointTypeEnum.All;

    [ObservableProperty]
    public partial bool IsModified { get; set; }

    // ReSharper disable UnusedParameterInPartialMethod

    partial void OnRectChanged(Rect value) => IsModified = true;

    // ReSharper restore UnusedParameterInPartialMethod

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

    public override ControlPoint[] GetControlPoints()
    {
        var controlPoints = new List<ControlPoint>(ControlPointDefinitions.Length);

        foreach (var (type, name, getPoint) in ControlPointDefinitions)
        {
            if ((ControlPointTypeEnum & type) == type) controlPoints.Add(new ControlPoint(name, getPoint(Rect)));
        }

        return [.. controlPoints];
    }
}