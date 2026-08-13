using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class AODWaveformElectrodeOffsetFrequencyPeriodParam : ObservableObject
{
    [ObservableProperty]
    public partial OpticsAODElectrodeEnum OpticsAODElectrodeEnum { get; set; }

    [ObservableProperty]
    public partial double BoardCardOffsetFrequencyPeriodCoefficient { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<GenerateAODWaveformUniformityConfiguration> UniformityConfigurations { get; set; } = [];

    public object ToHtmlAnonymous() => new
    {
        OpticsAODElectrodeEnum,
        BoardCardOffsetFrequencyPeriodCoefficient,
        UniformityConfigurations = new HtmlPlot2DLinesChart([(string.Empty, [.. UniformityConfigurations.Select(t => new Point(t.Frequency, t.Coefficient))])], string.Empty)
    };
}