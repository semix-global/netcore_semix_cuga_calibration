using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models;

namespace Local.SQL.DB.Providers.Models.Entities.DTO;

public sealed partial class SysDeptDto : SysBaseDto, ICloneable<SysDeptDto>, IAdaptTo<SysDept>, IAdaptIn<SysDept, SysDeptDto>
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private long _parentId = Constants.NegInt32Value;

    [ObservableProperty]
    private int _orderNum;

    [ObservableProperty]
    private string _leader = string.Empty;

    [ObservableProperty]
    private long _leaderUserId;

    [ObservableProperty]
    private bool? _isEnabled;

    /// <summary>
    /// treeview启用checkbox时可以使用该属性
    /// </summary>
    [ObservableProperty]
    private bool? _isSelected = false;

    /// <summary>
    /// 当前节点层数，自定义控件treeview样式使用
    /// </summary>
    [ObservableProperty]
    private int? _depth = 0;

    #region 导航属性

    [ObservableProperty]
    private SysDeptDto? _parent;

    [ObservableProperty]
    private List<SysDeptDto> _childList = [];

    [ObservableProperty]
    private List<SysUserDto> _sysUserList = [];

    [ObservableProperty]
    private List<SysRoleDto> _sysRoleList = [];

    #endregion 导航属性

    #region Mapper

    // 自定义的深度克隆方法
    public SysDeptDto Clone()
    {
        return new SysDeptDto
        {
            Name = Name,
            ParentId = ParentId,
            OrderNum = OrderNum,
            Leader = Leader,
            LeaderUserId = LeaderUserId,
            IsEnabled = IsEnabled,
            IsSelected = IsSelected,
            Depth = Depth,
            Parent = Parent?.Clone(),
            ChildList = [.. ChildList.Select(t => t.Clone())],
            SysUserList = [.. SysUserList.Select(t => t.Clone())],
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

    public SysDept AdaptTo() => new()
    {
        Name = Name,
        ParentId = ParentId,
        OrderNum = OrderNum,
        LeaderUserId = LeaderUserId,
        Parent = Parent?.AdaptTo(),
        ChildList = [.. ChildList.Select(t => t.AdaptTo())],
        SysUserList = [.. SysUserList.Select(t => t.AdaptTo())],
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

    public SysDeptDto AdaptIn(SysDept obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        Name = obj.Name;
        ParentId = obj.ParentId;
        OrderNum = obj.OrderNum;
        LeaderUserId = obj.LeaderUserId;
        Parent = obj.Parent is not null ? new SysDeptDto().AdaptIn(obj.Parent) : null;
        ChildList = [.. obj.ChildList.Select(t => new SysDeptDto().AdaptIn(t))];
        SysUserList = [.. obj.SysUserList.Select(t => new SysUserDto().AdaptIn(t))];
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