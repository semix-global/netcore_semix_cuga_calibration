using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.DB.Providers.Models.Enums;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

namespace Local.SQL.DB.Providers.Models.Entities.DTO;

public partial class SysMenuDto : SysBaseDto, ICloneable<SysMenuDto>, IAdaptTo<SysMenu>, IAdaptIn<SysMenu, SysMenuDto>
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private long _parentId = Constants.NegInt32Value;

    [ObservableProperty]
    private int _orderNum;

    [ObservableProperty]
    private string _icon = string.Empty;

    [ObservableProperty]
    private MenuTypeEnum _menuTypeEnum;

    [ObservableProperty]
    private string _component = string.Empty;

    [ObservableProperty]
    private string? _perms = string.Empty;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private bool? _isEnabled;

    [ObservableProperty]
    private bool _isCheckable = false;

    /// <summary>
    /// 角色是否分配该菜单
    /// </summary>
    [ObservableProperty]
    private bool _isDistributed;

    /// <summary>
    /// 当前节点层数，自定义控件treeview样式使用
    /// </summary>
    [ObservableProperty]
    private int? _depth = 0;

    #region 导航属性

    [ObservableProperty]
    private SysMenuDto? _parent;

    [ObservableProperty]
    private List<SysMenuDto> _childList = [];

    [ObservableProperty]
    private List<SysRoleDto> _sysRoleList = [];

    #endregion 导航属性

    #region Mapper

    // 自定义的深度克隆方法
    public SysMenuDto Clone()
    {
        return new SysMenuDto
        {
            Name = Name,
            ParentId = ParentId,
            OrderNum = OrderNum,
            Icon = Icon,
            MenuTypeEnum = MenuTypeEnum,
            Component = Component,
            Perms = Perms,
            IsVisible = IsVisible,
            IsEnabled = IsEnabled,
            IsCheckable = IsCheckable,
            IsDistributed = IsDistributed,
            Depth = Depth,
            Parent = Parent?.Clone(),
            ChildList = [.. ChildList.Select(t => t.Clone())],
            SysRoleList = [.. SysRoleList.Select(t => t.Clone())],
            Id = Id,
            CreatedUserId = CreatedUserId,
            CreatedUserName = CreatedUserName,
            CreatedTime = CreatedTime,
            ModifiedUserId = ModifiedUserId,
            ModifiedUserName = ModifiedUserName,
            ModifiedTime = ModifiedTime,
            Remark = Remark,
            IsDeleted = IsDeleted
        };
    }

    public SysMenu AdaptTo() => new()
    {
        Name = Name,
        ParentId = ParentId,
        OrderNum = OrderNum,
        Icon = Icon,
        MenuTypeEnum = MenuTypeEnum,
        Component = Component,
        Perms = Perms,
        IsVisible = IsVisible,
        IsCheckable = IsCheckable,
        Parent = Parent?.AdaptTo(),
        ChildList = [.. ChildList.Select(t => t.AdaptTo())],
        SysRoleList = [.. SysRoleList.Select(t => t.AdaptTo())],
        Id = Id,
        CreatedUserId = CreatedUserId,
        CreatedUserName = CreatedUserName,
        CreatedTime = CreatedTime,
        ModifiedUserId = ModifiedUserId,
        ModifiedUserName = ModifiedUserName,
        ModifiedTime = ModifiedTime,
        Remark = Remark,
        IsDeleted = IsDeleted
    };

    public SysMenuDto AdaptIn(SysMenu obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        Name = obj.Name;
        ParentId = obj.ParentId;
        OrderNum = obj.OrderNum;
        Icon = obj.Icon;
        MenuTypeEnum = obj.MenuTypeEnum;
        Component = obj.Component;
        Perms = obj.Perms;
        IsVisible = obj.IsVisible;
        IsCheckable = obj.IsCheckable;
        Parent = obj.Parent is not null ? new SysMenuDto().AdaptIn(obj.Parent) : null;
        ChildList = [.. obj.ChildList.Select(t => new SysMenuDto().AdaptIn(t))];
        SysRoleList = [.. obj.SysRoleList.Select(t => new SysRoleDto().AdaptIn(t))];
        Id = obj.Id;
        CreatedUserId = obj.CreatedUserId;
        CreatedUserName = obj.CreatedUserName;
        CreatedTime = obj.CreatedTime;
        ModifiedUserId = obj.ModifiedUserId;
        ModifiedUserName = obj.ModifiedUserName;
        ModifiedTime = obj.ModifiedTime;
        Remark = obj.Remark;
        IsDeleted = obj.IsDeleted;

        return this;
    }

    #endregion Mapper
}