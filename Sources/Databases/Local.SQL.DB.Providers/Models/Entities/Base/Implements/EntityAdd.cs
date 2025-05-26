using FreeSql.DataAnnotations;
using Local.SQL.DB.Providers.Models.Attributes;
using Local.SQL.DB.Providers.Models.Entities.Base.Interface;
using Net.Utilities.Helper.Json;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Local.SQL.DB.Providers.Models.Entities.Base.Implements;

public class EntityAdd : Entity, IEntityAdd
{
    /// <summary>
    /// 创建者用户Id
    /// </summary>
    [Description("创建者用户Id")]
    [Column(Position = -7002, CanUpdate = false)]
    [JsonProperty(Order = 7000)]
    [JsonConverter(typeof(JsonConverterUtil.LongJsonConverter))]
    public virtual long CreatedUserId { get; set; }

    /// <summary>
    /// 创建者用户名
    /// </summary>
    [Description("创建者用户名")]
    [Column(Position = -7001, CanUpdate = false, StringLength = 64)]
    [JsonProperty(Order = 7001)]
    public virtual string CreatedUserName { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    [Description("创建时间")]
    [Column(Position = -7000, CanUpdate = false)]
    [ServerTime]
    [JsonProperty(Order = 7002)]
    [JsonConverter(typeof(JsonConverterUtil.DateTimeJsonConverter))]
    public virtual DateTime CreatedTime { get; set; } = DateTime.Now;
}