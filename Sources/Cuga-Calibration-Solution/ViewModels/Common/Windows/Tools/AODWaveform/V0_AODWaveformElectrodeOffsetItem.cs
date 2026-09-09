using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class V0AODWaveformElectrodeOffsetItem : AODWaveformCommonItem
{
    [ObservableProperty]
    public partial IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> ElectrodeConfigurations { get; set; } = [];

    [ObservableProperty]
    public partial double Frequency { get; set; }

    [ObservableProperty]
    public partial double Amplitude { get; set; }

    [ObservableProperty]
    public partial double Delay { get; set; }

    public override object ToHtmlAnonymous() => new
    {
        Frequency,
        Amplitude,
        Delay,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}