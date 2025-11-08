using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using CommunityToolkit.Diagnostics;
using CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

namespace CugaCalibration.Views.Common.Windows.Tools.AODWaveform;

public sealed class AODWaveformElectrodeOffsetConvert : MarkupExtension, IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is IEnumerable<AODWaveformElectrodeOffsetItem> items
            ? $"{items.First().Frequency}MHz"
            : ThrowHelper.ThrowNotSupportedException<object>();

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => ThrowHelper.ThrowNotSupportedException<object>();

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}