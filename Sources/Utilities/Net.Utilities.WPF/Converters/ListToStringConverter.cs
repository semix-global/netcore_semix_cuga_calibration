using System.Globalization;
using System.Windows;

namespace Net.Utilities.WPF.Converters;

public sealed class ListToStringConverter : AbstractSingletonConverterBase<ListToStringConverter>
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
            // 将字符串按逗号分割，并转换为double列表
            var stringArray = stringValue.Split([','], StringSplitOptions.RemoveEmptyEntries);
            var list = new List<double>();
            foreach (var item in stringArray)
            {
                if (int.TryParse(item.Trim(), out var number))
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