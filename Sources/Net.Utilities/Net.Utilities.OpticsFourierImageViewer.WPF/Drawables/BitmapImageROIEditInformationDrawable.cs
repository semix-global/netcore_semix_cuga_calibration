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
    private static readonly TextStyle TitleTextStyle = new(Fonts.Sans, 13d, isBold: true);
    private static readonly FillStyle TitleForegroundStyle = new(new SKColor(185, 220, 255));

    private static readonly TextStyle SubTitleTextStyle = new(Fonts.Sans);
    private static readonly FillStyle SubTitleForegroundStyle = new(new SKColor(168, 182, 202));
    private static readonly FillStyle ShortcutForegroundStyle = new(new SKColor(242, 204, 143));

    private static readonly TextStyle BodyTextStyle = new(Fonts.Sans, 11d);
    private static readonly FillStyle BodyForegroundStyle = new(new SKColor(214, 222, 233));
    private static readonly FillStyle BodyShortcutForegroundStyle = new(new SKColor(154, 198, 210));

    private static readonly FillStyle BackgroundStyle = new(new SKColor(20, 28, 40, 238));
    private static readonly LineStyle BorderStyle = new(new SKColor(67, 83, 106, 190));

    public override bool IsInModel => false;

    public override void Draw(Renderer renderer)
    {
        var image = options.BitmapImageDrawable;
        if (image.BitmapImage?.IsEmpty != false) return;

        List<(string Text, TextStyle TextStyle, FillStyle ForegroundStyle)> lines =
        [
            ("ROI editing: Only visible and unlocked ROIs can be edited.", SubTitleTextStyle, SubTitleForegroundStyle),
            ("Enter / Ctrl + S: Save and exit; Esc: Discard changes and exit.", BodyTextStyle, ShortcutForegroundStyle),
            ("Ctrl + Z: Undo; Ctrl + Shift + Z: Redo.", BodyTextStyle, ShortcutForegroundStyle),
            ("Tab: Show / hide this help panel.", BodyTextStyle, ShortcutForegroundStyle),
            ("Select ROIs(Mouse): Left-Click a ROI or drag a selection box; Ctrl + Left-Click / drag box: toggle; Ctrl + A: select all.", BodyTextStyle, BodyForegroundStyle),
            ($"Move ROIs(Mouse): {options.BitmapImageROIDragMoveTypeEnum switch
            {
                BitmapImageROIDragMoveTypeEnum.None => "Disabled.",
                BitmapImageROIDragMoveTypeEnum.X => "Drag a selected ROI (horizontal only).",
                BitmapImageROIDragMoveTypeEnum.Y => "Drag a selected ROI (vertical only).",
                _ => "Drag a selected ROI (all directions)."
            }}", BodyTextStyle, BodyForegroundStyle),
            ("Resize ROIs(Mouse): Drag a handle (joystick) on the ROI edge or corner.", BodyTextStyle, BodyForegroundStyle),
            ($"Move ROIs(Keyboard): {options.BitmapImageROIDragMoveTypeEnum switch
            {
                BitmapImageROIDragMoveTypeEnum.None => "Disabled.",
                BitmapImageROIDragMoveTypeEnum.X => $"Alt + ←→ step: {options.KeyboardMoveStep} px; Ctrl + Alt + ←→: 10x step; (horizontal only).",
                BitmapImageROIDragMoveTypeEnum.Y => $"Alt + ↑↓ step: {options.KeyboardMoveStep} px; Ctrl + Alt + ↑↓: 10x step; (vertical only).",
                _ => $"Alt + ↑↓←→ step: {options.KeyboardMoveStep} px; Ctrl + Alt + ↑↓←→: 10x step; (all directions)."
            }}", BodyTextStyle, BodyShortcutForegroundStyle),
            ($"Resize ROIs(Keyboard): ↑↓←→: bottom / left edge; Shift + ↑↓←→: top / right edge (step: {options.KeyboardResizeStep} px); Ctrl or (Ctrl + Shift) + ↑↓←→ : 10x step.", BodyTextStyle, BodyShortcutForegroundStyle),
            (options.IsDeleteEnabled ? "Delete ROIs(Keyboard): Delete: hide the selected ROIs (undo with Ctrl + Z)" : "Delete ROIs: Disabled.", BodyTextStyle, BodyShortcutForegroundStyle),
        ];

        if (string.IsNullOrWhiteSpace(options.Title) == false) lines.Insert(0, (options.Title, TitleTextStyle, TitleForegroundStyle));

        var bounds = renderer.View.GetViewBounds();

        var padding = renderer.View.ScreenToWorldDistance(5d);
        var gap = renderer.View.ScreenToWorldDistance(1d);
        var measuredLines = lines.Select(line => (
            line.Text,
            line.TextStyle,
            line.ForegroundStyle,
            renderer.Measure(line.TextStyle, line.ForegroundStyle, line.Text).Size)).ToArray();
        var width = measuredLines.Max(line => line.Size.Width) + padding * 2d;
        var height = measuredLines.Sum(line => line.Size.Height) + gap * (lines.Count - 1) + padding * 2d;

        var origin = options.EditInformationCornerEnum switch
        {
            BitmapImageROIResizeJoystickStateEnum.XMinYMax => new Point(bounds.XMin + padding, bounds.YMax - padding - height),
            BitmapImageROIResizeJoystickStateEnum.XMaxYMax => new Point(bounds.XMax - padding - width, bounds.YMax - padding - height),
            BitmapImageROIResizeJoystickStateEnum.XMinYMin => new Point(bounds.XMin + padding, bounds.YMin + padding),
            BitmapImageROIResizeJoystickStateEnum.XMaxYMin => new Point(bounds.XMax - padding - width, bounds.YMin + padding),
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Point>(nameof(options.EditInformationCornerEnum))
        };

        renderer.FillRectangle(BackgroundStyle, new Rect(origin, new Size(width, height)));
        renderer.DrawRectangle(BorderStyle, new Rect(origin, new Size(width, height)));

        var y = origin.Y + height - padding;
        foreach (var (text, textStyle, foregroundStyle, size) in measuredLines)
        {
            renderer.DrawString(
                textStyle,
                foregroundStyle,
                new Point(origin.X + padding, y - size.Height / 2d),
                text,
                AlignmentEnum.MiddleRight,
                FillStyles.Transparent,
                LineStyles.Transparent,
                new Vector(0d, 0d),
                new Padding(0d));
            y -= size.Height + gap;
        }
    }

    public override Extents GetExtents() => Extents.Infinity;
}