using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Nlog.Entities.HtmlElements;
using System.Collections;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class V0AODWaveformElectrodeOffsetCache<TItem, TResult> : AODWaveformCommonCache<TResult>
    where TItem : V0AODWaveformElectrodeOffsetItem, new()
    where TResult : V0AODWaveformElectrodeOffsetResult, new()
{
    #region Param

    [ObservableProperty]
    public partial double OffsetFrequency { get; set; }

    [ObservableProperty]
    public partial int InterpolationCount { get; set; } = 3;

    [ObservableProperty]
    public partial IReadOnlyList<double> Frequencies { get; set; } = [];

    [ObservableProperty]
    public partial double StepFrequency { get; set; }

    [ObservableProperty]
    public partial bool IsOnlyElectrode4 { get; set; } = true;

    #region 方式一逐步遍历

    [ObservableProperty]
    public partial IReadOnlyList<V0AODWaveformElectrodeOffsetFrequencyPeriodParam> ElectrodeOffsetFrequencyPeriodParams { get; set; } = [new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1 }];

    [ObservableProperty]
    public partial IReadOnlyList<V0AODWaveformElectrodeOffsetFrequencyWeightParam> ElectrodeOffsetFrequencyWeightParams { get; set; } = [];

    #endregion

    #region 方式二两两之间遍历

    [ObservableProperty]
    public partial V0AODWaveformElectrodeOffsetFrequencyPeriodParam Electrode2OffsetFrequencyPeriodParam { get; set; } = new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode2 };

    [ObservableProperty]
    public partial V0AODWaveformElectrodeOffsetFrequencyPeriodParam Electrode3OffsetFrequencyPeriodParam { get; set; } = new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode3 };

    [ObservableProperty]
    public partial V0AODWaveformElectrodeOffsetFrequencyPeriodParam Electrode4OffsetFrequencyPeriodParam { get; set; } = new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode4 };

    [ObservableProperty]
    public partial IReadOnlyList<double> Electrode2Weights { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> Electrode3Weights { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> Electrode4Weights { get; set; } = [];

    #endregion

    [ObservableProperty]
    public partial bool IsConfirmAODWaveformElectrodeOffsetResult { get; set; } = true;

    [ObservableProperty]
    public partial IReadOnlyList<V0AODWaveformElectrodeOffsetFrequencyUniformityParam> ElectrodeOffsetFrequencyUniformityParams { get; set; } = [];

    [ObservableProperty]
    public partial int ElectrodeOffsetFrequencyUniformityParamChunkSize { get; set; }

    #endregion Param

    #region Items

    [ObservableProperty]
    public partial IReadOnlyList<V0AODWaveformElectrodeOffsetFrequencyPeriod<TItem>> Step0Items { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<V0AODWaveformElectrodeOffsetFrequencyUniformity<TItem>> Step1Items { get; set; } = [];

    #endregion Items

    #region Result

    [ObservableProperty]
    public partial IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> ElectrodeConfigurationResults { get; set; } = [];

    #endregion Result

    partial void OnElectrodeOffsetFrequencyPeriodParamsChanged(IReadOnlyList<V0AODWaveformElectrodeOffsetFrequencyPeriodParam> value) => UpdateElectrodeOffsetFrequencyWeightParams(value, Frequencies);

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
        electrodeOffsetParamList.Add(new V0AODWaveformElectrodeOffsetFrequencyPeriodParam());

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
        foreach (V0AODWaveformElectrodeOffsetFrequencyPeriodParam selectItem in selectItems)
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
    private void AddElectrodeOffsetFrequencyUniformityParam() => ElectrodeOffsetFrequencyUniformityParams = [.. ElectrodeOffsetFrequencyUniformityParams, new V0AODWaveformElectrodeOffsetFrequencyUniformityParam()];

    [RelayCommand]
    private void RemoveElectrodeOffsetFrequencyUniformityParams(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var electrodeFrequencyUniformityParamList = ElectrodeOffsetFrequencyUniformityParams.ToList();
        foreach (V0AODWaveformElectrodeOffsetFrequencyUniformityParam selectItem in selectItems)
        {
            electrodeFrequencyUniformityParamList.Remove(selectItem);
        }

        ElectrodeOffsetFrequencyUniformityParams = electrodeFrequencyUniformityParamList;
    }

    private void UpdateElectrodeOffsetFrequencyWeightParams(IReadOnlyList<V0AODWaveformElectrodeOffsetFrequencyPeriodParam> electrodeOffsetParams, IReadOnlyList<double> frequencies)
    {
        var oldElectrodeOffsetFrequencyWeightParams = ElectrodeOffsetFrequencyWeightParams;

        ElectrodeOffsetFrequencyWeightParams =
        [
            .. electrodeOffsetParams
                .Where(t => t.OpticsAODElectrodeEnum != OpticsAODElectrodeEnum.Electrode1)
                .SelectMany(t => frequencies.Select(tt => new V0AODWaveformElectrodeOffsetFrequencyWeightParam
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