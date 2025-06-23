using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;

namespace Core.Models.Models.Laser.PmtAgcDelay;

public sealed partial class LaserPmtAgcDelayCache : CalibrationCacheBase
{
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum = OpticsMagTypeEnum.High;

    [ObservableProperty]
    private int _catchCount = 10;

    [ObservableProperty]
    private List<int> _pmtIdList = [];

    [ObservableProperty]
    private int _retryCount = 10;

    [ObservableProperty]
    private int _concurrentCount = 2;

    [ObservableProperty]
    private double _threshold = 5;
}