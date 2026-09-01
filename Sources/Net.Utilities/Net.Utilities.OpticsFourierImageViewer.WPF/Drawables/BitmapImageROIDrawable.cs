using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Graphics.Drawables;
using Net.Utilities.Graphics.Primitives.Editors;
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
    public partial FillStyle FillStyle { get; set; } = new(SKColors.Transparent);

    [ObservableProperty]
    public partial LineStyle LineStyle { get; set; } = new(SKColors.Red);

    [ObservableProperty]
    public partial Rect Rect { get; set; }

    [ObservableProperty]
    public partial bool IsShowCrossLine { get; set; }

    [ObservableProperty]
    public partial BitmapImageROIResizeJoystickStateEnum ResizeJoystickStateEnum { get; set; } = BitmapImageROIResizeJoystickStateEnum.All;

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
        var controlPointList = new List<ControlPoint>(ControlPointDefinitions.Length);

        foreach (var (type, name, getPoint) in ControlPointDefinitions)
        {
            if ((ResizeJoystickStateEnum & type) == type) controlPointList.Add(new ControlPoint(name, getPoint(Rect)));
        }

        return [.. controlPointList];
    }
}