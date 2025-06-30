using FreeSql.DataAnnotations;
using Local.SQL.DB.Providers.Models.Entities.Base.Implements;
using Net.Utilities.Helpers;
using Newtonsoft.Json;

namespace Local.SQL.DB.Providers.Models.Entities;

/// <summary>
/// 系统角色部门表
/// </summary>
[Table(Name = nameof(SysRoleDept))]
[Index($"idx_{nameof(SysRoleDept)}_{nameof(RoleId)}_{nameof(DeptId)}", $"{nameof(RoleId)},{nameof(DeptId)}", true)]
public sealed class SysRoleDept : EntityAdd
{
    /// <summary>
    /// 角色Id
    /// </summary>
    [Column(IsPrimary = true)]
    [JsonConverter(typeof(JsonConverterUtils.LongJsonConverter))]
    public long RoleId { get; init; }

    /// <summary>
    /// 部门Id
    /// </summary>
    [Column(IsPrimary = true)]
    [JsonConverter(typeof(JsonConverterUtils.LongJsonConverter))]
    public long DeptId { get; init; }

    #region 导航属性

    /// <summary>
    /// 角色
    /// </summary>
    [Navigate(nameof(RoleId))]
    public SysRole SysRole { get; set; } = null!;

    /// <summary>
    /// 部门
    /// </summary>
    [Navigate(nameof(DeptId))]
    public SysDept SysDept { get; set; } = null!;

    #endregion 导航属性
}