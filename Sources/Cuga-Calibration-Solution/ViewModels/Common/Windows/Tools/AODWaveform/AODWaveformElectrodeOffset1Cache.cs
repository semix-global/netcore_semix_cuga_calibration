using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformElectrodeOffset1Cache<TItem, TResult> : AODWaveformCommonCache<TResult>
    where TItem : AODWaveformElectrodeOffset1Item, new()
    where TResult : AODWaveformElectrodeOffset1Result, new()
{
    #region Param

    [ObservableProperty]
    private double _offsetFrequency;

    [ObservableProperty]
    private int _interpolationCount = 3;

    [ObservableProperty]
    private AODWaveformElectrodeOffsetFrequencyPeriodParam _electrode2OffsetFrequencyPeriodParam = new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode2 };

    [ObservableProperty]
    private AODWaveformElectrodeOffsetFrequencyPeriodParam _electrode3OffsetFrequencyPeriodParam = new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode3 };

    [ObservableProperty]
    private AODWaveformElectrodeOffsetFrequencyPeriodParam _electrode4OffsetFrequencyPeriodParam = new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode4 };

    [ObservableProperty]
    private bool _isConfirmAODWaveformElectrodeOffsetResult = true;

    [ObservableProperty]
    private IReadOnlyList<double> _frequencies = [];

    [ObservableProperty]
    private double _stepFrequency;

    [ObservableProperty]
    private IReadOnlyList<double> _weights = [];

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
    private IReadOnlyList<AODWaveformElectrodeOffsetFrequencyUniformity<TItem>> _step1Items = [];

    #endregion Items

    #region Result

    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> _electrodeConfigurationResults = [];

    #endregion Result

    partial void OnFrequenciesChanged(IReadOnlyList<double> value) => Weights = [.. value.Select(_ => 1)];

    public override object ToHtmlAnonymous() => new
    {
        OffsetFrequency,
        InterpolationCount,
        Electrode2OffsetFrequencyPeriodParam = new HtmlQuote(Electrode2OffsetFrequencyPeriodParam.ToHtmlAnonymous()),
        Electrode3OffsetFrequencyPeriodParam = new HtmlQuote(Electrode3OffsetFrequencyPeriodParam.ToHtmlAnonymous()),
        Electrode4OffsetFrequencyPeriodParam = new HtmlQuote(Electrode4OffsetFrequencyPeriodParam.ToHtmlAnonymous()),
        IsConfirmAODWaveformElectrodeOffsetResult,
        Frequencies,
        StepFrequency,
        Weights,
        ElectrodeOffsetFrequencyUniformityParams = new HtmlTable([.. ElectrodeOffsetFrequencyUniformityParams.Select(t => t.ToHtmlAnonymous())]),
        ElectrodeOffsetFrequencyUniformityParamChunkSize,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}