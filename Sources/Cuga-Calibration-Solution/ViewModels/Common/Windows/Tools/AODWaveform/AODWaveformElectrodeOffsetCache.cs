using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Nlog.Entities.HtmlElements;
using System.Collections;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformElectrodeOffsetCache<TItem, TResult> : AODWaveformCommonCache<TResult>
    where TItem : AODWaveformElectrodeOffsetItem, new()
    where TResult : AODWaveformElectrodeOffsetResult, new()
{
    #region Param

    [ObservableProperty]
    public partial double OffsetFrequency { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<double> Frequencies { get; set; } = [];

    [ObservableProperty]
    public partial int AlgorithmInitialPoints { get; set; } = 10;

    [ObservableProperty]
    public partial double AlgorithmNoise { get; set; } = 1e-4;

    [ObservableProperty]
    public partial int AlgorithmEarlyStop { get; set; } = 25;

    [ObservableProperty]
    public partial int AlgorithmRandomState { get; set; } = 42;

    [ObservableProperty]
    public partial int AlgorithmRetryTimes { get; set; } = 200;
    
    [ObservableProperty]
    public partial IReadOnlyList<AODWaveformElectrodeOffsetFrequencyPeriodParam> ElectrodeOffsetFrequencyPeriodParams { get; set; } = [new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1 }];

    [ObservableProperty]
    public partial IReadOnlyList<AODWaveformElectrodeOffsetFrequencyWeightParam> ElectrodeOffsetFrequencyWeightParams { get; set; } = [];

    [ObservableProperty]
    public partial double ElectrodeOffsetFrequencyUniformityParamStepFrequency { get; set; }

    [ObservableProperty]
    public partial int ElectrodeOffsetFrequencyUniformityParamChunkSize { get; set; }
    
    [ObservableProperty]
    public partial IReadOnlyList<AODWaveformElectrodeOffsetFrequencyUniformityParam> ElectrodeOffsetFrequencyUniformityParams { get; set; } = [];

    #endregion Param

    #region Items

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial AODWaveformElectrodeOffsetFrequencyPeriod<TItem> Step0 { get; set; } = new();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<AODWaveformElectrodeOffsetFrequencyUniformity<TItem>> Step1Items { get; set; } = [];

    #endregion Items

    #region Result

    [ObservableProperty]
    public partial IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> ElectrodeConfigurationResults { get; set; } = [];

    #endregion Result

    partial void OnFrequenciesChanged(IReadOnlyList<double> value)
    {
        var oldElectrodeOffsetFrequencyWeightParams = ElectrodeOffsetFrequencyWeightParams;

        ElectrodeOffsetFrequencyWeightParams =
        [
            .. value.Select(tt => new AODWaveformElectrodeOffsetFrequencyWeightParam
            {
                Frequency = tt,
                Weight = oldElectrodeOffsetFrequencyWeightParams
                    .FirstOrDefault(ttt => Equals(ttt.Frequency, tt))
                    ?.Weight ?? 1d
            })
        ];

        ElectrodeOffsetFrequencyWeightParams = [.. ElectrodeOffsetFrequencyWeightParams.DistinctBy(t => t.Frequency)];
    }

    [RelayCommand]
    private void AddElectrodeOffsetFrequencyPeriodParam()
    {
        var electrodeEnums = EnumHelper.Enums<OpticsAODElectrodeEnum>();

        if (ElectrodeOffsetFrequencyPeriodParams.Count > electrodeEnums.Length) return;

        var electrodeOffsetParamList = ElectrodeOffsetFrequencyPeriodParams.ToList();
        electrodeOffsetParamList.Add(new AODWaveformElectrodeOffsetFrequencyPeriodParam());

        foreach (var (index, item) in electrodeOffsetParamList
                     .Select((item, index) => (index, t: item)))
        {
            item.OpticsAODElectrodeEnum = electrodeEnums[index];
        }

        ElectrodeOffsetFrequencyPeriodParams = electrodeOffsetParamList;
    }

    [RelayCommand]
    private void RemoveElectrodeOffsetFrequencyPeriodParams(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var electrodeEnums = EnumHelper.Enums<OpticsAODElectrodeEnum>();

        var electrodeOffsetParamList = ElectrodeOffsetFrequencyPeriodParams.ToList();
        foreach (AODWaveformElectrodeOffsetFrequencyPeriodParam selectItem in selectItems)
        {
            if (selectItem.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode1) continue;

            electrodeOffsetParamList.Remove(selectItem);
        }

        foreach (var (index, item) in electrodeOffsetParamList
                     .Select((item, index) => (index, t: item)))
        {
            item.OpticsAODElectrodeEnum = electrodeEnums[index];
        }

        ElectrodeOffsetFrequencyPeriodParams = electrodeOffsetParamList;
    }

    [RelayCommand]
    private void AddElectrodeOffsetFrequencyUniformityParam() => ElectrodeOffsetFrequencyUniformityParams = [.. ElectrodeOffsetFrequencyUniformityParams, new AODWaveformElectrodeOffsetFrequencyUniformityParam()];

    [RelayCommand]
    private void RemoveElectrodeOffsetFrequencyUniformityParams(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var electrodeFrequencyUniformityParamList = ElectrodeOffsetFrequencyUniformityParams.ToList();
        foreach (AODWaveformElectrodeOffsetFrequencyUniformityParam selectItem in selectItems)
        {
            electrodeFrequencyUniformityParamList.Remove(selectItem);
        }

        ElectrodeOffsetFrequencyUniformityParams = electrodeFrequencyUniformityParamList;
    }

    public override object ToHtmlAnonymous() => new
    {
        OffsetFrequency,
        Frequencies,
        AlgorithmInitialPoints,
        AlgorithmNoise,
        AlgorithmEarlyStop,
        AlgorithmRandomState,
        AlgorithmRetryTimes,
        ElectrodeOffsetFrequencyPeriodParams = new HtmlTable([.. ElectrodeOffsetFrequencyPeriodParams.Select(t => t.ToHtmlAnonymous())]),
        ElectrodeOffsetFrequencyWeightParams = new HtmlTable([.. ElectrodeOffsetFrequencyWeightParams.Select(t => t.ToHtmlAnonymous())]),
        ElectrodeOffsetFrequencyUniformityParamStepFrequency,
        ElectrodeOffsetFrequencyUniformityParamChunkSize,
        ElectrodeOffsetFrequencyUniformityParams = new HtmlTable([.. ElectrodeOffsetFrequencyUniformityParams.Select(t => t.ToHtmlAnonymous())]),
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}