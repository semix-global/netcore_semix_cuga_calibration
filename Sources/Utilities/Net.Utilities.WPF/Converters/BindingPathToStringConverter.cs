using Net.Utilities.WPF.AttachedHelper;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Net.Utilities.WPF.Converters;

public sealed class BindingPathToStringConverter : AbstractSingletonConverterBase<BindingPathToStringConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DependencyObject obj)
        {
            var targetProp = BindingPathHelper.GetTargetProperty(obj);
            if (targetProp == null) return string.Empty;

            var binding = BindingOperations.GetBinding(obj, targetProp);
            return binding?.Path.Path.Split('.').Last() ?? string.Empty;
        }

        return string.Empty;
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}