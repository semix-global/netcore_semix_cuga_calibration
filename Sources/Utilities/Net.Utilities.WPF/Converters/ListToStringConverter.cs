using System.Globalization;
using System.Windows;

namespace Net.Utilities.WPF.Converters;

public sealed class ListToStringConverter : AbstractSingletonConverterBase<ListToStringConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is null) return string.Empty;
        if (value is List<string> listString) return string.Join(", ", listString);
        if (value is List<double> listDouble) return string.Join(", ", listDouble);
        if (value is List<int> listInt) return string.Join(", ", listInt);
        return DependencyProperty.UnsetValue;
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