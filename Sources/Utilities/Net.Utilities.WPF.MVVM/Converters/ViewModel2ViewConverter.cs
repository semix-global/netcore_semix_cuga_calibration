using Net.Utilities.WPF.Converters;
using Net.Utilities.WPF.MVVM.Services;
using System.Globalization;
using System.Windows;

namespace Net.Utilities.WPF.MVVM.Converters;

public sealed class ViewModel2ViewConverter : AbstractSingletonConverterBase<ViewModel2ViewConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is null ? DependencyProperty.UnsetValue : HostApplication.GetRequiredService<IViewLocatorService>().ViewModelType2View(value.GetType());
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}