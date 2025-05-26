using FreeSql.DataAnnotations;
using Local.SQL.DB.Providers.Models.Attributes;
using Local.SQL.DB.Providers.Models.Entities.Base.Interface;
using Net.Utilities.Helper.Json;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Local.SQL.DB.Providers.Models.Entities.Base.Implements;

public class Entity : IEntity
{
    /// <summary>
    /// 主键Id
    /// </summary>
    [Description("主键Id")]
    [Column(Position = 1, IsIdentity = false, IsPrimary = true)]
    [Snowflake]
    [JsonProperty(Order = 1)]
    [JsonConverter(typeof(JsonConverterUtil.LongJsonConverter))]
    public virtual long Id { get; set; }
}