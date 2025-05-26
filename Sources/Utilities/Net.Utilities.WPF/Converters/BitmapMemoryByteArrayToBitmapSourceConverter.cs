using Net.Utilities.WPF.Helper;
using System.Globalization;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Net.Utilities.WPF.Converters;

public sealed class BitmapMemoryByteArrayToBitmapSourceConverter : AbstractSingletonConverterBase<BitmapMemoryByteArrayToBitmapSourceConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] byteArray) return DependencyProperty.UnsetValue;

        return byteArray.Length <= 0
            ? DependencyProperty.UnsetValue
            : BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(byteArray);
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public sealed class BitmapMemoryByteArrayToBitmapFrameConverter : AbstractSingletonConverterBase<BitmapMemoryByteArrayToBitmapFrameConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] byteArray) return DependencyProperty.UnsetValue;

        return byteArray.Length <= 0
            ? DependencyProperty.UnsetValue
            : BitmapFrame.Create(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(byteArray));
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}