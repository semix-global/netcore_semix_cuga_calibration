using Net.Utilities.WPF.Helper;
using System.Globalization;
using System.IO;
using System.Windows;

namespace Net.Utilities.WPF.Converters;

public sealed class PackStringToBitmapSourceConverter : AbstractSingletonConverterBase<PackStringToBitmapSourceConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string stringValue) return DependencyProperty.UnsetValue;

        var resourceInfo = Application.GetResourceStream(new Uri(stringValue))!;
        using var stream = resourceInfo.Stream;
        using var binaryReader = new BinaryReader(stream);
        var bytes = binaryReader.ReadBytes((int)stream.Length);

        return BitmapSourceHelper.BitmapMemoryByteArrayToBitmapSource(bytes);
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}