using FreeSql.DataAnnotations;
using Local.SQL.DB.Providers.Models.Entities.Base;
using Net.Utilities.Constants;
using Net.Utilities.Helper.Json;
using Newtonsoft.Json;

namespace Local.SQL.DB.Providers.Models.Entities;

/// <summary>
/// 部门表
/// </summary>
[Table(Name = nameof(SysDept))]
[Index($"idx_{nameof(SysDept)}_{nameof(ParentId)}_{nameof(Name)}", $"{nameof(ParentId)},{nameof(Name)}", true)]
public sealed class SysDept : EntityBase
{
    /// <summary>
    /// 部门名称
    /// </summary>
    [Column(IsNullable = false, StringLength = 64)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 父部门ID
    /// </summary>
    [JsonConverter(typeof(JsonConverterUtil.LongJsonConverter))]
    public long ParentId { get; set; } = ConstantHelper.NegValue;

    /// <summary>
    /// 显示顺序
    /// </summary>
    public int OrderNum { get; set; }

    /// <summary>
    /// 负责人
    /// </summary>
    public long LeaderUserId { get; set; }

    #region 导航属性

    /// <summary>
    /// 父部门
    /// </summary>
    [Navigate(nameof(ParentId))]
    public SysDept? Parent { get; set; }

    /// <summary>
    /// 子部门列表
    /// </summary>
    [Navigate(nameof(ParentId))]
    public List<SysDept> ChildList { get; set; } = [];

    /// <summary>
    /// 用户列表
    /// </summary>
    [Navigate(nameof(SysUser.DeptId))]
    public List<SysUser> SysUserList { get; set; } = [];

    /// <summary>
    /// 角色列表
    /// </summary>
    [Navigate(ManyToMany = typeof(SysRoleDept))]
    public List<SysRole> SysRoleList { get; set; } = [];

    #endregion 导航属性
}