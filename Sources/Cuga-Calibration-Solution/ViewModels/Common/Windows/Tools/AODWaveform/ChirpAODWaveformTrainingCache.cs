using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Alignment;
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
    public partial AlignmentResultDto AlignmentResult { get; set; } = new();

    [ObservableProperty]
    public partial MicroscopeLensInformation MicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial Point DSWFindBFMachinePosition { get; set; }

    [ObservableProperty]
    public partial double ScanLength { get; set; }

    [ObservableProperty]
    public partial double CenterECS { get; set; }

    [ObservableProperty]
    public partial double RangeECS { get; set; }

    [ObservableProperty]
    public partial bool IsConfirmBestYStrehlRatioResult { get; set; } = true;

    [ObservableProperty]
    public partial IReadOnlyList<GenerateAODWaveformSlopeConfiguration> SlopeConfigurations { get; set; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<ChirpAODWaveformTrainingSlope> ChirpAODWaveformTrainingSlopes { get; set; } = [];

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial ChirpAODWaveformTrainingItem SelectedItem { get; set; } = new();

    [ObservableProperty]
    public partial ChirpAODWaveformTrainingItem Item { get; set; } = new();

    [ObservableProperty]
    [Newtonsoft.Json.JsonIgnore]
    public partial IReadOnlyList<ChirpAODWaveformTrainingItem> Items { get; set; } = [];

    partial void OnChirpAODWaveformTrainingSlopesChanged(IReadOnlyList<ChirpAODWaveformTrainingSlope> value)
    {
        SlopeConfigurations =
        [
            ..value.Select(_ => new GenerateAODWaveformSlopeConfiguration
            {
                DeltaKRate = 0d,
                Coefficient = 0d
            })
        ];
    }

    partial void OnItemChanged(ChirpAODWaveformTrainingItem value)
    {
        SlopeConfigurations = [..value.SlopeConfigurations.Select(t => t.Clone())];
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
        AlignmentResult = new HtmlQuote(AlignmentResult.ToHtmlAnonymous()),
        MicroscopeLensInformation,
        DSWFindBFMachinePosition,
        ScanLength,
        CenterECS,
        RangeECS,
        IsConfirmBestYStrehlRatioResult,
        SlopeConfigurations = new HtmlTable([.. SlopeConfigurations.Select(t => t.ToHtmlAnonymous())]),
        ChirpAODWaveformTrainingSlopes = new HtmlTable([.. ChirpAODWaveformTrainingSlopes.Select(t => t.ToHtmlAnonymous())]),
    };
}

public sealed partial class ChirpAODWaveformTrainingSlope : ObservableObject, ICloneable<ChirpAODWaveformTrainingSlope>
{
    [ObservableProperty]
    public partial double StartDeltaKRate { get; set; }

    [ObservableProperty]
    public partial double StepDeltaKRate { get; set; }

    [ObservableProperty]
    public partial double StopDeltaKRate { get; set; }

    public ChirpAODWaveformTrainingSlope Clone() => new()
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