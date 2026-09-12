using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform.Generates;
using MiniExcelLibs;
using Net.Utilities.Helpers.Helpers.Structs;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.WPF.Enums;
using Net.Utilities.WPF.MVVM;
using Net.Utilities.WPF.MVVM.Providers;
using System.Collections;
using System.ComponentModel;
using CommunityToolkit.Diagnostics;

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
    public partial AODWaveformScoreMethodEnum AODWaveformScoreMethodEnum { get; set; } = AODWaveformScoreMethodEnum.Legacy;

    [ObservableProperty]
    public partial double LegacyScoreLambda { get; set; } = 0d;

    [ObservableProperty]
    public partial double LegacyScoreGamma { get; set; } = 0d;

    [ObservableProperty]
    public partial double BandWidthScoreThreshold { get; set; } = 0.5;

    [ObservableProperty]
    public partial double BandWidthScoreEpsilon { get; set; } = 0.1;

    [ObservableProperty]
    public partial AODWaveformElectrodeDelayParam[] ElectrodeDelayParams { get; set; } = [new() { OpticsAODElectrodeEnum = OpticsAODElectrodeEnum.Electrode1 }];

    #endregion Step0 Param

    #region Step1 Param

    [ObservableProperty]
    public partial double AlgorithmMaxDelay { get; set; } = 10d;

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

    [ObservableProperty]
    public partial int StabilityMeasureTimes { get; set; } = 20;

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

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task ImportAODWaveformElectrodeDelayFrequenciesAsync(CancellationToken cancellationToken)
    {
        var dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();

        try
        {
            if (dialogWindowProvider.TryShowSelectFilePathDialog(".xlsx", out var filePath) != true) return;

            var values = (await MiniExcel.QueryAsync<AODWaveformElectrodeDelayFrequency>(filePath, cancellationToken: cancellationToken))
                .Where(t => t.Frequency > 0 && t.Amplitude > 0)
                .ToArray();

            Guard.IsNotEmpty(values, "Imported AOD Waveform Electrode Delay Frequencies must not be empty.");

            AODWaveformElectrodeDelayFrequencies = values;

            dialogWindowProvider.ShowDialog("Import AOD Waveform Electrode Delay Frequencies OK!");
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"""
                                             Import AOD Waveform Electrode Delay Frequencies Failed!
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private async Task ExportAODWaveformElectrodeDelayFrequenciesTemplateAsync()
    {
        var dialogWindowProvider = HostApplication.GetRequiredService<IDialogWindowProvider>();

        try
        {
            if (dialogWindowProvider.TryShowSaveFilePathDialog(".xlsx", out var filePath) != true) return;

            await MiniExcel.SaveAsAsync(filePath, new[]
            {
                new AODWaveformElectrodeDelayFrequency { Frequency = 100d, Amplitude = 0.5 },
                new AODWaveformElectrodeDelayFrequency { Frequency = 150d, Amplitude = 1d }
            }, overwriteFile: true);

            dialogWindowProvider.ShowDialog("Export AOD Waveform Electrode Delay Frequencies Template OK!");
        }
        catch (Exception ex)
        {
            dialogWindowProvider.ShowDialog($"""
                                             Export AOD Waveform Electrode Delay Frequencies Template Failed!
                                             {ex.Message}
                                             """, DialogButtonsEnum.OK, DialogIconEnum.Warning);
        }
    }

    [RelayCommand]
    private void AddElectrodeDelayParam()
    {
        var electrodeEnums = EnumHelper.Enums<OpticsAODElectrodeEnum>();

        if (ElectrodeDelayParams.Length >= electrodeEnums.Length) return;

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
        AODWaveformScoreMethodEnum,
        LegacyScoreLambda,
        LegacyScoreGamma,
        BandWidthScoreThreshold,
        BandWidthScoreEpsilon,
        ElectrodeDelayParams = new HtmlTable([.. ElectrodeDelayParams.Select(t => t.ToHtmlAnonymous())]),
        AlgorithmMaxDelay,
        AlgorithmInitialPoints,
        AlgorithmEarlyStop,
        AlgorithmAcquisitionFunctionEnum,
        AlgorithmUniformityAnchorCount,
        AlgorithmRetryTimes,
        StabilityMeasureTimes,
        Noise,
        Base = new HtmlQuote(base.ToHtmlAnonymous())
    };
}

// ReSharper disable InconsistentNaming
public enum AODWaveformScoreMethodEnum
{
    [Description("legacy")]
    Legacy,

    [Description("bandwidth")]
    BandWidth
}

public enum AlgorithmAcquisitionFunctionEnum
{
    LCB,
    EI,
    PI
}
// ReSharper restore InconsistentNaming