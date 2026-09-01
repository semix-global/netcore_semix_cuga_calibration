using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Graphics.Drawables;
using Net.Utilities.Graphics.Primitives.Editors;
using Net.Utilities.Graphics.Primitives.Enums.Medias;
using Net.Utilities.Graphics.Primitives.Enums.Medias.Styles;
using Net.Utilities.Graphics.Primitives.Medias;
using Net.Utilities.Graphics.Primitives.Medias.Styles;
using Net.Utilities.Graphics.Renderings;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;
using SkiaSharp;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

public sealed partial class BitmapImageROIDrawable(BitmapImageDrawable bitmapImageDrawable) : AbstractDrawable
{
    private static readonly TextStyle TextStyle = new(Fonts.Monospace, 18d, true, false, true);
    private static readonly FillStyle TextForeground = new(SKColors.Red);
    private static readonly FillStyle TextBackground = new(SKColors.Transparent);
    private static readonly LineStyle TextBorder = new(SKColors.Transparent, 1d, DashEnum.Solid, true);

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
    public partial string Text { get; set; } = string.Empty;

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

        if (IsShowCrossLine)
        {
            renderer.DrawLine(LineStyle, new Point(Rect.Center.X, Rect.YMin), new Point(Rect.Center.X, Rect.YMax));
            renderer.DrawLine(LineStyle, new Point(Rect.XMin, Rect.Center.Y), new Point(Rect.XMax, Rect.Center.Y));
        }

        if (string.IsNullOrEmpty(Text) == false)
        {
            renderer.DrawString(
                TextStyle,
                TextForeground,
                Rect.Center,
                Text,
                AlignmentEnum.MiddleCenter,
                TextBackground,
                TextBorder,
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