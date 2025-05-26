namespace Local.SQL.DB.Providers.Models.Entities.Base.Interface;

public interface IDelete
{
    /// <summary>
    /// 是否删除
    /// </summary>
    bool IsDeleted { get; set; }
}