using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Models;

namespace Core.Models.Models.Ads.PressureGains;

public sealed partial class AdsPressureGainsCache : CalibrationCacheBase
{
    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private double _threshold;
}