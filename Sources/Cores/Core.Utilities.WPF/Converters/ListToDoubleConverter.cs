using Net.Utilities.WPF.Converters.SingleValue;
using System.Globalization;
using System.Windows;

namespace Core.Utilities.WPF.Converters;

public sealed class ListToDoubleConverter : AbstractSingletonConverterBase<ListToDoubleConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => string.Empty,
            IEnumerable<string> listString => string.Join(", ", listString),
            IEnumerable<double> listDouble => string.Join(", ", listDouble),
            IEnumerable<int> listInt => string.Join(", ", listInt),
            _ => DependencyProperty.UnsetValue
        };
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string stringValue)
        {
            // 支持多种分隔符（可选增强）
            var separators = new char[] { ',', ';', ' ', '\t' };
            var stringArray = stringValue.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            var list = new List<double>();
            foreach (var item in stringArray)
            {
                if (double.TryParse(
                        item.Trim(),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out double number))
                {
                    list.Add(number);
                }
                else
                {
                    return DependencyProperty.UnsetValue;
                }
            }

            return list;
        }

        return DependencyProperty.UnsetValue;
    }
}