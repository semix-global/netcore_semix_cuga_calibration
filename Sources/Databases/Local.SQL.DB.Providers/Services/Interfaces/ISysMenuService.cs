using Local.SQL.DB.Providers.Models.Entities.DTO;

namespace Local.SQL.DB.Providers.Services.Interfaces;

public interface ISysMenuService
{
    /// <summary>
    /// 级联更新开关，使能状态才会更新
    /// </summary>
    /// <param name="isEnable"></param>
    void EnableCascadeSave(bool isEnable);

    /// <summary>
    /// 获取菜单信息
    /// </summary>
    /// <param name="id">id array</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单信息</returns>
    Task<SysMenuDto?> GetAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// 获取菜单信息列表
    /// </summary>
    /// <param name="ids">id array</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单信息列表</returns>
    Task<List<SysMenuDto>> GetAsync(long[] ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// 条件查询获取菜单信息列表
    /// </summary>
    /// <param name="conditionMenuDto">条件dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门信息列表</returns>
    Task<List<SysMenuDto>> GetByConditionAsync(SysMenuDto conditionMenuDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 递归条件查询获取菜单信息列表
    /// </summary>
    /// <param name="conditionMenuDto">条件dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门信息列表</returns>
    Task<List<SysMenuDto>> GetByConditionAsTreeAsync(SysMenuDto conditionMenuDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 递归获取全部菜单信息列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单信息列表</returns>
    Task<List<SysMenuDto>> GetAllAsTreeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取全部菜单信息列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>菜单信息列表</returns>
    Task<List<SysMenuDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 构建树结构
    /// </summary>
    /// <param name="menus">菜单列表</param>
    /// <returns>树结构</returns>
    List<SysMenuDto> BuildMenuTree(List<SysMenuDto> menus);

    /// <summary>
    /// 展开树结构
    /// </summary>
    /// <param name="rootList">部门列表</param>
    /// <returns>展开后集合</returns>
    List<SysMenuDto> FlattenMenuTree(List<SysMenuDto> rootList);
}