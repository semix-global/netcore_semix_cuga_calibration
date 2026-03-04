using System.Globalization;
using System.Windows;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.ImageViewer.WPF.Drawables;
using Net.Utilities.WPF.Converters;

namespace Core.Models.Models.Common.DarkField;

public sealed class DarkFieldRawScanImageDTOToBitmapImageDrawableConverter : AbstractSingletonConverterBase<DarkFieldRawScanImageDTOToBitmapImageDrawableConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DarkFieldRawScanImageDTO darkFieldImageDTO) return DependencyProperty.UnsetValue;

        using var image = darkFieldImageDTO.GetImage();

        var bitmapImageDrawable = new BitmapImageDrawable
        {
            BitmapImage = image.ToBitmapImage(12)
        };

        return bitmapImageDrawable;
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}