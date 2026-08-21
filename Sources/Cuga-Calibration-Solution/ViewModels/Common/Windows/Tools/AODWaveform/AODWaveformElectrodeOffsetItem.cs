using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformElectrodeOffsetItem : AODWaveformCommonItem
{
    [ObservableProperty]
    public partial GenerateAODWaveformElectrodeConfiguration[] ElectrodeConfigurations { get; set; } = [];

    [ObservableProperty]
    public partial double Frequency { get; set; }

    [ObservableProperty]
    public partial double Amplitude { get; set; }

    public override object ToHtmlAnonymous() => new
    {
        ElectrodeConfigurations = new HtmlTable([.. ElectrodeConfigurations.Select(t => t.ToFlatnessHtmlAnonymous())]),
        Frequency,
        Amplitude,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}