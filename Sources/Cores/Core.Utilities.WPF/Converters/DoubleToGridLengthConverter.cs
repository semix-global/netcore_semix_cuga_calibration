using CommunityToolkit.Diagnostics;
using Net.Utilities.WPF.Converters;
using System.Globalization;
using System.Windows;

namespace Core.Utilities.WPF.Converters;

public sealed class DoubleToGridLengthConverter : AbstractSingletonConverterBase<DoubleToGridLengthConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is double i
        ? new GridLength(i)
        : ThrowHelper.ThrowNotSupportedException<object>(nameof(value));

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is GridLength gl
        ? gl.Value
        : ThrowHelper.ThrowNotSupportedException<object>(nameof(value));
}