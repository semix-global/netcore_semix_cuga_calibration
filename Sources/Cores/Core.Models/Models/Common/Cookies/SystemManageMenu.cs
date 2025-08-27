using CommunityToolkit.Mvvm.ComponentModel;

namespace Core.Models.Models.Common.Cookies;

/// <summary>
/// 系统管理菜单（权限管理...）
/// </summary>
public sealed partial class SystemManageMenu : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _displayName = "NaA";

    [ObservableProperty]
    private int _orderNum;

    [ObservableProperty]
    private string _component = "NaA";

    [ObservableProperty]
    private List<SystemManageMenu> _childList = [];
}