using FreeSql.DataAnnotations;
using Local.SQL.DB.Providers.Models.Entities.Base.Implements;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Local.SQL.DB.Providers.Models.Entities.Base;

public class EntityBase : EntityDelete
{
    /// <summary>
    /// 备注
    /// </summary>
    [Description("修改者用户Id")]
    [Column(Position = -9000, StringLength = 500)]
    [JsonProperty(Order = 9000)]
    public string? Remark { get; set; }

    /// <summary>
    /// 启用
    /// </summary>
    [Description("启用")]
    [Column(Position = -9001)]
    [JsonProperty(Order = 9001)]
    public bool IsEnabled { get; set; } = true;
}