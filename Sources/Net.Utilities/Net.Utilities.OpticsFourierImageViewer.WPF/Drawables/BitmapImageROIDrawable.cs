using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Graphics.Drawables;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Editors;
using Net.Utilities.Graphics.Primitives.Enums.Medias;
using Net.Utilities.Graphics.Primitives.Medias;
using Net.Utilities.Graphics.Primitives.Medias.Styles;
using Net.Utilities.Graphics.Renderings;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;
using SkiaSharp;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

public sealed partial class BitmapImageROIDrawable(BitmapImageDrawable bitmapImageDrawable) : AbstractDrawable
{
    private static readonly (BitmapImageROIResizeJoystickStateEnum Type, string Name, Func<Rect, Point> GetPoint)[] ControlPointDefinitions =
    [
        (BitmapImageROIResizeJoystickStateEnum.XMaxYMax, nameof(BitmapImageROIResizeJoystickStateEnum.XMaxYMax), rect => rect.XMaxYMax),
        (BitmapImageROIResizeJoystickStateEnum.XMinYMax, nameof(BitmapImageROIResizeJoystickStateEnum.XMinYMax), rect => rect.XMinYMax),
        (BitmapImageROIResizeJoystickStateEnum.XMinYMin, nameof(BitmapImageROIResizeJoystickStateEnum.XMinYMin), rect => rect.XMinYMin),
        (BitmapImageROIResizeJoystickStateEnum.XMaxYMin, nameof(BitmapImageROIResizeJoystickStateEnum.XMaxYMin), rect => rect.XMaxYMin),
        (BitmapImageROIResizeJoystickStateEnum.XCenterYMax, nameof(BitmapImageROIResizeJoystickStateEnum.XCenterYMax), rect => rect.XCenterYMax),
        (BitmapImageROIResizeJoystickStateEnum.XMinYCenter, nameof(BitmapImageROIResizeJoystickStateEnum.XMinYCenter), rect => rect.XMinYCenter),
        (BitmapImageROIResizeJoystickStateEnum.XCenterYMin, nameof(BitmapImageROIResizeJoystickStateEnum.XCenterYMin), rect => rect.XCenterYMin),
        (BitmapImageROIResizeJoystickStateEnum.XMaxYCenter, nameof(BitmapImageROIResizeJoystickStateEnum.XMaxYCenter), rect => rect.XMaxYCenter)
    ];

    public readonly BitmapImageDrawable BitmapImageDrawable = bitmapImageDrawable;

    [ObservableProperty]
    public partial FillStyle FillStyle { get; set; } = new(SKColors.Red.WithAlpha(64));

    [ObservableProperty]
    public partial FillStyle FixedFillStyle { get; set; } = new(SKColors.Green.WithAlpha(64));

    [ObservableProperty]
    public partial Rect Rect { get; set; }

    [ObservableProperty]
    public partial bool IsFixed { get; set; } = false;

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    [ObservableProperty]
    public partial BitmapImageROIResizeJoystickStateEnum ResizeJoystickStateEnum { get; set; } = BitmapImageROIResizeJoystickStateEnum.All;

    [ObservableProperty]
    public partial bool IsEditorModified { get; internal set; } = false;

    public override void Draw(Renderer renderer)
    {
        if (BitmapImageDrawable.BitmapImage is null || Rect.IsEmpty) return;

        renderer.FillRectangle(IsFixed ? FixedFillStyle : FillStyle, Rect);
        var lineStyle = new LineStyle(IsFixed
            ? FixedFillStyle.BackgroundColor.WithAlpha(255)
            : FillStyle.BackgroundColor.WithAlpha(255));
        renderer.DrawRectangle(lineStyle, Rect);

        if (string.IsNullOrEmpty(Text) == false)
        {
            renderer.DrawString(
                new TextStyle(Fonts.Monospace, Math.Min(Rect.Width, Rect.Height) / 2d, isUnitPx: false),
                new FillStyle(IsFixed
                    ? FixedFillStyle.BackgroundColor.ToReadableForegroundColor().WithAlpha(FixedFillStyle.BackgroundColor.Alpha)
                    : FillStyle.BackgroundColor.ToReadableForegroundColor().WithAlpha(FillStyle.BackgroundColor.Alpha)),
                Rect.Center,
                Text,
                AlignmentEnum.MiddleCenter,
                new FillStyle(SKColors.Transparent),
                new LineStyle(SKColors.Transparent),
                new Vector(0d, 0d),
                new Padding(0d));
        }
    }

    public override Extents GetExtents() => (Extents)Rect;

    public override ControlPoint[] GetControlPoints()
    {
        var controlPointList = new List<ControlPoint>(ControlPointDefinitions.Length);

        foreach (var (type, name, getPoint) in ControlPointDefinitions)
        {
            if ((ResizeJoystickStateEnum & type) == type) controlPointList.Add(new ControlPoint(name, getPoint(Rect)));
        }

        return [.. controlPointList];
    }
}