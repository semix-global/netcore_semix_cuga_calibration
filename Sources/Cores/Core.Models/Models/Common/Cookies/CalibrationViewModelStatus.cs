using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Models.Models.Common.Cookies;

public partial class CalibrationViewModelStatus : ObservableObject
{
    public bool IsOk => TotalCalibrationCount > 0 && CalibratedCount + ReviewCount == 2 * TotalCalibrationCount;

    public double Progress => TotalCalibrationCount > 0
        ? (CalibratedCount + ReviewCount) / (TotalCalibrationCount * 2d) * 100d
        : 0d;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    [NotifyPropertyChangedFor(nameof(IsOk))]
    public partial int TotalCalibrationCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    [NotifyPropertyChangedFor(nameof(IsOk))]
    public partial int CalibratedCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progress))]
    [NotifyPropertyChangedFor(nameof(IsOk))]
    public partial int ReviewCount { get; set; }

    [ObservableProperty]
    public partial string XamlMessage { get; set; } = string.Empty;
}