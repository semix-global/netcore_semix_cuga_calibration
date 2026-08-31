using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Graphics.Drawables;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Graphics.Primitives.Medias.Styles;
using Net.Utilities.Graphics.Renderings;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;
using SkiaSharp;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

public sealed partial class BitmapImageDrawable : AbstractDrawable
{
    private const double ScreenCursorPointRectLength = 9d;
    private const double ScreenRingGap = ScreenCursorPointRectLength / 6d;

    private static readonly LineStyle CursorPointBlackStyle = new(SKColors.Black, ScreenRingGap);
    private static readonly LineStyle CursorPointWhiteStyle = new(SKColors.White, ScreenRingGap);

    [ObservableProperty]
    public partial Point Point { get; set; } = Point.Origin;

    public BitmapImage? BitmapImage
    {
        get;
        set
        {
            field?.Dispose();
            GC.Collect();

            SetProperty(ref field, value);
        }
    }

    [ObservableProperty]
    public partial bool IsShowCrossLine { get; set; }

    [ObservableProperty]
    public partial Point? CursorPoint { get; set; }

    public override void Draw(Renderer renderer)
    {
        if (BitmapImage is null) return;

        renderer.DrawImage(BitmapImage, Point);

        if (IsShowCrossLine)
        {
            var axisBorderLineStyle = renderer.View.Document.Settings.AxisBorderLineStyle;

            var crossLineDistance = Math.Max(BitmapImage.Width / 16d, BitmapImage.Height / 16d);
            var (centerX, centerY) = Point + new Vector(BitmapImage.Width, BitmapImage.Height) / 2d;
            renderer.DrawLine(axisBorderLineStyle, new Point(centerX - crossLineDistance, centerY), new Point(centerX + crossLineDistance, centerY));
            renderer.DrawLine(axisBorderLineStyle, new Point(centerX, centerY - crossLineDistance), new Point(centerX, centerY + crossLineDistance));
        }

        if (CursorPoint is null) return;

        var screenDistanceOf1 = renderer.View.WorldToScreenDistance(1d);
        var wordRingGap = renderer.View.ScreenToWorldDistance(ScreenRingGap);

        double wordWidth;
        if (screenDistanceOf1 > ScreenCursorPointRectLength) wordWidth = 1d;
        else
        {
            var screenCount = (int)Math.Floor(ScreenCursorPointRectLength / screenDistanceOf1);
            if ((screenCount & 0x1) == 0) screenCount += 1; //偶数

            wordWidth = renderer.View.ScreenToWorldDistance(screenCount * screenDistanceOf1);
        }

        var wordOffset = (wordWidth - 1d) / 2d;
        var wordInnerRect = this.ImageCoordinateToCartesianCoordinate(new Rect(
            new Point(CursorPoint.Value.X - wordOffset, CursorPoint.Value.Y - wordOffset),
            new Size(wordWidth, wordWidth)));
        var workMiddleRect = wordInnerRect.Inflate(wordRingGap, wordRingGap);
        var wordOuterRect = workMiddleRect.Inflate(wordRingGap, wordRingGap);

        renderer.DrawRectangle(CursorPointBlackStyle, wordOuterRect);
        renderer.DrawRectangle(CursorPointWhiteStyle, workMiddleRect);
        renderer.DrawRectangle(CursorPointBlackStyle, wordInnerRect);
    }

    public override Extents GetExtents()
    {
        if (BitmapImage is null) return Extents.Empty;

        var extents2D = new Extents();
        extents2D.Add(new Rect(Point, new Size(BitmapImage.Width, BitmapImage.Height)));

        return extents2D;
    }
}