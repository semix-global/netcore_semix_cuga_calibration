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
    public partial double[] Frequencies { get; set; } = [];

    [ObservableProperty]
    public partial double AlgorithmLambda { get; set; } = 1d;

    [ObservableProperty]
    public partial int AlgorithmInitialPoints { get; set; } = 10;

    [ObservableProperty]
    public partial double AlgorithmNoise { get; set; } = 1e-3;

    [ObservableProperty]
    public partial int AlgorithmEarlyStop { get; set; } = 25;

    [ObservableProperty]
    public partial int AlgorithmRandomState { get; set; } = 42;

    [ObservableProperty]
    public partial int AlgorithmRetryTimes { get; set; } = 200;

    [ObservableProperty]
    public partial int DetailLogInterval { get; set; } = 20;

    [ObservableProperty]
    public partial AODWaveformElectrodeOffsetFrequencyPeriodParam[] ElectrodeOffsetFrequencyPeriodParams { get; set; } = [new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1 }];

    [ObservableProperty]
    public partial double ElectrodeOffsetFrequencyUniformityParamStepFrequency { get; set; }

    [ObservableProperty]
    public partial int ElectrodeOffsetFrequencyUniformityParamChunkSize { get; set; }

    [ObservableProperty]
    public partial AODWaveformElectrodeOffsetFrequencyUniformityParam[] ElectrodeOffsetFrequencyUniformityParams { get; set; } = [];

    #endregion Param

    #region Items

    [ObservableProperty]
    public partial AODWaveformElectrodeOffsetFrequencyPeriod<TItem> Step0 { get; set; } = new();

    [ObservableProperty]
    public partial AODWaveformElectrodeOffsetFrequencyUniformity<TItem>[] Step1Items { get; set; } = [];

    #endregion Items

    #region Result

    [ObservableProperty]
    public partial GenerateAODWaveformElectrodeConfiguration[] ElectrodeConfigurationResults { get; set; } = [];

    #endregion Result

    [RelayCommand]
    private void AddElectrodeOffsetFrequencyPeriodParam()
    {
        var electrodeEnums = EnumHelper.Enums<OpticsAODElectrodeEnum>();

        if (ElectrodeOffsetFrequencyPeriodParams.Length > electrodeEnums.Length) return;

        AODWaveformElectrodeOffsetFrequencyPeriodParam[] electrodeOffsetFrequencyPeriodParams = [.. ElectrodeOffsetFrequencyPeriodParams, new()];

        foreach (var (index, item) in electrodeOffsetFrequencyPeriodParams.Index()) item.OpticsAODElectrodeEnum = electrodeEnums[index];

        ElectrodeOffsetFrequencyPeriodParams = electrodeOffsetFrequencyPeriodParams;
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

        foreach (var (index, item) in electrodeOffsetParamList.Index()) item.OpticsAODElectrodeEnum = electrodeEnums[index];

        ElectrodeOffsetFrequencyPeriodParams = [.. electrodeOffsetParamList];
    }


    [RelayCommand]
    private void AddElectrodeOffsetFrequencyUniformityParam() => ElectrodeOffsetFrequencyUniformityParams = [.. ElectrodeOffsetFrequencyUniformityParams, new AODWaveformElectrodeOffsetFrequencyUniformityParam()];

    [RelayCommand]
    private void RemoveElectrodeOffsetFrequencyUniformityParams(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var electrodeFrequencyUniformityParamList = ElectrodeOffsetFrequencyUniformityParams.ToList();

        foreach (AODWaveformElectrodeOffsetFrequencyUniformityParam selectItem in selectItems) electrodeFrequencyUniformityParamList.Remove(selectItem);

        ElectrodeOffsetFrequencyUniformityParams = [.. electrodeFrequencyUniformityParamList];
    }

    public override object ToHtmlAnonymous() => new
    {
        OffsetFrequency,
        Frequencies,
        AlgorithmLambda,
        AlgorithmInitialPoints,
        AlgorithmNoise,
        AlgorithmEarlyStop,
        AlgorithmRandomState,
        AlgorithmRetryTimes,
        DetailLogInterval,
        ElectrodeOffsetFrequencyPeriodParams = new HtmlTable([.. ElectrodeOffsetFrequencyPeriodParams.Select(t => t.ToHtmlAnonymous())]),
        ElectrodeOffsetFrequencyUniformityParamStepFrequency,
        ElectrodeOffsetFrequencyUniformityParamChunkSize,
        ElectrodeOffsetFrequencyUniformityParams = new HtmlTable([.. ElectrodeOffsetFrequencyUniformityParams.Select(t => t.ToHtmlAnonymous())]),
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}