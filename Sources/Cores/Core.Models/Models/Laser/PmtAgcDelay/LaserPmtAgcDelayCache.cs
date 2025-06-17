using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Optics;
using Net.Utilities.Models;

namespace Core.Models.Models.Laser.PmtAgcDelay;

public sealed partial class LaserPmtAgcDelayCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private double _coefficient = 1.0d;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private string _pmtGainFilePath = string.Empty;
}