using Net.Utilities.Graphics.Extensions;
using Net.Utilities.ImageViewer.WPF.Drawables;
using Net.Utilities.WPF.Converters.SingleValue;
using System.Globalization;
using System.Windows;

namespace Core.Models.Models.Common.DarkField;

public sealed class DarkFieldRawScanImageDTOToBitmapImageDrawableConverter : AbstractSingletonConverterBase<DarkFieldRawScanImageDTOToBitmapImageDrawableConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DarkFieldRawScanImageDTO darkFieldImageDTO) return DependencyProperty.UnsetValue;

        var image = darkFieldImageDTO.GetImage();

        var bitmapImageDrawable = new BitmapImageDrawable
        {
            BitmapImage = image
        };

        var (min, max) = bitmapImageDrawable.BitmapImage.GetChannelRange();
        bitmapImageDrawable.ChannelMinValue = min;
        bitmapImageDrawable.ChannelMaxValue = max;

        return bitmapImageDrawable;
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}