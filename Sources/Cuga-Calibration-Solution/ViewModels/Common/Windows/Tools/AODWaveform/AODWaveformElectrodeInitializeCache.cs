using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformElectrodeInitializeCache<TItem, TResult> : AODWaveformCommonCache<TResult>
    where TItem : AODWaveformElectrodeOffsetItem, new()
    where TResult : AODWaveformCommonResult, new()
{
    [ObservableProperty]
    private int _interpolationCount = 3;

    [ObservableProperty]
    private IReadOnlyList<double> _electrode2OffsetFrequencyPeriodCoefficients = [];

    [ObservableProperty]
    private IReadOnlyList<double> _electrode4OffsetFrequencyPeriodCoefficients = [];

    [ObservableProperty]
    private AODWaveformElectrodeOffsetFrequencyPeriodParam _electrode3OffsetFrequencyPeriodParam = new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode3 };

    [ObservableProperty]
    private IReadOnlyList<double> _frequencies = [];

    [ObservableProperty]
    private IReadOnlyList<double> _weights = [];

    #region Items

    [ObservableProperty]
    private AODWaveformElectrodeInitializeStep0<TItem> _step0 = new();

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeOffsetFrequencyPeriod<TItem>> _step1Items = [];

    #endregion

    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> _electrodeConfigurationResults = [];

    [ObservableProperty]
    private IReadOnlyList<TResult> _results = [];

    partial void OnFrequenciesChanged(IReadOnlyList<double> value) => Weights = [.. value.Select(_ => 1)];

    public override object ToHtmlAnonymous() => new
    {
        InterpolationCount,
        Electrode2OffsetFrequencyPeriodCoefficients,
        Electrode4OffsetFrequencyPeriodCoefficients,
        Electrode3OffsetFrequencyPeriodParam = new HtmlQuote(Electrode3OffsetFrequencyPeriodParam.ToHtmlAnonymous()),
        Frequencies,
        Weights,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}