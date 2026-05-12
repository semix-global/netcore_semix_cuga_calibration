using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models;

public abstract partial class CalibrationDTOBase : ObservableCacheBase
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

public abstract partial class CalibrationDTOBase<T> : CalibrationDTOBase, ICloneable<T>
    where T : CalibrationDTOBase<T>
{
    public abstract T Clone();
}
