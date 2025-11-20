using CommunityToolkit.Diagnostics;
using Net.Utilities.WPF.Converters;
using System.Globalization;

namespace Core.Utilities.WPF.Converters;

public sealed class DoublesToStringConverter : AbstractSingletonConverterBase<DoublesToStringConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is IEnumerable<double> doubles
            ? string.Join(", ", doubles)
            : ThrowHelper.ThrowNotSupportedException<object>();

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string str
            ? str.Split([','], StringSplitOptions.RemoveEmptyEntries)
                .Select(t => double.TryParse(t, out var result) ? (IsOk: true, Result: result) : (IsOk: false, Result: 0d))
                .Where(t => t.IsOk)
                .Select(t => t.Result)
                .ToArray()
            : ThrowHelper.ThrowNotSupportedException<object>();
}