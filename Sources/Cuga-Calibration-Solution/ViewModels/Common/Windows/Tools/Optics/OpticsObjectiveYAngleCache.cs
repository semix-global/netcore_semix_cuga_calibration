using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Optics;

public sealed partial class OpticsObjectiveYAngleCache : ObservableCacheBase
{
    #region Param

    [ObservableProperty]
    public partial double PrescanFrequency { get; set; }

    [ObservableProperty]
    public partial GeneratePrescanAODWaveformParam GeneratePrescanAODWaveformParam { get; set; } = new();

    [ObservableProperty]
    public partial double ChirpFrequency { get; set; }

    [ObservableProperty]
    public partial GenerateChirpAODWaveformParam GenerateChirpAODWaveformParam { get; set; } = new();

    [ObservableProperty]
    public partial ProductivityInformation HazeProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial Point HazeBFMachinePosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial LaserLightInformation HazeLaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial ProductivityInformation ShinyWaferProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial Point ShinyWaferBFMachinePosition { get; set; } = Point.Origin;

    [ObservableProperty]
    public partial LaserLightInformation ShinyWaferLaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial int ChannelId { get; set; } = CalibrationConstantsHelper.MainChannelId;

    [ObservableProperty]
    public partial double RotateAngle { get; set; } = 0d;

    [ObservableProperty]
    public partial double ScanLength { get; set; } = 20d;

    /// <summary>
    /// 最大半角度(°)
    /// </summary>
    [ObservableProperty]
    public partial double CenterChannelLensMaximumAngle { get; set; } = 84.13;

    [ObservableProperty]
    public partial double Threshold { get; set; } = 2d;

    #endregion

    [ObservableProperty]
    public partial string ChirpAODWaveformResultFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<PrescanAODWaveformProfile> PrescanAODWaveformProfiles { get; set; } = [];

    [ObservableProperty]
    public partial string PrescanAODWaveformResultFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<ChirpAODWaveformProfile> ChirpAODWaveformProfiles { get; set; } = [];

    [ObservableProperty]
    public partial OpticsObjectiveYAngleResult Result { get; set; } = new();

    public object ToHtmlAnonymous() => new
    {
        ChannelId,
        ScanLength,
        PrescanFrequency,
        GeneratePrescanAODWaveformParam = new HtmlQuote(GeneratePrescanAODWaveformParam.ToFlatnessHtmlAnonymous()),
        ChirpFrequency,
        GenerateChirpAODWaveformParam = new HtmlQuote(GenerateChirpAODWaveformParam.ToFlatnessHtmlAnonymous()),
        HazeProductivityInformation,
        HazeBFMachinePosition,
        HazeLaserLightInformation,
        ShinyWaferProductivityInformation,
        ShinyWaferBFMachinePosition,
        ShinyWaferLaserLightInformation,
        Threshold
    };
}