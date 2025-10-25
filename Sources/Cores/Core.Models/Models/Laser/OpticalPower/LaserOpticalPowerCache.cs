using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.OpticalPower;

public sealed partial class LaserOpticalPowerCache : CalibrationCacheBase
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private int _rowNumber = 11;

    [ObservableProperty]
    private int _columnNumber = 11;

    [ObservableProperty]
    private double _columnCellWidth = 100;

    [ObservableProperty]
    private double _rowCellHeight = 100;

    [ObservableProperty]
    private double _waitTime = 15;

    [ObservableProperty]
    private int _repeatCount = 5;

    [ObservableProperty]
    private double _threshold = 0.05;
}