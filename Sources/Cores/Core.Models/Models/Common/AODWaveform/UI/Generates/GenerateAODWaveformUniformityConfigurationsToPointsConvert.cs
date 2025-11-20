using CommunityToolkit.Diagnostics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.Converters;
using System.Globalization;

namespace Core.Models.Models.Common.AODWaveform.UI.Generates;

public sealed class GenerateAODWaveformUniformityConfigurationsToPointsConvert : AbstractSingletonConverterBase<GenerateAODWaveformUniformityConfigurationsToPointsConvert>
{
    public override object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is IEnumerable<GenerateAODWaveformUniformityConfiguration> uniformityConfigurations
            ? uniformityConfigurations.Select(t => new Point(t.Frequency, t.Coefficient)).ToArray()
            : ThrowHelper.ThrowNotSupportedException<object>();

    public override object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is IEnumerable<Point> points
            ? points.Select(t => new GenerateAODWaveformUniformityConfiguration { Frequency = t.X, Coefficient = t.Y }).ToArray()
            : ThrowHelper.ThrowNotSupportedException<object>();
}