using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Optics;
using Net.Utilities.Models;

namespace Core.Models.Models.Laser.PmtAgcDelay;

public sealed partial class LaserPmtAgcDelayCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum = MicroscopeMagnificationEnum.Magnification5X;

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private int _catchCount = 1;

    [ObservableProperty]
    private IReadOnlyList<int> _pmtIdList = [];

    [ObservableProperty]
    private double _coefficient = 1.0d;

    [ObservableProperty]
    private string _pmtGainFilePath = string.Empty;

    [ObservableProperty]
    private int _retryCount = 5;

    [ObservableProperty]
    private int _semaphoreCount = 5;

    [ObservableProperty]
    private double _threshold = 50;

    [ObservableProperty]
    private List<double> _pmtGains = [];
}