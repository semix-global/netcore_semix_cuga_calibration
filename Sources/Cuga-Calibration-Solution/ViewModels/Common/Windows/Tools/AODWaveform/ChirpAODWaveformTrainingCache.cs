using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
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
    public partial IReadOnlyList<GenerateAODWaveformSlopeDeltaKConfiguration> SlopeDeltaKConfigurations { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<ChirpAODWaveformTrainingDeltaK> ChirpAODWaveformTrainingDeltaKs { get; set; } = [];

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
        SlopeDeltaKConfigurations = [..value.SlopeDeltaKConfigurations.Select(t => t.Clone())];
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
        ChirpAODWaveformTrainingDeltaKs = new HtmlTable([.. ChirpAODWaveformTrainingDeltaKs.Select(t => t.ToHtmlAnonymous())]),
        RetryTimes
    };
}

public sealed partial class ChirpAODWaveformTrainingDeltaK : ObservableObject, ICloneable<ChirpAODWaveformTrainingDeltaK>
{
    [ObservableProperty]
    public partial double StartDeltaKRate { get; set; }

    [ObservableProperty]
    public partial double StepDeltaKRate { get; set; }

    [ObservableProperty]
    public partial double StopDeltaKRate { get; set; }

    public ChirpAODWaveformTrainingDeltaK Clone() => new()
    {
        StartDeltaKRate = StartDeltaKRate,
        StepDeltaKRate = StepDeltaKRate,
        StopDeltaKRate = StopDeltaKRate
    };

    public object ToHtmlAnonymous() => new
    {
        StartDeltaKRate,
        StepDeltaKRate,
        StopDeltaKRate
    };
}