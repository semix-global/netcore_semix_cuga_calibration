using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Models.Models.Common.Cookies;

/// <summary>
/// 系统管理菜单（权限管理...）
/// </summary>
public sealed partial class SystemManageMenu : ObservableObject
{
    [ObservableProperty]
    public partial int Id { get; set; }

    [ObservableProperty]
    public partial string DisplayName { get; set; } = "NaA";

    [ObservableProperty]
    public partial int OrderNum { get; set; }

    [ObservableProperty]
    public partial string Component { get; set; } = "NaA";

    [ObservableProperty]
    public partial List<SystemManageMenu> ChildList { get; set; } = [];
}