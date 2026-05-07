using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.Cache.Providers.Bases;

namespace Core.Models.Models;

public partial class CalibrationDTOBase : ObservableCacheBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOk))]
    public partial bool IsVerified { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOk))]
    public partial bool IsCalibrated { get; set; }

    [ObservableProperty]
    public partial bool IsRequiredSelfCheck { get; set; }

    public bool IsOk => IsCalibrated && IsVerified;
}