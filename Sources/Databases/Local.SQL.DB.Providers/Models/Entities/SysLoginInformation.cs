using FreeSql.DataAnnotations;
using Local.SQL.DB.Providers.Models.Attributes;
using Local.SQL.DB.Providers.Models.Entities.Base.Implements;
using Net.Utilities.Helper.Json;
using Newtonsoft.Json;

namespace Local.SQL.DB.Providers.Models.Entities;

/// <summary>
/// 系统登录日志表
/// </summary>
[Table(Name = nameof(SysLoginInformation))]
[Index($"idx_{nameof(SysLoginInformation)}_{nameof(LoginUserId)}", nameof(LoginUserId))]
[Index($"idx_{nameof(SysLoginInformation)}_{nameof(LoginUserName)}", nameof(LoginUserName))]
public sealed class SysLoginInformation : Entity
{
    /// <summary>
    /// 创建者用户Id
    /// </summary>
    [Column(CanUpdate = false)]
    [JsonConverter(typeof(JsonConverterUtil.LongJsonConverter))]
    public long LoginUserId { get; set; }

    /// <summary>
    /// 创建者用户名
    /// </summary>
    [Column(CanUpdate = false, StringLength = 64)]
    public string LoginUserName { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Column(CanUpdate = false)]
    [ServerTime]
    [JsonConverter(typeof(JsonConverterUtil.DateTimeJsonConverter))]
    public DateTime LoginTime { get; set; }

    /// <summary>
    /// 是否登录成功
    /// </summary>
    public bool IsLoginOk { get; set; } = true;

    /// <summary>
    /// 提示消息
    /// </summary>
    public string? Message { get; set; }
}