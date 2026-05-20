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
    private double _offsetFrequency;

    [ObservableProperty]
    private int _interpolationCount = 3;

    [ObservableProperty]
    private IReadOnlyList<double> _frequencies = [];

    [ObservableProperty]
    private double _stepFrequency;

    [ObservableProperty]
    private bool _isOnlyElectrode4 = true;

    #region 方式一逐步遍历

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeOffsetFrequencyPeriodParam> _electrodeOffsetFrequencyPeriodParams = [new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1 }];

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeOffsetFrequencyWeightParam> _electrodeOffsetFrequencyWeightParams = [];

    #endregion

    #region 方式二两两之间遍历

    [ObservableProperty]
    private AODWaveformElectrodeOffsetFrequencyPeriodParam _electrode2OffsetFrequencyPeriodParam = new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode2 };

    [ObservableProperty]
    private AODWaveformElectrodeOffsetFrequencyPeriodParam _electrode3OffsetFrequencyPeriodParam = new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode3 };

    [ObservableProperty]
    private AODWaveformElectrodeOffsetFrequencyPeriodParam _electrode4OffsetFrequencyPeriodParam = new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode4 };

    [ObservableProperty]
    private IReadOnlyList<double> _electrode2Weights = [];

    [ObservableProperty]
    private IReadOnlyList<double> _electrode3Weights = [];

    [ObservableProperty]
    private IReadOnlyList<double> _electrode4Weights = [];

    #endregion

    [ObservableProperty]
    private bool _isConfirmAODWaveformElectrodeOffsetResult = true;

    [ObservableProperty]
    private IReadOnlyList<AODWaveformElectrodeOffsetFrequencyUniformityParam> _electrodeOffsetFrequencyUniformityParams = [];

    [ObservableProperty]
    private int _electrodeOffsetFrequencyUniformityParamChunkSize;

    #endregion Param

    #region Items

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    private IReadOnlyList<AODWaveformElectrodeOffsetFrequencyPeriod<TItem>> _step0Items = [];

    [ObservableProperty]
    [property: Newtonsoft.Json.JsonIgnore]
    private IReadOnlyList<AODWaveformElectrodeOffsetFrequencyUniformity<TItem>> _step1Items = [];

    #endregion Items

    #region Result

    [ObservableProperty]
    private IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> _electrodeConfigurationResults = [];

    #endregion Result

    partial void OnElectrodeOffsetFrequencyPeriodParamsChanged(IReadOnlyList<AODWaveformElectrodeOffsetFrequencyPeriodParam> value) => UpdateElectrodeOffsetFrequencyWeightParams(value, Frequencies);

    partial void OnFrequenciesChanged(IReadOnlyList<double> value)
    {
        UpdateElectrodeOffsetFrequencyWeightParams(ElectrodeOffsetFrequencyPeriodParams, value);
        Electrode2Weights = [.. value.Select(_ => 1)];
        Electrode3Weights = [.. value.Select(_ => 1)];
        Electrode4Weights = [.. value.Select(_ => 1)];
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
        Frequencies,
        StepFrequency,
        ElectrodeOffsetFrequencyPeriodParams = new HtmlTable([.. ElectrodeOffsetFrequencyPeriodParams.Select(t => t.ToHtmlAnonymous())]),
        Electrode2OffsetFrequencyPeriodParam = new HtmlQuote(Electrode2OffsetFrequencyPeriodParam.ToHtmlAnonymous()),
        Electrode3OffsetFrequencyPeriodParam = new HtmlQuote(Electrode3OffsetFrequencyPeriodParam.ToHtmlAnonymous()),
        Electrode4OffsetFrequencyPeriodParam = new HtmlQuote(Electrode4OffsetFrequencyPeriodParam.ToHtmlAnonymous()),
        Electrode2Weights,
        Electrode3Weights,
        Electrode4Weights,
        IsConfirmAODWaveformElectrodeOffsetResult,
        ElectrodeOffsetFrequencyWeightParams = new HtmlTable([.. ElectrodeOffsetFrequencyWeightParams.Select(t => t.ToHtmlAnonymous())]),
        ElectrodeOffsetFrequencyUniformityParams = new HtmlTable([.. ElectrodeOffsetFrequencyUniformityParams.Select(t => t.ToHtmlAnonymous())]),
        ElectrodeOffsetFrequencyUniformityParamChunkSize,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}