using FreeSql.DataAnnotations;
using Local.SQL.DB.Providers.Models.Entities.Base.Implements;
using Net.Utilities.Helpers;
using Newtonsoft.Json;

namespace Local.SQL.DB.Providers.Models.Entities;

/// <summary>
/// 系统角色菜单表
/// </summary>
[Table(Name = nameof(SysRoleMenu))]
[Index($"idx_{nameof(SysRoleMenu)}_{nameof(RoleId)}_{nameof(MenuId)}", $"{nameof(RoleId)},{nameof(MenuId)}", true)]
public sealed class SysRoleMenu : EntityAdd
{
    /// <summary>
    /// 角色Id
    /// </summary>
    [Column(IsPrimary = true)]
    [JsonConverter(typeof(JsonConverterUtils.LongJsonConverter))]
    public long RoleId { get; init; }

    /// <summary>
    /// 菜单Id
    /// </summary>
    [Column(IsPrimary = true)]
    [JsonConverter(typeof(JsonConverterUtils.LongJsonConverter))]
    public long MenuId { get; init; }

    #region 导航属性

    /// <summary>
    /// 角色
    /// </summary>
    [Navigate(nameof(RoleId))]
    public SysRole SysRole { get; set; } = null!;

    /// <summary>
    /// 菜单
    /// </summary>
    [Navigate(nameof(MenuId))]
    public SysMenu SysMenu { get; set; } = null!;

    #endregion 导航属性
}