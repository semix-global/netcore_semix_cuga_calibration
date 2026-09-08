using CommunityToolkit.Diagnostics;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;
using SkiaSharp;
using System.Globalization;
using System.Windows.Data;
using Point = Net.Utilities.Models.Geometries.Point;
using Rect = Net.Utilities.Models.Geometries.Rect;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Converts;

internal sealed class MultiBitmapImageDrawableToStringConverter : IMultiValueConverter
{
    private const string DefaultString = "- | -";

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var pointString = "-";
        var pixelString = "-";

        if (values is not [Point point, OpticsFourierImageDocument opticsFourierImageDocument]) return ThrowHelper.ThrowNotSupportedException<object>(nameof(values));

        using var scope = opticsFourierImageDocument.View.Sync.EnterScope();

        foreach (var temp in opticsFourierImageDocument.ImageModel) temp.CursorPoint = null;

        var selectionPickDistance = opticsFourierImageDocument.View.ScreenToWorldDistance(opticsFourierImageDocument.Settings.SelectionPickDistance);
        var bitmapImageDrawable = opticsFourierImageDocument.ImageModel
            .FirstOrDefault(d => d.Contains(point, selectionPickDistance));

        if (bitmapImageDrawable?.BitmapImage is null) return DefaultString;

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