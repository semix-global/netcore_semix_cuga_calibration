using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.DB.Providers.Models.Entities.DTO;

namespace CugaCalibration.Core.Models;

/// <summary>
/// 校准的菜单
/// </summary>
public sealed partial class CalibrationMenu : ObservableObject
{
    [ObservableProperty]
    private SysMenuDto _sysMenuDto = new();

    [ObservableProperty]
    private bool _isCalibrated;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private List<CalibrationMenu> _childList = [];
}