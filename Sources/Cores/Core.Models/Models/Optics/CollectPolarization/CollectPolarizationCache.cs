using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;


namespace Core.Models.Models.Optics.CollectPolarization;

public sealed partial class CollectPolarizationCache : CalibrationCacheBase
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
    private double _polarizationPositionNDFSCH1 = 0;

    [ObservableProperty]
    private double _polarizationPositionNDFSCH2 = 0;

    [ObservableProperty]
    private double _polarizationPositionNDFSCH3 = 0;

    [ObservableProperty]
    private Point[] _polarizationPositionNDFPListCH1 = [];

    [ObservableProperty]
    private Point[] _polarizationPositionNDFPListCH2 = [];

    [ObservableProperty]
    private Point[] _polarizationPositionNDFPListCH3 = [];

    [ObservableProperty]
    private double _polarizationPositionNDFPCH1 = 0;

    [ObservableProperty]
    private double _polarizationPositionNDFPCH2 = 0;

    [ObservableProperty]
    private double _polarizationPositionNDFPCH3 = 0;

    [ObservableProperty]
    private LaserLightInformation _laserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private CIBInformation _cIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();
}