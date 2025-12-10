using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformElectrodeOffsetCache<TItem, TResult> : AODWaveformCommonCache
    where TItem : AODWaveformElectrodeOffsetItem, new()
    where TResult : AODWaveformCommonResult, new()
{
    #region Param

    [ObservableProperty]
    private double _offsetFrequency;

    [ObservableProperty]
    private int _interpolationCount = 3;

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeOffsetFrequencyPeriodParam> _electrodeOffsetFrequencyPeriodParams = [new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1 }];

    [ObservableProperty]
    private bool _isConfirmAODWaveformElectrodeOffsetResult = true;

    [ObservableProperty]
    private IReadOnlyList<double> _frequencies = [];

    [ObservableProperty]
    private double _stepFrequency;

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeOffsetFrequencyWeightParam> _electrodeOffsetFrequencyWeightParams = [];

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeOffsetFrequencyUniformityParam> _electrodeOffsetFrequencyUniformityParams = [];

    [ObservableProperty]
    private int _electrodeOffsetFrequencyUniformityParamChunkSize;

    #endregion Param

    #region Items

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IReadOnlyList<AODWaveformElectrodeOffsetFrequencyPeriod<TItem>> _step0Items = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    [property: System.Text.Json.Serialization.JsonIgnore]
    [property: System.Xml.Serialization.XmlIgnore]
    [property: LiteDB.BsonIgnore]
    private IReadOnlyList<AODWaveformElectrodeOffsetStep1<TItem>> _step1Items = [];

    #endregion Items

    #region Result

    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> _electrodeConfigurationResults = [];

    [ObservableProperty]
    private IReadOnlyList<TResult> _results = [];

    #endregion Result

    partial void OnElectrodeOffsetFrequencyPeriodParamsChanged(IReadOnlyList<AODWaveformElectrodeOffsetFrequencyPeriodParam> value) => UpdateElectrodeOffsetFrequencyWeightParams(value, Frequencies);

    partial void OnFrequenciesChanged(IReadOnlyList<double> value) => UpdateElectrodeOffsetFrequencyWeightParams(ElectrodeOffsetFrequencyPeriodParams, value);

    private void UpdateElectrodeOffsetFrequencyWeightParams(IReadOnlyList<AODWaveformElectrodeOffsetFrequencyPeriodParam> electrodeOffsetParams, IReadOnlyList<double> frequencies)
    {
        var oldElectrodeOffsetFrequencyWeightParams = ElectrodeOffsetFrequencyWeightParams;

        ElectrodeOffsetFrequencyWeightParams =
        [
            ..electrodeOffsetParams
                .Where(t => t.OpticsAODElectrodeEnum != OpticsAODElectrodeEnum.Electrode1)
                .SelectMany(t => frequencies.Select(tt => new AODWaveformElectrodeOffsetFrequencyWeightParam
                {
                    OpticsAODElectrodeEnum = t.OpticsAODElectrodeEnum,
                    Frequency = tt,
                    Weight = oldElectrodeOffsetFrequencyWeightParams
                        .FirstOrDefault(ttt => ttt.OpticsAODElectrodeEnum == t.OpticsAODElectrodeEnum && Equals(ttt.Frequency, tt))
                        ?.Weight ?? 1d
                }))
        ];

        ElectrodeOffsetFrequencyWeightParams = ElectrodeOffsetFrequencyWeightParams.DistinctBy(t => (t.OpticsAODElectrodeEnum, t.Frequency)).ToArray();
    }

    public override object ToHtmlAnonymous() => new
    {
        OffsetFrequency,
        InterpolationCount,
        ElectrodeOffsetFrequencyPeriodParams = new HtmlTable([.. ElectrodeOffsetFrequencyPeriodParams.Select(t => t.ToHtmlAnonymous())]),
        IsConfirmAODWaveformElectrodeOffsetResult,
        Frequencies,
        StepFrequency,
        ElectrodeOffsetFrequencyWeightParams = new HtmlTable([.. ElectrodeOffsetFrequencyWeightParams.Select(t => t.ToHtmlAnonymous())]),
        ElectrodeOffsetFrequencyUniformityParams = new HtmlTable([.. ElectrodeOffsetFrequencyUniformityParams.Select(t => t.ToHtmlAnonymous())]),
        ElectrodeOffsetFrequencyUniformityParamChunkSize,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}