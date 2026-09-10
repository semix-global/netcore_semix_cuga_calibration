using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Nlog.Entities.HtmlElements;
using System.Collections;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformElectrodeDelayCache<TItem, TResult> : AODWaveformCommonCache<TResult>
    where TItem : AODWaveformElectrodeDelayItem, new()
    where TResult : AODWaveformElectrodeDelayResult, new()
{
    #region Step0 Param

    [ObservableProperty]
    public partial AODWaveformElectrodeDelayFrequency[] AODWaveformElectrodeDelayFrequencies { get; set; } = [];

    [ObservableProperty]
    public partial int DetailLogInterval { get; set; } = 5;

    [ObservableProperty]
    public partial int NoiseMeasureTimes { get; set; } = 20;

    [ObservableProperty]
    public partial double ScoreLambda { get; set; } = 0d;

    [ObservableProperty]
    public partial double ScoreGamma { get; set; } = 0d;

    [ObservableProperty]
    public partial AODWaveformElectrodeDelayParam[] ElectrodeDelayParams { get; set; } = [new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1 }];

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
    public partial AODWaveformElectrodeDelay<TItem> Step0 { get; set; } = new();

    [ObservableProperty]
    public partial AODWaveformElectrodeDelay<TItem> Step1 { get; set; } = new();

    #endregion Items

    #region Result

    [ObservableProperty]
    public partial double Noise { get; set; } = 1e-3;

    [ObservableProperty]
    public partial GenerateAODWaveformElectrodeConfiguration[] ElectrodeConfigurationResults { get; set; } = [];

    #endregion Result

    [RelayCommand]
    private void AddAODWaveformElectrodeDelayFrequency() => AODWaveformElectrodeDelayFrequencies = [.. AODWaveformElectrodeDelayFrequencies, new AODWaveformElectrodeDelayFrequency()];

    [RelayCommand]
    private void RemoveAODWaveformElectrodeDelayFrequencies(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var frequencyList = AODWaveformElectrodeDelayFrequencies.ToList();

        foreach (AODWaveformElectrodeDelayFrequency selectItem in selectItems) frequencyList.Remove(selectItem);

        AODWaveformElectrodeDelayFrequencies = [.. frequencyList];
    }

    [RelayCommand]
    private void AddElectrodeDelayParam()
    {
        var electrodeEnums = EnumHelper.Enums<OpticsAODElectrodeEnum>();

        if (ElectrodeDelayParams.Length > electrodeEnums.Length) return;

        AODWaveformElectrodeDelayParam[] electrodeDelayParams = [.. ElectrodeDelayParams, new()];

        foreach (var (index, item) in electrodeDelayParams.Index()) item.OpticsAODElectrodeEnum = electrodeEnums[index];

        ElectrodeDelayParams = electrodeDelayParams;
    }

    [RelayCommand]
    private void RemoveElectrodeDelayParams(IEnumerable? selectItems)
    {
        if (selectItems is null) return;

        var electrodeEnums = EnumHelper.Enums<OpticsAODElectrodeEnum>();

        var electrodeDelayParamList = ElectrodeDelayParams.ToList();

        foreach (AODWaveformElectrodeDelayParam selectItem in selectItems)
        {
            if (selectItem.OpticsAODElectrodeEnum == OpticsAODElectrodeEnum.Electrode1) continue;

            electrodeDelayParamList.Remove(selectItem);
        }

        foreach (var (index, item) in electrodeDelayParamList.Index()) item.OpticsAODElectrodeEnum = electrodeEnums[index];

        ElectrodeDelayParams = [.. electrodeDelayParamList];
    }

    public override object ToHtmlAnonymous() => new
    {
        AODWaveformElectrodeDelayFrequencies = new HtmlTable([.. AODWaveformElectrodeDelayFrequencies.Select(t => t.ToHtmlAnonymous())]),
        DetailLogInterval,
        NoiseMeasureTimes,
        ScoreLambda,
        ScoreGamma,
        ElectrodeDelayParams = new HtmlTable([.. ElectrodeDelayParams.Select(t => t.ToHtmlAnonymous())]),
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