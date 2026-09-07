using Local.SQL.DB.Providers.Models.Entities;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Repositories.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace Net.Utilities.Calibration;

[IOCAppService(ServiceType = typeof(ISysUserRoleService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class SysUserRoleServiceImpl(
    ISysUserRepository sysUserRepository,
    ISysUserRoleRepository sysUserRoleRepository) : ISysUserRoleService
{
    public async Task<bool> InsertAsync(SysUserDTO sysUserDTO, CancellationToken cancellationToken = default)
    {
        // SysUser.SysRoleList 是 [Navigate(ManyToMany = typeof(SysUserRole))] 导航属性,
        // 由中间表 (UserId, RoleId) 关联解析; 禁用级联保存后, 直接写中间表即完成角色分配

        // 新增用户的雪花Id由仓储插入时分配, DTO不回写, Id为0时按用户名(唯一索引)反查真实Id
        var userId = sysUserDTO.Id;
        if (userId == 0)
        {
            var sysUser = await sysUserRepository.Select
                .Where(t => t.UserName == sysUserDTO.UserName)
                .ToOneAsync(cancellationToken).ConfigureAwait(false);
            if (sysUser is null) return false;

            userId = sysUser.Id;
        }

        // 先清空旧关联再写入本次勾选的角色: 新增/编辑统一语义, 编辑时避免撞 (UserId, RoleId) 唯一索引
        await sysUserRoleRepository.DeleteAsync(t => t.UserId == userId, cancellationToken).ConfigureAwait(false);

        var sysUserRoleList = sysUserDTO.SysRoleList
            .Select(t => new SysUserRole { UserId = userId, RoleId = t.Id })
            .ToList();

        if (sysUserRoleList.Count == 0) return true;

        var addedList = await sysUserRoleRepository.InsertAsync(sysUserRoleList, cancellationToken).ConfigureAwait(false);
        return addedList.Count == sysUserRoleList.Count;
    }
}
