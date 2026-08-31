using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Diagnostics;
using Net.Utilities.Graphics;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;
using SkiaSharp;
using Point = Net.Utilities.Models.Geometries.Point;
using Rect = Net.Utilities.Models.Geometries.Rect;
using Size = Net.Utilities.Models.Geometries.Size;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Converts;

internal sealed class MultiBitmapImageDrawableToStringConverter : IMultiValueConverter
{
    private const string DefaultString = "- | -";

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var pointString = "-";
        var pixelString = "-";

        if (values is not [Point point, CanvasDocument document]) return ThrowHelper.ThrowNotSupportedException<object>(nameof(values));

        using var scope = document.View.Sync.EnterScope();

        var selectionPickDistance = document.View.ScreenToWorldDistance(document.Settings.SelectionPickDistance);
        var bitmapImageDrawable = document.View.VisibleItems
            .OfType<BitmapImageDrawable>()
            .FirstOrDefault(d => d.Contains(point, selectionPickDistance));

        if (bitmapImageDrawable?.BitmapImage is null)
        {
            foreach (var temp in document.View.VisibleItems.OfType<BitmapImageDrawable>()) temp.CursorPoint = null;

            return DefaultString;
        }

        var cursorPosition = bitmapImageDrawable.CartesianCoordinateToImageCoordinate(point);
        uint? cursorPointColor;
        if (new Rect(Point.Origin, bitmapImageDrawable.BitmapImage.Size).Contains(cursorPosition))
        {
            bitmapImageDrawable.CursorPoint = cursorPosition;

            var (x, y) = (PointI)cursorPosition;
            cursorPointColor = bitmapImageDrawable.BitmapImage.GetPixel(x, y);
        }
        else
        {
            bitmapImageDrawable.CursorPoint = null;

            cursorPointColor = null;
        }

        if (bitmapImageDrawable.CursorPoint is not null) pointString = bitmapImageDrawable.CursorPoint.ToString();
        if (cursorPointColor is null) return $"{pixelString} | {pointString}";

        if (bitmapImageDrawable.BitmapImage.ImageInfo.BytesPerPixel == 4)
        {
            var skColor = new SKColor(cursorPointColor.Value);
            pixelString = $"{skColor.Red}, {skColor.Green}, {skColor.Blue}, {skColor.Alpha}";
        }
        else pixelString = cursorPointColor.Value.ToString();

        return $"{pixelString} | {pointString}";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object[]>(nameof(value));
}