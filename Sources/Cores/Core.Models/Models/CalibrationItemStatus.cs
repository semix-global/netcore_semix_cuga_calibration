using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Models.Models;

public partial class CalibrationItemStatus : ObservableObject
{
    public bool IsOk => Progress >= 1;

    public double Progress => TotalCalibrationCount == 0d ? 0d : (CalibratedCount + ReviewCount) / (TotalCalibrationCount * 2d);

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