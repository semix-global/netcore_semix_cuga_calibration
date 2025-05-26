using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Local.SQL.DB.Providers.Models.Entities.Base.Interface;

namespace Core.Models.Models;

public partial class CalibrationDtoBase : ObservableCacheBase, IEntityAdd
{
    [ObservableProperty]
    private bool _isVerified;

    [ObservableProperty]
    private bool _isCalibrated;

    /// <summary>
    /// Cuga初始化是否需要自检此项校准结果是否Ok
    /// </summary>
    [ObservableProperty]
    private bool _isRequiredSelfCheck = false;

    [ObservableProperty]
    private long _createdUserId;

    [ObservableProperty]
    private string _createdUserName = string.Empty;

    public bool IsOk => IsCalibrated && IsVerified;
}