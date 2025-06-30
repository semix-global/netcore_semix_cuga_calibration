using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.BeamStabilizer;

public sealed partial class LaserBeamStabilizerCache : CalibrationCacheBase
{
    [ObservableProperty]
    private Point _currentPDPosition1;

    [ObservableProperty]
    private Point _currentPDPosition2;

    [ObservableProperty]
    private Point _originPosition1;

    [ObservableProperty]
    private Point _originPosition2;

    [ObservableProperty]
    private int _interval;

    [ObservableProperty]
    private int _threshold;

    [ObservableProperty]
    private int _repeatNumber = 5;
}