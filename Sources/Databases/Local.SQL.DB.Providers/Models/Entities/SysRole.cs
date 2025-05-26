using FreeSql.DataAnnotations;
using Local.SQL.DB.Providers.Models.Entities.Base;
using Local.SQL.DB.Providers.Models.Enums;

namespace Local.SQL.DB.Providers.Models.Entities;

/// <summary>
/// 系统角色表
/// </summary>
[Table(Name = nameof(SysRole))]
[Index($"idx_{nameof(SysRole)}_{nameof(Name)}", $"{nameof(Name)}", true)]
public sealed class SysRole : EntityBase
{
    /// <summary>
    /// 角色名称
    /// </summary>
    [Column(IsNullable = false, StringLength = 64)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 数据范围
    /// </summary>
    public DataScopeEnum DataScopeEnum { get; set; }

    /// <summary>
    /// 显示顺序
    /// </summary>
    public int OrderNum { get; set; }

    #region 导航属性

    /// <summary>
    /// 部门列表
    /// </summary>
    [Navigate(ManyToMany = typeof(SysRoleDept))]
    public List<SysDept> SysDeptList { get; set; } = [];

    /// <summary>
    /// 菜单列表
    /// </summary>
    [Navigate(ManyToMany = typeof(SysRoleMenu))]
    public List<SysMenu> SysMenuList { get; set; } = [];

    /// <summary>
    /// 用户列表
    /// </summary>
    [Navigate(ManyToMany = typeof(SysUserRole))]
    public List<SysUser> SysUserList { get; set; } = [];

    #endregion 导航属性
}