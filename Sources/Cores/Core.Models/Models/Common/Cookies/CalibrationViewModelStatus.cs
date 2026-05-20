using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Models.Models.Common.Cookies;

public partial class CalibrationViewModelStatus : ObservableObject
{
    public bool IsOk => 2 * TotalCalibrationCount > 0 && CalibratedCount + VerifiedCount == 2 * TotalCalibrationCount;

    public bool IsCalibrated => TotalCalibrationCount > 0 && CalibratedCount == TotalCalibrationCount;

    public double Progress => TotalCalibrationCount > 0
        ? (CalibratedCount + VerifiedCount) / (2d * TotalCalibrationCount) * 100d
        : 0d;

    public int NotOkCalibratedCount => TotalCalibrationCount - CalibratedCount;

    public int NotOkVerifiedCount => TotalCalibrationCount - VerifiedCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOk))]
    [NotifyPropertyChangedFor(nameof(IsCalibrated))]
    [NotifyPropertyChangedFor(nameof(Progress))]
    [NotifyPropertyChangedFor(nameof(NotOkCalibratedCount))]
    [NotifyPropertyChangedFor(nameof(NotOkVerifiedCount))]
    public partial int TotalCalibrationCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOk))]
    [NotifyPropertyChangedFor(nameof(IsCalibrated))]
    [NotifyPropertyChangedFor(nameof(Progress))]
    [NotifyPropertyChangedFor(nameof(NotOkCalibratedCount))]
    [NotifyPropertyChangedFor(nameof(NotOkVerifiedCount))]
    public partial int CalibratedCount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOk))]
    [NotifyPropertyChangedFor(nameof(IsCalibrated))]
    [NotifyPropertyChangedFor(nameof(Progress))]
    [NotifyPropertyChangedFor(nameof(NotOkCalibratedCount))]
    [NotifyPropertyChangedFor(nameof(NotOkVerifiedCount))]
    public partial int VerifiedCount { get; set; }

    [ObservableProperty]
    public partial IReadOnlyList<Detail> Details { get; set; } = [];

    public sealed record Detail(string Item, bool? IsCalibrated, bool? IsVerified);
}