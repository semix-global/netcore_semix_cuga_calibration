using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Nlog.Entities.HtmlElements;
using System.Collections;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class V0AODWaveformElectrodeDelayCache<TItem, TResult> : AODWaveformCommonCache<TResult>
    where TItem : V0AODWaveformElectrodeDelayItem, new()
    where TResult : V0AODWaveformElectrodeDelayResult, new()
{
    #region Param

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
    public partial IReadOnlyList<V0AODWaveformElectrodeDelayParam> ElectrodeDelayParams { get; set; } = [new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1 }];

    [ObservableProperty]
    public partial IReadOnlyList<V0AODWaveformElectrodeDelayFrequencyWeightParam> ElectrodeDelayFrequencyWeightParams { get; set; } = [];

    #endregion

    #region 方式二两两之间遍历

    [ObservableProperty]
    public partial V0AODWaveformElectrodeDelayParam Electrode2DelayParam { get; set; } = new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode2 };

    [ObservableProperty]
    public partial V0AODWaveformElectrodeDelayParam Electrode3DelayParam { get; set; } = new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode3 };

    [ObservableProperty]
    public partial V0AODWaveformElectrodeDelayParam Electrode4DelayParam { get; set; } = new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode4 };

    [ObservableProperty]
    public partial IReadOnlyList<double> Electrode2Weights { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> Electrode3Weights { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<double> Electrode4Weights { get; set; } = [];

    #endregion

    [ObservableProperty]
    public partial bool IsConfirmAODWaveformElectrodeDelayResult { get; set; } = true;

    [ObservableProperty]
    public partial IReadOnlyList<V0AODWaveformElectrodeDelayFrequencyUniformityParam> ElectrodeDelayFrequencyUniformityParams { get; set; } = [];

    [ObservableProperty]
    public partial int ElectrodeDelayFrequencyUniformityParamChunkSize { get; set; }

    #endregion Param

    #region Items

    [ObservableProperty]
    public partial IReadOnlyList<V0AODWaveformElectrodeDelay<TItem>> Step0Items { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<V0AODWaveformElectrodeDelayFrequencyUniformity<TItem>> Step1Items { get; set; } = [];

    #endregion Items

    #region Result

    [ObservableProperty]
    public partial IReadOnlyList<GenerateAODWaveformElectrodeConfiguration> ElectrodeConfigurationResults { get; set; } = [];

    #endregion Result

    partial void OnElectrodeDelayParamsChanged(IReadOnlyList<V0AODWaveformElectrodeDelayParam> value) => UpdateElectrodeDelayFrequencyWeightParams(value, Frequencies);

    partial void OnFrequenciesChanged(IReadOnlyList<double> value)
    {
        UpdateElectrodeDelayFrequencyWeightParams(ElectrodeDelayParams, value);
        Electrode2Weights = [.. value.Select(_ => 1)];
        Electrode3Weights = [.. value.Select(_ => 1)];
        Electrode4Weights = [.. value.Select(_ => 1)];
    }

    [RelayCommand]
    private void AddElectrodeDelayParam()
    {
        var electrodeEnums = EnumHelper.Enums<OpticsAODElectrodeEnum>();

        if (ElectrodeDelayParams.Count > electrodeEnums.Length) return;

        var electrodeDelayParamList = ElectrodeDelayParams.ToList();
        electrodeDelayParamList.Add(new V0AODWaveformElectrodeDelayParam());

        foreach (var (index, item) in electrodeDelayParamList
                     .Select((item, index) => (index, t: item)))
        {
            item.OpticsAODElectrodeEnum = electrodeEnums[index];
        }

        ElectrodeDelayParams = electrodeDelayParamList;
    }

    [RelayCommand]
    private void RemoveElectrodeDelayParams(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var electrodeEnums = EnumHelper.Enums<OpticsAODElectrodeEnum>();

        var electrodeDelayParamList = ElectrodeDelayParams.ToList();
        foreach (V0AODWaveformElectrodeDelayParam selectItem in selectItems)
        {
            if (selectItem.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode1) continue;

            electrodeDelayParamList.Remove(selectItem);
        }

        foreach (var (index, item) in electrodeDelayParamList
                     .Select((item, index) => (index, t: item)))
        {
            item.OpticsAODElectrodeEnum = electrodeEnums[index];
        }

        ElectrodeDelayParams = electrodeDelayParamList;
    }

    [RelayCommand]
    private void AddElectrodeDelayFrequencyUniformityParam() => ElectrodeDelayFrequencyUniformityParams = [.. ElectrodeDelayFrequencyUniformityParams, new V0AODWaveformElectrodeDelayFrequencyUniformityParam()];

    [RelayCommand]
    private void RemoveElectrodeDelayFrequencyUniformityParams(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var electrodeFrequencyUniformityParamList = ElectrodeDelayFrequencyUniformityParams.ToList();
        foreach (V0AODWaveformElectrodeDelayFrequencyUniformityParam selectItem in selectItems)
        {
            electrodeFrequencyUniformityParamList.Remove(selectItem);
        }

        ElectrodeDelayFrequencyUniformityParams = electrodeFrequencyUniformityParamList;
    }

    private void UpdateElectrodeDelayFrequencyWeightParams(IReadOnlyList<V0AODWaveformElectrodeDelayParam> electrodeDelayParams, IReadOnlyList<double> frequencies)
    {
        var oldElectrodeDelayFrequencyWeightParams = ElectrodeDelayFrequencyWeightParams;

        ElectrodeDelayFrequencyWeightParams =
        [
            .. electrodeDelayParams
                .Where(t => t.OpticsAODElectrodeEnum != OpticsAODElectrodeEnum.Electrode1)
                .SelectMany(t => frequencies.Select(tt => new V0AODWaveformElectrodeDelayFrequencyWeightParam
                {
                    OpticsAODElectrodeEnum = t.OpticsAODElectrodeEnum,
                    Frequency = tt,
                    Weight = oldElectrodeDelayFrequencyWeightParams
                        .FirstOrDefault(ttt => ttt.OpticsAODElectrodeEnum == t.OpticsAODElectrodeEnum && Equals(ttt.Frequency, tt))
                        ?.Weight ?? 1d
                }))
        ];

        ElectrodeDelayFrequencyWeightParams = ElectrodeDelayFrequencyWeightParams.DistinctBy(t => (t.OpticsAODElectrodeEnum, t.Frequency)).ToArray();
    }

    public override object ToHtmlAnonymous() => new
    {
        InterpolationCount,
        Frequencies,
        StepFrequency,
        ElectrodeDelayParams = new HtmlTable([.. ElectrodeDelayParams.Select(t => t.ToHtmlAnonymous())]),
        Electrode2DelayParam = new HtmlQuote(Electrode2DelayParam.ToHtmlAnonymous()),
        Electrode3DelayParam = new HtmlQuote(Electrode3DelayParam.ToHtmlAnonymous()),
        Electrode4DelayParam = new HtmlQuote(Electrode4DelayParam.ToHtmlAnonymous()),
        Electrode2Weights,
        Electrode3Weights,
        Electrode4Weights,
        IsConfirmAODWaveformElectrodeDelayResult,
        ElectrodeDelayFrequencyWeightParams = new HtmlTable([.. ElectrodeDelayFrequencyWeightParams.Select(t => t.ToHtmlAnonymous())]),
        ElectrodeDelayFrequencyUniformityParams = new HtmlTable([.. ElectrodeDelayFrequencyUniformityParams.Select(t => t.ToHtmlAnonymous())]),
        ElectrodeDelayFrequencyUniformityParamChunkSize,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}