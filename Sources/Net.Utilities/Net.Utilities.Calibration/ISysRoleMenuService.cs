using Local.SQL.DB.Providers.Models.Entities.DTO;

namespace Net.Utilities.Calibration;

public interface ISysRoleMenuService
{
    /// <summary>
    /// 删除角色-菜单关联数据 (删除角色时清空中间表)
    /// </summary>
    /// <param name="sysRoleDTO">待删除关联的角色DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DeleteAsync(SysRoleDTO sysRoleDTO, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增/同步角色-菜单关联数据 (显式维护中间表, 不依赖级联保存)
    /// </summary>
    /// <param name="sysRoleDTO">携带菜单列表的角色DTO</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> InsertAsync(SysRoleDTO sysRoleDTO, CancellationToken cancellationToken = default);
}