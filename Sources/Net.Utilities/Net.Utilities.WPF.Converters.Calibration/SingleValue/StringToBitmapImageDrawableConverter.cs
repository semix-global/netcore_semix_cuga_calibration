using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.ImageViewer.WPF.Drawables;
using Net.Utilities.WPF.Converters.SingleValue;
using System.Globalization;
using System.IO;
using System.Windows;

namespace Net.Utilities.WPF.Converters.Calibration.SingleValue;

public sealed class StringToBitmapImageDrawableConverter : AbstractSingletonConverterBase<StringToBitmapImageDrawableConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string stringValue
            || File.Exists(stringValue) == false
            || stringValue.IsImageFile(true) == false) return DependencyProperty.UnsetValue;

        var bitmapImageDrawable = new BitmapImageDrawable
        {
            BitmapImage = BitmapHelper.OpenImage(stringValue)
        };

        return bitmapImageDrawable;
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}