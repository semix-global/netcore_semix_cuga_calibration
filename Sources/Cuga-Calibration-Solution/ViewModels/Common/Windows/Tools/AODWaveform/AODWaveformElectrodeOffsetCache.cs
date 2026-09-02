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
    #region Step0 Param

    [ObservableProperty]
    public partial double OffsetFrequency { get; set; }

    [ObservableProperty]
    public partial AODWaveformElectrodeOffsetFrequency[] AODWaveformElectrodeOffsetFrequencies { get; set; } = [];

    [ObservableProperty]
    public partial int DetailLogInterval { get; set; } = 5;

    [ObservableProperty]
    public partial int NoiseMeasureTimes { get; set; } = 20;

    [ObservableProperty]
    public partial double ScoreLambda { get; set; } = 0d;

    [ObservableProperty]
    public partial double ScoreGamma { get; set; } = 0d;

    [ObservableProperty]
    public partial AODWaveformElectrodeOffsetFrequencyPeriodParam[] ElectrodeOffsetFrequencyPeriodParams { get; set; } = [new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1 }];

    #endregion Step0 Param

    #region Step1 Param

    [ObservableProperty]
    public partial int AlgorithmInitialPoints { get; set; } = 10;

    [ObservableProperty]
    public partial int AlgorithmEarlyStop { get; set; } = 25;

    [ObservableProperty]
    public partial AlgorithmAcquisitionFunctionEnum AlgorithmAcquisitionFunctionEnum { get; set; } = AlgorithmAcquisitionFunctionEnum.LCB;

    [ObservableProperty]
    public partial int AlgorithmUniformityAnchorCount { get; set; } = 5;

    [ObservableProperty]
    public partial int AlgorithmRetryTimes { get; set; } = 200;

    #endregion Step1 Param

    #region Items

    [ObservableProperty]
    public partial AODWaveformElectrodeOffsetFrequencyPeriod<TItem> Step0 { get; set; } = new();

    [ObservableProperty]
    public partial AODWaveformElectrodeOffsetFrequencyPeriod<TItem> Step1 { get; set; } = new();

    #endregion Items

    #region Result

    [ObservableProperty]
    public partial double Noise { get; set; } = 1e-3;

    [ObservableProperty]
    public partial GenerateAODWaveformElectrodeConfiguration[] ElectrodeConfigurationResults { get; set; } = [];

    #endregion Result

    [RelayCommand]
    private void AddAODWaveformElectrodeOffsetFrequency() => AODWaveformElectrodeOffsetFrequencies = [.. AODWaveformElectrodeOffsetFrequencies, new AODWaveformElectrodeOffsetFrequency()];

    [RelayCommand]
    private void RemoveAODWaveformElectrodeOffsetFrequencies(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var frequencyList = AODWaveformElectrodeOffsetFrequencies.ToList();

        foreach (AODWaveformElectrodeOffsetFrequency selectItem in selectItems) frequencyList.Remove(selectItem);

        AODWaveformElectrodeOffsetFrequencies = [.. frequencyList];
    }

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

    public override object ToHtmlAnonymous() => new
    {
        OffsetFrequency,
        AODWaveformElectrodeOffsetFrequencies = new HtmlTable([.. AODWaveformElectrodeOffsetFrequencies.Select(t => t.ToHtmlAnonymous())]),
        DetailLogInterval,
        NoiseMeasureTimes,
        ScoreLambda,
        ScoreGamma,
        ElectrodeOffsetFrequencyPeriodParams = new HtmlTable([.. ElectrodeOffsetFrequencyPeriodParams.Select(t => t.ToHtmlAnonymous())]),
        AlgorithmInitialPoints,
        AlgorithmEarlyStop,
        AlgorithmAcquisitionFunctionEnum,
        AlgorithmUniformityAnchorCount,
        AlgorithmRetryTimes,
        Noise,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}

// ReSharper disable InconsistentNaming
public enum AlgorithmAcquisitionFunctionEnum
{
    LCB,
    EI,
    PI
}
// ReSharper restore InconsistentNaming