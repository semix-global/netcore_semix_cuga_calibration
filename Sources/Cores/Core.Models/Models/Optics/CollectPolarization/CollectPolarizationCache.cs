using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;


namespace Core.Models.Models.Optics.CollectPolarization;

public sealed partial class CollectPolarizationCache : CalibrationCacheBase<CollectPolarizationCache>
{
    [ObservableProperty]
    private Point _hazeWaferPosition;

    [ObservableProperty]
    private double _findAngleMin = 1;

    [ObservableProperty]
    private double _findAngleMax = 180;

    [ObservableProperty]
    private double _findAngleInterval = 2;

    [ObservableProperty]
    private int _imageWidth = 1000;

    [ObservableProperty]
    public Point[] _polarizationPositionNDFSListCH1 = [];

    [ObservableProperty]
    private Point[] _polarizationPositionNDFSListCH2 = [];

    [ObservableProperty]
    private Point[] _polarizationPositionNDFSListCH3 = [];

    [ObservableProperty]
    private double _polarizationPositionNDFSCH1;

    [ObservableProperty]
    private double _polarizationPositionNDFSCH2;

    [ObservableProperty]
    private double _polarizationPositionNDFSCH3;

    [ObservableProperty]
    private Point[] _polarizationPositionNDFPListCH1 = [];

    [ObservableProperty]
    private Point[] _polarizationPositionNDFPListCH2 = [];

    [ObservableProperty]
    private Point[] _polarizationPositionNDFPListCH3 = [];

    [ObservableProperty]
    private double _polarizationPositionNDFPCH1;

    [ObservableProperty]
    private double _polarizationPositionNDFPCH2;

    [ObservableProperty]
    private double _polarizationPositionNDFPCH3;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

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