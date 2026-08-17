using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Nlog.Entities.HtmlElements;
using System.Collections;
using MathNet.Numerics.Interpolation;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public partial class AODWaveformElectrodeOffsetCache<TItem, TResult> : AODWaveformCommonCache<TResult>
    where TItem : AODWaveformElectrodeOffsetItem, new()
    where TResult : AODWaveformElectrodeOffsetResult, new()
{
    #region Step0 Param

    [ObservableProperty]
    public partial double OffsetFrequency { get; set; }

    [ObservableProperty]
    public partial double[] Frequencies { get; set; } = [];

    [ObservableProperty]
    public partial int NoiseMeasureTimes { get; set; } = 20;

    [ObservableProperty]
    public partial double ScoreLambda { get; set; } = 1d;
    
    [ObservableProperty]
    public partial double ScoreGamma { get; set; } = 0.5;

    [ObservableProperty]
    public partial AODWaveformElectrodeOffsetFrequencyPeriodParam[] ElectrodeOffsetFrequencyPeriodParams { get; set; } = [new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1 }];

    #endregion Step0 Param

    #region Step1 Param

    [ObservableProperty]
    public partial int AlgorithmInitialPoints { get; set; } = 10;

    [ObservableProperty]
    public partial int AlgorithmEarlyStop { get; set; } = 25;

    [ObservableProperty]
    public partial int AlgorithmRandomState { get; set; } = 42;

    [ObservableProperty]
    public partial int AlgorithmRetryTimes { get; set; } = 200;

    [ObservableProperty]
    public partial int DetailLogInterval { get; set; } = 20;

    #endregion Step1 Param

    #region Step2 Param

    [ObservableProperty]
    public partial double ElectrodeOffsetFrequencyUniformityParamStepFrequency { get; set; }

    [ObservableProperty]
    public partial int ElectrodeOffsetFrequencyUniformityParamChunkSize { get; set; }

    [ObservableProperty]
    public partial AODWaveformElectrodeOffsetFrequencyUniformityParam[] ElectrodeOffsetFrequencyUniformityParams { get; set; } = [];

    #endregion Step2 Param

    #region Items

    [ObservableProperty]
    public partial AODWaveformElectrodeOffsetFrequencyPeriod<TItem> Step0 { get; set; } = new();

    [ObservableProperty]
    public partial AODWaveformElectrodeOffsetFrequencyPeriod<TItem> Step1 { get; set; } = new();

    [ObservableProperty]
    public partial AODWaveformElectrodeOffsetFrequencyUniformity<TItem>[] Step2Items { get; set; } = [];

    #endregion Items

    #region Result

    [ObservableProperty]
    public partial double Noise { get; set; } = 1e-3;

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

    public LinearSpline?[] CreateElectrodeOffsetFrequencyPeriodLinearSplines() =>
    [
        .. ElectrodeOffsetFrequencyPeriodParams.Select(t => t.UniformityConfigurations.Length > 0
            ? LinearSpline.InterpolateSorted(
                [.. t.UniformityConfigurations.Select(configuration => configuration.Frequency)],
                [.. t.UniformityConfigurations.Select(configuration => configuration.Coefficient)])
            : null)
    ];

    public override object ToHtmlAnonymous() => new
    {
        OffsetFrequency,
        Frequencies,
        NoiseMeasureTimes,
        ScoreLambda,
        ScoreGamma,
        ElectrodeOffsetFrequencyPeriodParams = new HtmlTable([.. ElectrodeOffsetFrequencyPeriodParams.Select(t => t.ToHtmlAnonymous())]),
        AlgorithmInitialPoints,
        AlgorithmEarlyStop,
        AlgorithmRandomState,
        AlgorithmRetryTimes,
        DetailLogInterval,
        ElectrodeOffsetFrequencyUniformityParamStepFrequency,
        ElectrodeOffsetFrequencyUniformityParamChunkSize,
        ElectrodeOffsetFrequencyUniformityParams = new HtmlTable([.. ElectrodeOffsetFrequencyUniformityParams.Select(t => t.ToHtmlAnonymous())]),
        Noise,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}