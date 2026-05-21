using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.AODWaveform;

public sealed partial class ChirpAODWaveformTrainingCache : ObservableCacheBase
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial GeneratePrescanAODWaveformParam GeneratePrescanAODWaveformParam { get; set; } = new();

    [ObservableProperty]
    public partial GenerateChirpAODWaveformParam GenerateChirpAODWaveformParam { get; set; } = new();

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial Point DSWMachinePosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial double ScanLength { get; set; }

    [ObservableProperty]
    public partial double CenterECS { get; set; }

    [ObservableProperty]
    public partial double RangeECS { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmBestYStrehlRatioResult { get; set; } = true;

    [ObservableProperty]
    public partial double P2Coefficient { get; set; }

    [ObservableProperty]
    public partial double P3Coefficient { get; set; }

    [ObservableProperty]
    public partial double P4Coefficient { get; set; }

    [ObservableProperty]
    public partial double P5Coefficient { get; set; }

    [ObservableProperty]
    public partial double P6Coefficient { get; set; }

    [ObservableProperty]
    public partial double P7Coefficient { get; set; }

    [ObservableProperty]
    public partial double P8Coefficient { get; set; }

    [ObservableProperty]
    public partial double StartP2Coefficient { get; set; }

    [ObservableProperty]
    public partial double StepP2Coefficient { get; set; } = 0.02;

    [ObservableProperty]
    public partial double StopP2Coefficient { get; set; }

    [ObservableProperty]
    public partial double StartP3Coefficient { get; set; }

    [ObservableProperty]
    public partial double StepP3Coefficient { get; set; } = 0.02;

    [ObservableProperty]
    public partial double StopP3Coefficient { get; set; }

    [ObservableProperty]
    public partial double StartP4Coefficient { get; set; }

    [ObservableProperty]
    public partial double StepP4Coefficient { get; set; } = 0.02;

    [ObservableProperty]
    public partial double StopP4Coefficient { get; set; }

    [ObservableProperty]
    public partial double StartP5Coefficient { get; set; }

    [ObservableProperty]
    public partial double StepP5Coefficient { get; set; } = 0.001;

    [ObservableProperty]
    public partial double StopP5Coefficient { get; set; }

    [ObservableProperty]
    public partial double StartP6Coefficient { get; set; }

    [ObservableProperty]
    public partial double StepP6Coefficient { get; set; } = 0.001;

    [ObservableProperty]
    public partial double StopP6Coefficient { get; set; }

    [ObservableProperty]
    public partial double StartP7Coefficient { get; set; }

    [ObservableProperty]
    public partial double StepP7Coefficient { get; set; } = 0.0001;

    [ObservableProperty]
    public partial double StopP7Coefficient { get; set; }

    [ObservableProperty]
    public partial double StartP8Coefficient { get; set; }

    [ObservableProperty]
    public partial double StepP8Coefficient { get; set; } = 0.0001;

    [ObservableProperty]
    public partial double StopP8Coefficient { get; set; }

    [ObservableProperty]
    public partial int RetryTimes { get; set; } = 10;

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial ChirpAODWaveformTrainingItem SelectedItem { get; set; } = new();

    [ObservableProperty]
    public partial ChirpAODWaveformTrainingItem Item { get; set; } = new();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<ChirpAODWaveformTrainingItem> Items { get; set; } = [];

    partial void OnItemChanged(ChirpAODWaveformTrainingItem value)
    {
        if (ReferenceEquals(value, null)) return;

        P2Coefficient = value.P2Coefficient;
        P3Coefficient = value.P3Coefficient;
        P4Coefficient = value.P4Coefficient;
        P5Coefficient = value.P5Coefficient;
        P6Coefficient = value.P6Coefficient;
        P7Coefficient = value.P7Coefficient;
        P8Coefficient = value.P8Coefficient;
    }

    public object ToHtmlAnonymous() => new
    {
        ProductivityInformation,
        LaserLightInformation,
        GeneratePrescanAODWaveformParam = new HtmlQuote(GeneratePrescanAODWaveformParam.ToHtmlAnonymous()),
        GenerateChirpAODWaveformParam = new HtmlQuote(GenerateChirpAODWaveformParam.ToHtmlAnonymous()),
        CIBInformation = new HtmlQuote(CIBInformation.ToHtmlAnonymous()),
        OpticsConfiguration = new HtmlQuote(OpticsConfiguration.ToHtmlAnonymous()),
        CIBConfiguration = new HtmlQuote(CIBConfiguration.ToHtmlAnonymous()),
        DSWMachinePosition,
        ScanLength,
        CenterECS,
        RangeECS,
        IsConfirmBestYStrehlRatioResult,
        P2Coefficient,
        P3Coefficient,
        P4Coefficient,
        P5Coefficient,
        P6Coefficient,
        P7Coefficient,
        P8Coefficient,
        StartP2Coefficient,
        StepP2Coefficient,
        StopP2Coefficient,
        StartP3Coefficient,
        StepP3Coefficient,
        StopP3Coefficient,
        StartP4Coefficient,
        StepP4Coefficient,
        StopP4Coefficient,
        StartP5Coefficient,
        StepP5Coefficient,
        StopP5Coefficient,
        StartP6Coefficient,
        StepP6Coefficient,
        StopP6Coefficient,
        StartP7Coefficient,
        StepP7Coefficient,
        StopP7Coefficient,
        StartP8Coefficient,
        StepP8Coefficient,
        StopP8Coefficient,
        RetryTimes
    };
}