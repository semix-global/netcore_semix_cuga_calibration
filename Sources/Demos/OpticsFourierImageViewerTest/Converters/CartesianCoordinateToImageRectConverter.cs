using System.Globalization;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Diagnostics;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Extensions;
using Rect = Net.Utilities.Models.Geometries.Rect;

namespace OpticsFourierImageViewerTest.Converters;

public sealed class CartesianCoordinateToImageRectConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values is [Rect rect, BitmapImageROIDrawable bitmapImageROIDrawable]
            ? bitmapImageROIDrawable.BitmapImageDrawable.CartesianCoordinateToImageCoordinate(rect).ToString()
            : DependencyProperty.UnsetValue;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object[]>(nameof(value));
}