using Net.Utilities.Helper.File;
using Net.Utilities.WPF.Helper;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Net.Utilities.WPF.Converters;

public sealed class StringToBitmapSourceConverter : AbstractSingletonConverterBase<StringToBitmapSourceConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string stringValue) return DependencyProperty.UnsetValue;
        if (File.Exists(stringValue) == false) return DependencyProperty.UnsetValue;
        if (FileHelper.IsImageFile(stringValue) == false) return DependencyProperty.UnsetValue;

        var readAllBytes = File.ReadAllBytes(stringValue);

        return BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(readAllBytes);
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public sealed class StringToBitmapFrameConverter : AbstractSingletonConverterBase<StringToBitmapFrameConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string stringValue) return DependencyProperty.UnsetValue;
        if (File.Exists(stringValue) == false) return DependencyProperty.UnsetValue;
        if (FileHelper.IsImageFile(stringValue) == false) return DependencyProperty.UnsetValue;

        var readAllBytes = File.ReadAllBytes(stringValue);

        return BitmapFrame.Create(BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(readAllBytes));
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}