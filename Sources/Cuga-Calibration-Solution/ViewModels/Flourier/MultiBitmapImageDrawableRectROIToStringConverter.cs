using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Rect = Net.Utilities.Models.Geometries.Rect;

namespace CugaCalibration.ViewModels.Flourier;

internal sealed class MultiBitmapImageDrawableRectROIToStringConverter : IMultiValueConverter
{
    private BitmapImageDrawable? _bitmapImageDrawable;
    private RectROIDrawable? _rectROIDrawable;

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values is not [BitmapImageDrawable bitmapImageDrawable, RectROIDrawable rectROIDrawable, Rect rect]) return "- | -";

        _bitmapImageDrawable = bitmapImageDrawable;
        _rectROIDrawable = rectROIDrawable;

        return $"{bitmapImageDrawable.CartesianCoordinateToImageCoordinate(rect)} | {rect}";
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        if (_bitmapImageDrawable is not null && _rectROIDrawable is not null && value is string str)
        {
            // (3, 11); {24, 23}
            var parts = str.Split('|')[0].Trim().Split(';');
            if (parts.Length != 2) return [_bitmapImageDrawable, _rectROIDrawable, _rectROIDrawable.Rect];

            var pointStr = parts[0].Trim().Trim('(', ')');
            var sizeStr = parts[1].Trim().Trim('{', '}');
            var pointParts = pointStr.Split(',');
            var sizeParts = sizeStr.Split(',');
            if (pointParts.Length == 2
                && sizeParts.Length == 2
                && double.TryParse(pointParts[0].Trim(), out var x)
                && double.TryParse(pointParts[1].Trim(), out var y)
                && double.TryParse(sizeParts[0].Trim(), out var width)
                && double.TryParse(sizeParts[1].Trim(), out var height))
            {
                _rectROIDrawable.Rect = _bitmapImageDrawable.ImageCoordinateToCartesianCoordinate(new Rect(x, y, width, height));
            }

            return [_bitmapImageDrawable, _rectROIDrawable, _rectROIDrawable.Rect];
        }

        return [DependencyProperty.UnsetValue, DependencyProperty.UnsetValue, DependencyProperty.UnsetValue];
    }
}