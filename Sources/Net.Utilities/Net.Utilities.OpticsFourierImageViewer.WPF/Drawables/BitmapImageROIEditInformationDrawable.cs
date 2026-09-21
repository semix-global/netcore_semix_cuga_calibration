using CommunityToolkit.Diagnostics;
using Net.Utilities.Graphics.Drawables;
using Net.Utilities.Graphics.Primitives.Enums.Medias;
using Net.Utilities.Graphics.Primitives.Medias;
using Net.Utilities.Graphics.Primitives.Medias.Styles;
using Net.Utilities.Graphics.Renderings;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Editors;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;
using SkiaSharp;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

public sealed class BitmapImageROIEditInformationDrawable(ModifyBitmapImageROIDrawableInputOptions options) : AbstractDrawable
{
    private static readonly TextStyle BodyTextStyle = new(Fonts.Monospace, 11d);
    private static readonly TextStyle SaveTextStyle = new(Fonts.Monospace, 13d, isBold: true);
    private static readonly FillStyle BackgroundStyle = new(new SKColor(20, 25, 35, 225));
    private static readonly FillStyle ForegroundStyle = new(SKColors.White);
    private static readonly FillStyle SaveForegroundStyle = new(new SKColor(255, 215, 90));
    private static readonly FillStyle TransparentFillStyle = new(SKColors.Transparent);
    private static readonly LineStyle TransparentLineStyle = new(SKColors.Transparent);

    public override bool IsInModel => false;

    public override void Draw(Renderer renderer)
    {
        var image = options.BitmapImageDrawable;
        if (image.BitmapImage?.IsEmpty != false) return;

        string[] lines =
        [
            "Enter / Ctrl + S: Save and exit editing",
            "ROI editing: Only visible, unlocked ROIs can be edited",
            "Left-click / drag to select; hold Ctrl to toggle selection",
            "Drag inside a selected ROI to move; drag a handle to resize",
            $"Movement: {options.BitmapImageROIDragMoveTypeEnum switch
            {
                BitmapImageROIDragMoveTypeEnum.None => "disabled",
                BitmapImageROIDragMoveTypeEnum.X => "horizontal only",
                BitmapImageROIDragMoveTypeEnum.Y => "vertical only",
                _ => "horizontal and vertical"
            }}; resize uses each ROI's allowed handles",
            $"Arrow keys: Move by {options.KeyboardMoveStep:g} px",
            $"Shift + Arrow keys: Expand the corresponding edge by {options.KeyboardResizeStep:g} px",
            "Ctrl + A: Select all editable ROIs",
            "Ctrl + Z: Undo; Ctrl + Shift + Z: Redo",
            options.IsDeleteEnabled ? "Delete: Hide selected ROIs (can be undone)" : "Delete: Disabled",
            "Esc: Cancel editing and restore the original state"
        ];

        var bounds = renderer.View.GetViewBounds();
        var padding = renderer.View.ScreenToWorldDistance(6d);
        var gap = renderer.View.ScreenToWorldDistance(2d);
        var sizes = lines.Select((text, index) => renderer.Measure(index == 0 ? SaveTextStyle : BodyTextStyle, ForegroundStyle, text).Size).ToArray();
        var width = sizes.Max(size => size.Width) + padding * 2d;
        var height = sizes.Sum(size => size.Height) + gap * (lines.Length - 1) + padding * 2d;

        var origin = options.EditInformationCorner switch
        {
            BitmapImageROIResizeJoystickStateEnum.XMinYMax => new Point(bounds.XMin + padding, bounds.YMax - padding - height),
            BitmapImageROIResizeJoystickStateEnum.XMaxYMax => new Point(bounds.XMax - padding - width, bounds.YMax - padding - height),
            BitmapImageROIResizeJoystickStateEnum.XMinYMin => new Point(bounds.XMin + padding, bounds.YMin + padding),
            BitmapImageROIResizeJoystickStateEnum.XMaxYMin => new Point(bounds.XMax - padding - width, bounds.YMin + padding),
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(options.EditInformationCorner))
        };
        renderer.FillRectangle(BackgroundStyle, new Rect(origin, new Size(width, height)));

        var y = origin.Y + height - padding;
        for (var i = 0; i < lines.Length; i++)
        {
            renderer.DrawString(
                i == 0 ? SaveTextStyle : BodyTextStyle,
                i == 0 ? SaveForegroundStyle : ForegroundStyle,
                new Point(origin.X + padding, y - sizes[i].Height / 2d),
                lines[i],
                AlignmentEnum.MiddleRight,
                TransparentFillStyle,
                TransparentLineStyle,
                new Vector(0d, 0d),
                new Padding(0d));
            y -= sizes[i].Height + gap;
        }
    }

    public override Extents GetExtents() => Extents.Infinity;
}