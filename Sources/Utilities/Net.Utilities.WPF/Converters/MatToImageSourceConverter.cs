using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Net.Utilities.WPF.Converters;

public class MatToImageSourceConverter : AbstractSingletonConverterBase<MatToImageSourceConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return Binding.DoNothing; // 不需要任何绑定

        if (value is not Mat mat) throw new NotSupportedException();

        return mat.ToBitmapSource();
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not BitmapImage bitmapImage) throw new NotSupportedException();
        var convertedSource = new FormatConvertedBitmap(bitmapImage, PixelFormats.Bgr24, null, 0);
        return convertedSource.ToMat();
    }
}