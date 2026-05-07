using Net.Utilities.WPF.Converters;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Xml;

namespace CugaCalibration.Core.Converters;

public sealed class StringToXamlConverter : AbstractSingletonConverterBase<StringToXamlConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string xaml || string.IsNullOrWhiteSpace(xaml)) return DependencyProperty.UnsetValue;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xaml));
        using var reader = XmlReader.Create(stream);
        return XamlReader.Load(reader);
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
