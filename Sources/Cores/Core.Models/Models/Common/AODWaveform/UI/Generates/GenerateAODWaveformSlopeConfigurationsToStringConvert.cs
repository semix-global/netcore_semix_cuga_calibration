using CommunityToolkit.Diagnostics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.WPF.Converters.SingleValue;
using System.Globalization;

namespace Core.Models.Models.Common.AODWaveform.UI.Generates;

public sealed class GenerateAODWaveformSlopeConfigurationsToStringConvert : AbstractSingletonConverterBase<GenerateAODWaveformSlopeConfigurationsToStringConvert>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is IEnumerable<GenerateAODWaveformSlopeConfiguration> slopeConfigurations
            ? string.Join(", ", slopeConfigurations.Index().Select(t => $"{t.Index + 1}: ({t.Item.DeltaKRate:0.##################}, {t.Item.Coefficient:0.###})"))
            : ThrowHelper.ThrowNotSupportedException<object>();

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => ThrowHelper.ThrowNotSupportedException<object>();
}