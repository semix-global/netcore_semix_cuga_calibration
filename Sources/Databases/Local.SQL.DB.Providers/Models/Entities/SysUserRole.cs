using FreeSql.DataAnnotations;
using Local.SQL.DB.Providers.Models.Entities.Base.Implements;
using Net.Utilities.Helper.Json;
using Newtonsoft.Json;

namespace Local.SQL.DB.Providers.Models.Entities;

/// <summary>
/// 系统用户角色表
/// </summary>
[Table(Name = nameof(SysUserRole))]
[Index($"idx_{nameof(SysUserRole)}_{nameof(UserId)}_{nameof(RoleId)}", $"{nameof(UserId)},{nameof(RoleId)}", true)]
public sealed class SysUserRole : EntityAdd
{
    /// <summary>
    /// 用户Id
    /// </summary>
    [Column(IsPrimary = true)]
    [JsonConverter(typeof(JsonConverterUtil.LongJsonConverter))]
    public long UserId { get; init; }

    /// <summary>
    /// 角色Id
    /// </summary>
    [Column(IsPrimary = true)]
    [JsonConverter(typeof(JsonConverterUtil.LongJsonConverter))]
    public long RoleId { get; init; }

    #region 导航属性

    /// <summary>
    /// 用户
    /// </summary>
    [Navigate(nameof(UserId))]
    public SysUser SysUser { get; set; } = null!;

    /// <summary>
    /// 权限
    /// </summary>
    [Navigate(nameof(RoleId))]
    public SysRole SysRole { get; set; } = null!;

    #endregion 导航属性
}