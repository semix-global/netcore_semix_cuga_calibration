using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.DB.Providers.Models.Entities.DTO;

namespace Core.Models.Models.Common.Cookies;

/// <summary>
/// 校准的菜单
/// </summary>
public sealed partial class CalibrationMenu : ObservableObject
{
    [ObservableProperty]
    public partial SysMenuDTO SysMenu { get; set; } = new();

    [ObservableProperty]
    public partial CalibrationItemStatus Status { get; set; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<CalibrationMenu> Children { get; set; } = [];
}