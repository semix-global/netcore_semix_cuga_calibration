using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformElectrodeOffsetItem : AODWaveformCommonItem
{
    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> _electrodeConfigurations = [];

    [ObservableProperty]
    private double _frequency;

    [ObservableProperty]
    private double _amplitude;

    [ObservableProperty]
    private double _offsetFrequencyPeriodCoefficient;

    public override object ToHtmlAnonymous() => new
    {
        Frequency,
        Amplitude,
        OffsetFrequencyPeriodCoefficient,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}