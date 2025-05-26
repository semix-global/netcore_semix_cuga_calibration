using FreeSql.DataAnnotations;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Local.SQL.DB.Providers.Models.Entities.Base.Implements;

public class EntityVersion : EntityBase
{
    /// <summary>
    /// 版本
    /// </summary>
    [Description("版本")]
    [Column(Position = -8000, IsVersion = true)]
    [JsonProperty(Order = 8000)]
    public virtual long Version { get; set; }
}