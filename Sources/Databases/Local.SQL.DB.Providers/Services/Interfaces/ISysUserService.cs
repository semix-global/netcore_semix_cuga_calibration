using Local.SQL.DB.Providers.Models.Entities.DTO;

namespace Local.SQL.DB.Providers.Services.Interfaces;

public interface ISysUserService
{
    /// <summary>
    /// 级联更新开关，使能状态才会更新
    /// </summary>
    /// <param name="isEnable"></param>
    void EnableCascadeSave(bool isEnable);

    /// <summary>
    /// 登录
    /// </summary>
    /// <param name="user">用户对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户</returns>
    Task<SysUserDto> LoginAsync(SysUserDto user, CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过id获取用户
    /// </summary>
    /// <param name="id">id</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户</returns>
    Task<SysUserDto?> GetAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过id获取用户信息列表
    /// </summary>
    /// <param name="ids">id array</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户信息列表</returns>
    Task<List<SysUserDto>> GetAsync(long[] ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// 条件查询获取用户信息列表
    /// </summary>
    /// <param name="conditionUserDto">条件dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户信息列表</returns>
    Task<List<SysUserDto>> GetByConditionAsync(SysUserDto conditionUserDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取全部用户信息列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户信息列表</returns>
    Task<List<SysUserDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 增加用户信息列表
    /// </summary>
    /// <param name="sysUserDto">待新增的dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> InsertAsync(SysUserDto sysUserDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改用户信息列表
    /// </summary>
    /// /// <param name="sysUserDto">待修改的dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateAsync(SysUserDto sysUserDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户信息列表(级联删除)
    /// </summary>
    /// /// <param name="sysUserDto">待删除的dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DeleteAsync(SysUserDto sysUserDto, CancellationToken cancellationToken = default);
}