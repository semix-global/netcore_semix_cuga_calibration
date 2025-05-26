using Local.SQL.DB.Providers.Models.Entities.DTO;

namespace Local.SQL.DB.Providers.Services.Interfaces;

public interface ISysPostService
{
    /// <summary>
    /// 级联更新开关，使能状态才会更新
    /// </summary>
    /// <param name="isEnable"></param>
    void EnableCascadeSave(bool isEnable);

    /// <summary>
    /// 获取岗位信息
    /// </summary>
    /// <param name="id">id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>岗位信息</returns>
    Task<SysPostDto?> GetAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// 获取岗位信息列表
    /// </summary>
    /// <param name="ids">id array</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>岗位信息列表</returns>
    Task<List<SysPostDto>> GetAsync(long[] ids, CancellationToken cancellationToken = default);
}