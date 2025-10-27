using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;

namespace Core.Models.Models.Laser.Attenuator;

public sealed partial class LaserAttenuatorCache : CalibrationCacheBase
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private double _coefficientStep = 0.02;

    [ObservableProperty]
    private double _waitTime = 15;
}