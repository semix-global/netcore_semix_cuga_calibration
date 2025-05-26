namespace Local.SQL.DB.Providers.Models.Entities.Base.Interface;

public interface IVersion
{
    /// <summary>
    /// 数据版本
    /// </summary>
    long Version { get; set; }
}