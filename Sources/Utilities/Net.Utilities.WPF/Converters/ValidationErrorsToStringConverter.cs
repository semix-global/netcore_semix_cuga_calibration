using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Net.Utilities.WPF.Converters;

public sealed class ValidationErrorsToStringConverter : AbstractSingletonConverterBase<ValidationErrorsToStringConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is not ReadOnlyObservableCollection<ValidationError> errors || errors.Count == 0
            ? DependencyProperty.UnsetValue
            : string.Join(Environment.NewLine, errors.Select(t => t.ErrorContent));
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}