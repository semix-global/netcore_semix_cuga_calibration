using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Models.Models.Common.Cookies;

public partial class CalibrationViewModelStatus : ObservableObject
{
    public bool IsOk => 2 * TotalCalibrationCount > 0 && CalibratedCount + ReviewCount == 2 * TotalCalibrationCount;

    public bool IsCalibrated => TotalCalibrationCount > 0 && CalibratedCount == TotalCalibrationCount;

    public double Progress => TotalCalibrationCount > 0
        ? (CalibratedCount + ReviewCount) / (2d * TotalCalibrationCount) * 100d
        : 0d;

    public int NotOkCalibratedCount => TotalCalibrationCount - CalibratedCount;

    public int NotOkReviewCount => TotalCalibrationCount - ReviewCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOk))]
    [NotifyPropertyChangedFor(nameof(IsCalibrated))]
    [NotifyPropertyChangedFor(nameof(Progress))]
    public partial int TotalCalibrationCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOk))]
    [NotifyPropertyChangedFor(nameof(IsCalibrated))]
    [NotifyPropertyChangedFor(nameof(Progress))]
    public partial int CalibratedCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOk))]
    [NotifyPropertyChangedFor(nameof(IsCalibrated))]
    [NotifyPropertyChangedFor(nameof(Progress))]
    public partial int ReviewCount { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Detail> Details { get; set; } = [];

    public sealed record Detail(string Item, bool? IsCalibrated, bool? IsReviewed);
}