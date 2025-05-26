using FreeSql.DataAnnotations;
using Local.SQL.DB.Providers.Models.Attributes;
using Local.SQL.DB.Providers.Models.Entities.Base.Interface;
using Net.Utilities.Helper.Json;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Local.SQL.DB.Providers.Models.Entities.Base.Implements;

public class EntityUpdate : EntityAdd, IEntityUpdate
{
    /// <summary>
    /// 修改者用户Id
    /// </summary>
    [Description("修改者用户Id")]
    [Column(Position = -6002)]
    [JsonProperty(Order = 6000)]
    [JsonConverter(typeof(JsonConverterUtil.LongJsonConverter))]
    public virtual long? ModifiedUserId { get; set; }

    /// <summary>
    /// 修改者用户名
    /// </summary>
    [Description("修改者用户名")]
    [Column(Position = -6001, StringLength = 64)]
    [JsonProperty(Order = 6001)]
    public virtual string? ModifiedUserName { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    [Description("修改时间")]
    [Column(Position = -6000)]
    [ServerTime(CanInsert = false, CanUpdate = true)]
    [JsonProperty(Order = 6002)]
    [JsonConverter(typeof(JsonConverterUtil.DateTimeJsonConverter))]
    public virtual DateTime? ModifiedTime { get; set; }
}