using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models;

namespace Core.Models.Models.Laser.AutoFocus;

public sealed partial class LaserAutoFocusCache : CalibrationCacheBase
{
    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private double _thresholdFMin = 6500;

    [ObservableProperty]
    private double _thresholdFMax = 8000;

    [ObservableProperty]
    private double _thresholdNMin = 13000;

    [ObservableProperty]
    private double _thresholdNMax = 16000;

    [ObservableProperty]
    private double _thresholdCurrentMin;

    [ObservableProperty]
    private double _thresholdCurrentMax = 550;

    [ObservableProperty]
    private double _verifyResultFa;

    [ObservableProperty]
    private double _verifyResultFb;

    [ObservableProperty]
    private double _verifyResultNa;

    [ObservableProperty]
    private double _verifyResultNb;

    [ObservableProperty]
    private double _findInterval;

    [ObservableProperty]
    private bool _isA;
}