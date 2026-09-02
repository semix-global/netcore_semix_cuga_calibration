using CommunityToolkit.Diagnostics;
using Net.Utilities.WPF.Converters.SingleValue;
using System.Globalization;

namespace Net.Utilities.WPF.Converters.Calibration.SingleValue;

public sealed class IntsToStringConverter : AbstractSingletonConverterBase<IntsToStringConverter>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is IEnumerable<int> ints
            ? string.Join(", ", ints)
            : ThrowHelper.ThrowNotSupportedException<object>();

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string str
            ? str.Split([','], StringSplitOptions.RemoveEmptyEntries)
                .Select(t => int.TryParse(t, out var result) ? (IsOk: true, Result: result) : (IsOk: false, Result: 0d))
                .Where(t => t.IsOk)
                .Select(t => t.Result)
                .ToArray()
            : ThrowHelper.ThrowNotSupportedException<object>();
}