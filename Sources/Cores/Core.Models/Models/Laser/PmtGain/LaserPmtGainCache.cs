using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.PmtGain;

public sealed partial class LaserPmtGainCache : CalibrationCacheBase
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum = OpticsMagTypeEnum.High;

    [ObservableProperty]
    private double _pmtProtectValue = 4000;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private double _voltageMin = -10;

    [ObservableProperty]
    private double _voltageMax = 10;

    [ObservableProperty]
    private double _voltageInterval = 0.04;

    [ObservableProperty]
    private string _powerSettings = "1,7,10,14,24,38,52,67,78,99,127,157,170";

    [ObservableProperty]
    private string _pmtList = "1,2,3,4,5,6,7,8,9,10,11,12,13,14,15";

    [ObservableProperty]
    private string _channelList = "1,2,3";

    [ObservableProperty]
    private double _voltageAging = 1;

    [ObservableProperty]
    private double _ratioAging = 50;

    [ObservableProperty]
    public int _lineValue = 1000;

    [ObservableProperty]
    public double _minValue = -5;

    [ObservableProperty]
    public double _maxValue = 5;

    [ObservableProperty]
    public int _waitTime = 5;
}