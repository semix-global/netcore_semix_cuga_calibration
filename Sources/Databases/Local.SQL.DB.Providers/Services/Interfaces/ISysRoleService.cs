using Local.SQL.DB.Providers.Models.Entities.DTO;

namespace Local.SQL.DB.Providers.Services.Interfaces;

public interface ISysRoleService
{
    /// <summary>
    /// 级联更新开关，使能状态才会更新
    /// </summary>
    /// <param name="isEnable"></param>
    void EnableCascadeSave(bool isEnable);

    /// <summary>
    /// 获取角色信息
    /// </summary>
    /// <param name="id">id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色信息</returns>
    Task<SysRoleDto?> GetAsync(long id, CancellationToken cancellationToken);

    /// <summary>
    /// 获取角色信息列表
    /// </summary>
    /// <param name="ids">id array</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>角色信息列表</returns>
    Task<List<SysRoleDto>> GetAsync(long[] ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// 条件查询获取角色信息列表
    /// </summary>
    /// <param name="conditionUserDto">条件dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户信息列表</returns>
    Task<List<SysRoleDto>> GetByConditionAsync(SysRoleDto conditionUserDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取全部角色信息列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户信息列表</returns>
    Task<List<SysRoleDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 增加角色信息列表
    /// </summary>
    /// <param name="sysRoleDto">待新增的dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> InsertAsync(SysRoleDto sysRoleDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改角色信息列表
    /// </summary>
    /// /// <param name="sysRoleDto">待修改的dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateAsync(SysRoleDto sysRoleDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除角色信息列表(级联删除)
    /// </summary>
    /// /// <param name="sysRoleDto">待删除的dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DeleteAsync(SysRoleDto sysRoleDto, CancellationToken cancellationToken = default);
}