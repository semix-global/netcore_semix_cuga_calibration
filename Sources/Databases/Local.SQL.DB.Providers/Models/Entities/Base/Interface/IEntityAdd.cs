namespace Local.SQL.DB.Providers.Models.Entities.Base.Interface;

public interface IEntityAdd
{
    /// <summary>
    /// 创建者用户Id
    /// </summary>
    long CreatedUserId { get; set; }

    /// <summary>
    /// 创建者
    /// </summary>
    string CreatedUserName { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    DateTime CreatedTime { get; set; }
}