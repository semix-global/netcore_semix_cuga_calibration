using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Local.SQL.DB.Providers.Models.Enums;
using Net.Utilities.Mapper.Interfaces;

namespace Local.SQL.DB.Providers.Models.Entities.DTO;

public sealed partial class SysRoleDto : SysBaseDto, ICloneable<SysRoleDto>, IAdaptTo<SysRole>, IAdaptIn<SysRole, SysRoleDto>
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private DataScopeEnum _dataScopeEnum;

    [ObservableProperty]
    private int _roleSort;

    [ObservableProperty]
    private bool _isEnabled;

    public bool IsAdmin => Id == 1;

    #region 导航属性

    [ObservableProperty]
    private List<SysDeptDto> _sysDeptList = [];

    [ObservableProperty]
    private List<SysMenuDto> _sysMenuList = [];

    [ObservableProperty]
    private List<SysUserDto> _sysUserList = [];

    #endregion 导航属性

    #region Mapper

    // 自定义的深度克隆方法
    public SysRoleDto Clone()
    {
        return new SysRoleDto
        {
            Name = Name,
            DataScopeEnum = DataScopeEnum,
            RoleSort = RoleSort,
            IsEnabled = IsEnabled,
            SysUserList = [.. SysUserList.Select(t => t.Clone())],
            SysDeptList = [.. SysDeptList.Select(t => t.Clone())],
            SysMenuList = [.. SysMenuList.Select(t => t.Clone())],
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

    public SysRole AdaptTo() => new()
    {
        Name = Name,
        DataScopeEnum = DataScopeEnum,
        IsEnabled = IsEnabled,
        SysDeptList = [.. SysDeptList.Select(t => t.AdaptTo())],
        SysUserList = [.. SysUserList.Select(t => t.AdaptTo())],
        SysMenuList = [.. SysMenuList.Select(t => t.AdaptTo())],
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

    public SysRoleDto AdaptIn(SysRole obj)
    {
        Guard.IsNotNull(obj, nameof(obj));

        Name = obj.Name;
        DataScopeEnum = obj.DataScopeEnum;
        IsEnabled = obj.IsEnabled;
        SysDeptList = [.. obj.SysDeptList.Select(t => new SysDeptDto().AdaptIn(t))];
        SysUserList = [.. obj.SysUserList.Select(t => new SysUserDto().AdaptIn(t))];
        SysMenuList = [.. obj.SysMenuList.Select(t => new SysMenuDto().AdaptIn(t))];
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