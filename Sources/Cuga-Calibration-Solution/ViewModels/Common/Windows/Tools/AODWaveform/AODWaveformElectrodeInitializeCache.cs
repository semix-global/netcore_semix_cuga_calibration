using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformElectrodeInitializeCache<TItem, TResult> : AODWaveformCommonCache<TResult>
    where TItem : AODWaveformElectrodeInitializeItem, new()
    where TResult : AODWaveformElectrodeInitializeResult, new()
{
    [ObservableProperty]
    public partial int InterpolationCount { get; set; } = 3;

    [ObservableProperty]
    public partial IReadOnlyList<double> Electrode2OffsetFrequencyPeriodCoefficients { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> Electrode4OffsetFrequencyPeriodCoefficients { get; set; } = [];

    [ObservableProperty]
    public partial AODWaveformElectrodeOffsetFrequencyPeriodParam Electrode3OffsetFrequencyPeriodParam { get; set; } = new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode3 };

    [ObservableProperty]
    public partial IReadOnlyList<double> Frequencies { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> Weights { get; set; } = [];

    #region Items

    [ObservableProperty]
    public partial AODWaveformElectrodeInitializeStep0<TItem> Step0 { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<AODWaveformElectrodeOffsetFrequencyPeriod<TItem>> Step1Items { get; set; } = [];

    #endregion

    [ObservableProperty]
    public partial IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> ElectrodeConfigurationResults { get; set; } = [];

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