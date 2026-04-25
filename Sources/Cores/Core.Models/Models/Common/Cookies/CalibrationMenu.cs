using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.DB.Providers.Models.Entities.DTO;

namespace Core.Models.Models.Common.Cookies;

/// <summary>
/// 校准的菜单
/// </summary>
public sealed partial class CalibrationMenu : ObservableObject
{
    [ObservableProperty]
    private SysMenuDTO _sysMenuDTO = new();

    [ObservableProperty]
    private bool _isCalibrated;

    [ObservableProperty]
    private List<CalibrationMenu> _childList = [];
}