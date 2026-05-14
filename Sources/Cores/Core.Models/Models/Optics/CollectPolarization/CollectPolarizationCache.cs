using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;


namespace Core.Models.Models.Optics.CollectPolarization;

public sealed partial class CollectPolarizationCache : CalibrationCacheBase<CollectPolarizationCache>
{
    [ObservableProperty]
    public partial Point HazeWaferPosition { get; set; }

    [ObservableProperty]
    public partial double FindAngleMin { get; set; } = 1;

    [ObservableProperty]
    public partial double FindAngleMax { get; set; } = 180;

    [ObservableProperty]
    public partial double FindAngleInterval { get; set; } = 2;

    [ObservableProperty]
    public partial int ImageWidth { get; set; } = 1000;

    [ObservableProperty]
    public partial Point[] PolarizationPositionNDFSListCH1 { get; set; } = [];

    [ObservableProperty]
    public partial Point[] PolarizationPositionNDFSListCH2 { get; set; } = [];

    [ObservableProperty]
    public partial Point[] PolarizationPositionNDFSListCH3 { get; set; } = [];

    [ObservableProperty]
    public partial double PolarizationPositionNDFSCH1 { get; set; }

    [ObservableProperty]
    public partial double PolarizationPositionNDFSCH2 { get; set; }

    [ObservableProperty]
    public partial double PolarizationPositionNDFSCH3 { get; set; }

    [ObservableProperty]
    public partial Point[] PolarizationPositionNDFPListCH1 { get; set; } = [];

    [ObservableProperty]
    public partial Point[] PolarizationPositionNDFPListCH2 { get; set; } = [];

    [ObservableProperty]
    public partial Point[] PolarizationPositionNDFPListCH3 { get; set; } = [];

    [ObservableProperty]
    public partial double PolarizationPositionNDFPCH1 { get; set; }

    [ObservableProperty]
    public partial double PolarizationPositionNDFPCH2 { get; set; }

    [ObservableProperty]
    public partial double PolarizationPositionNDFPCH3 { get; set; }

    [ObservableProperty]
    public partial LaserLightInformation LaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial CIBInformation CIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial CIBConfiguration CIBConfiguration { get; set; } = new();

    public override CollectPolarizationCache Clone() => new()
    {
        HazeWaferPosition = HazeWaferPosition,
        FindAngleMin = FindAngleMin,
        FindAngleMax = FindAngleMax,
        FindAngleInterval = FindAngleInterval,
        ImageWidth = ImageWidth,
        PolarizationPositionNDFSListCH1 = [.. PolarizationPositionNDFSListCH1],
        PolarizationPositionNDFSListCH2 = [.. PolarizationPositionNDFSListCH2],
        PolarizationPositionNDFSListCH3 = [.. PolarizationPositionNDFSListCH3],
        PolarizationPositionNDFSCH1 = PolarizationPositionNDFSCH1,
        PolarizationPositionNDFSCH2 = PolarizationPositionNDFSCH2,
        PolarizationPositionNDFSCH3 = PolarizationPositionNDFSCH3,
        PolarizationPositionNDFPListCH1 = [.. PolarizationPositionNDFPListCH1],
        PolarizationPositionNDFPListCH2 = [.. PolarizationPositionNDFPListCH2],
        PolarizationPositionNDFPListCH3 = [.. PolarizationPositionNDFPListCH3],
        PolarizationPositionNDFPCH1 = PolarizationPositionNDFPCH1,
        PolarizationPositionNDFPCH2 = PolarizationPositionNDFPCH2,
        PolarizationPositionNDFPCH3 = PolarizationPositionNDFPCH3,
        LaserLightInformation = LaserLightInformation.Clone(),
        CIBInformation = CIBInformation.Clone(),
        CIBConfiguration = CIBConfiguration.Clone(),
        AlgorithmTemplateTypeEnum = AlgorithmTemplateTypeEnum,
        AlgorithmTemplateSizeEnum = AlgorithmTemplateSizeEnum,
        Id = Id,
        Expiration = Expiration
    };
}