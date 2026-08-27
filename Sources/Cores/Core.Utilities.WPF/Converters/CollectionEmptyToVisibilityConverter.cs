using CommunityToolkit.Diagnostics;
using Net.Utilities.WPF.Converters.SingleValue;
using System.Collections;
using System.Globalization;
using System.Windows;

namespace Core.Utilities.WPF.Converters;

public sealed class CollectionEmptyToVisibilityConverter : AbstractSingletonConverterBase<CollectionEmptyToVisibilityConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ICollection collection) return DependencyProperty.UnsetValue;

        return collection.Count <= 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => ThrowHelper.ThrowNotSupportedException<object>();
}