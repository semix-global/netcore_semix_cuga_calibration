namespace Local.SQL.DB.Providers.Models.Entities.Base.Interface;

public interface IEntityUpdate
{
    /// <summary>
    /// 修改者Id
    /// </summary>
    long? ModifiedUserId { get; set; }

    /// <summary>
    /// 修改者
    /// </summary>
    string? ModifiedUserName { get; set; }

    /// <summary>
    /// 修改时间
    /// </summary>
    DateTime? ModifiedTime { get; set; }
}