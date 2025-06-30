using FreeSql.DataAnnotations;
using Local.SQL.DB.Providers.Models.Entities.Base.Implements;
using Net.Utilities.Helpers;
using Newtonsoft.Json;

namespace Local.SQL.DB.Providers.Models.Entities;

/// <summary>
/// 系统用户岗位表
/// </summary>
[Table(Name = nameof(SysUserPost))]
[Index($"idx_{nameof(SysUserPost)}_{nameof(UserId)}_{nameof(PostId)}", $"{nameof(UserId)},{nameof(PostId)}", true)]
public sealed class SysUserPost : EntityAdd
{
    /// <summary>
    /// 用户Id
    /// </summary>
    [Column(IsPrimary = true)]
    [JsonConverter(typeof(JsonConverterUtils.LongJsonConverter))]
    public long UserId { get; init; }

    /// <summary>
    /// 岗位Id
    /// </summary>
    [Column(IsPrimary = true)]
    [JsonConverter(typeof(JsonConverterUtils.LongJsonConverter))]
    public long PostId { get; init; }

    #region 导航属性

    /// <summary>
    /// 用户
    /// </summary>
    [Navigate(nameof(UserId))]
    public SysUser SysUser { get; set; } = null!;

    /// <summary>
    /// 岗位
    /// </summary>
    [Navigate(nameof(PostId))]
    public SysPost SysPost { get; set; } = null!;

    #endregion 导航属性
}