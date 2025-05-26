using FreeSql.DataAnnotations;
using Local.SQL.DB.Providers.Models.Entities.Base.Interface;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Local.SQL.DB.Providers.Models.Entities.Base.Implements;

/// <summary>
/// 实体删除
/// </summary>
public class EntityDelete : EntityUpdate, IDelete
{
    /// <summary>
    /// 是否删除
    /// </summary>
    [Description("是否删除")]
    [Column(Position = -5000)]
    [JsonProperty(Order = 5000)]
    public virtual bool IsDeleted { get; set; }
}