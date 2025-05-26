using System.Globalization;
using System.Windows;
using Size = Net.Utilities.Models.Size;

namespace Net.Utilities.WPF.Converters;

public sealed class SizeToStringConverter : AbstractSingletonConverterBase<SizeToStringConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return DependencyProperty.UnsetValue;
        if (value is not Size sz) throw new NotSupportedException();
        return sz.ToShortString();
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string str) throw new NotSupportedException();
        var parts = str.Replace(" ", "").Split(';', ',');

        if (parts.Length != 2 || parts.All(part => double.TryParse(part, out _)) == false) return Size.Empty;

        var width = double.Parse(parts[0]);
        var height = double.Parse(parts[1]);
        return new Size(width, height);
    }
}