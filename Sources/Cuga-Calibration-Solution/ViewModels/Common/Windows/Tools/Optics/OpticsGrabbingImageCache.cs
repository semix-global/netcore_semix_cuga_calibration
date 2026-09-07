using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Nlog.Entities.HtmlElements;

namespace CugaCalibration.ViewModels.Common.Windows.Tools.Optics;

public partial class OpticsGrabbingImageCache : ObservableCacheBase
{
    [ObservableProperty]
    public partial ProductivityInformation ProductivityInformation { get; set; } = ProductivityInformation.Default;

    [ObservableProperty]
    public partial StageCoordinateSystemEnum StageCoordinateSystemEnum { get; set; } = StageCoordinateSystemEnum.Bright;

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 800;

    [ObservableProperty]
    public partial double ScanLength { get; set; } = 5100;

    [ObservableProperty]
    public partial int ColumnCount { get; set; } = 5;

    [ObservableProperty]
    public partial double ColumnWidth { get; set; } = 15300;

    [ObservableProperty]
    public partial double CenterECS { get; set; }

    [ObservableProperty]
    public partial double RangeECS { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<CIBInformation> CIBInformations { get; set; } = [];

    [ObservableProperty]
    public partial CalChipSiteModelEnum CalChipSiteModelEnum { get; set; } = CalChipSiteModelEnum.ChuckModel;

    [ObservableProperty]
    public partial OpticsConfiguration OpticsConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = null!;

    [ObservableProperty]
    public partial bool IsForward { get; set; } = true;

    [ObservableProperty]
    public partial bool IsAutoFocus { get; set; } = true;

    [ObservableProperty]
    public partial double ECS { get; set; } = 6000;

    [ObservableProperty]
    public partial bool IsGenerateAODWaveform { get; set; }

    [ObservableProperty]
    public partial GeneratePrescanAODWaveformParam GeneratePrescanAODWaveformParam { get; set; } = new();

    [ObservableProperty]
    public partial string PrescanAODWaveformResultFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<PrescanAODWaveformProfile> PrescanAODWaveformProfiles { get; set; } = [];

    [ObservableProperty]
    public partial GenerateChirpAODWaveformParam GenerateChirpAODWaveformParam { get; set; } = new();

    [ObservableProperty]
    public partial string ChirpAODWaveformResultFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<ChirpAODWaveformProfile> ChirpAODWaveformProfiles { get; set; } = [];

    public object ToHtmlAnonymous(OpticsGrabbingImageTypeEnum opticsGrabbingImageTypeEnum) =>
        opticsGrabbingImageTypeEnum switch
        {
            OpticsGrabbingImageTypeEnum.Width => new
            {
                ProductivityInformation,
                StageCoordinateSystemEnum,
                ImageWidth,
                CIBInformations = string.Join(", ", CIBInformations),
                CalChipSiteModelEnum,
                OpticsConfiguration = new HtmlQuote(OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(CIBConfiguration.ToHtmlAnonymous()),
                LaserLightInformation,
                IsForward,
                IsAutoFocus,
                ECS
            },
            OpticsGrabbingImageTypeEnum.PTP => new
            {
                ProductivityInformation,
                StageCoordinateSystemEnum,
                ScanLength,
                CIBInformations = string.Join(", ", CIBInformations),
                CalChipSiteModelEnum,
                OpticsConfiguration = new HtmlQuote(OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(CIBConfiguration.ToHtmlAnonymous()),
                LaserLightInformation,
                IsForward,
                IsAutoFocus,
                ECS
            },
            OpticsGrabbingImageTypeEnum.PEG => new
            {
                ProductivityInformation,
                StageCoordinateSystemEnum,
                ImageWidth,
                ColumnCount,
                ColumnWidth,
                CIBInformations = string.Join(", ", CIBInformations),
                CalChipSiteModelEnum,
                OpticsConfiguration = new HtmlQuote(OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(CIBConfiguration.ToHtmlAnonymous()),
                LaserLightInformation,
                IsForward,
                IsAutoFocus,
                ECS
            },
            OpticsGrabbingImageTypeEnum.XZSync => new
            {
                ProductivityInformation,
                StageCoordinateSystemEnum,
                ScanLength,
                CenterECS,
                RangeECS,
                CIBInformations = string.Join(", ", CIBInformations),
                CalChipSiteModelEnum,
                OpticsConfiguration = new HtmlQuote(OpticsConfiguration.ToHtmlAnonymous()),
                CIBConfiguration = new HtmlQuote(CIBConfiguration.ToHtmlAnonymous()),
                LaserLightInformation,
                IsForward
            },
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<object>(nameof(opticsGrabbingImageTypeEnum))
        };

    public enum OpticsGrabbingImageTypeEnum
    {
        Width,
        PTP,
        PEG,
        XZSync
    }
}