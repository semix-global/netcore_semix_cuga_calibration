using Local.SQL.DB.Providers.Models.Entities;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Repositories.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace Net.Utilities.Calibration;

[IOCAppService(ServiceType = typeof(ISysRoleMenuService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public class SysRoleMenuServiceImpl(
    ISysRoleRepository sysRoleRepository,
    ISysRoleMenuRepository sysRoleMenuRepository) : ISysRoleMenuService
{
    public async Task<bool> InsertAsync(SysRoleDTO sysRoleDTO, CancellationToken cancellationToken = default)
    {
        // SysRole.SysMenuList 是 [Navigate(ManyToMany = typeof(SysRoleMenu))] 导航属性,
        // 由中间表 (RoleId, MenuId) 关联解析; 禁用级联保存后, 直接写中间表即完成菜单分配

        // 新增角色的雪花Id由仓储插入时分配, DTO不回写, Id为0时按角色名(唯一索引)反查真实Id
        var roleId = sysRoleDTO.Id;
        if (roleId == 0)
        {
            var sysRole = await sysRoleRepository.Select
                .Where(t => t.Name == sysRoleDTO.Name)
                .ToOneAsync(cancellationToken).ConfigureAwait(false);
            if (sysRole is null) return false;

            roleId = sysRole.Id;
        }

        // 先清空旧关联再写入本次勾选的菜单: 新增/编辑统一语义, 编辑时避免撞 (RoleId, MenuId) 唯一索引
        await DeleteAsync(new SysRoleDTO { Id = roleId }, cancellationToken).ConfigureAwait(false);

        var sysRoleMenuList = sysRoleDTO.SysMenuList
            .Select(t => new SysRoleMenu { RoleId = roleId, MenuId = t.Id })
            .ToList();

        if (sysRoleMenuList.Count == 0) return true;

        var addedList = await sysRoleMenuRepository.InsertAsync(sysRoleMenuList, cancellationToken).ConfigureAwait(false);
        return addedList.Count == sysRoleMenuList.Count;
    }

    public async Task<bool> DeleteAsync(SysRoleDTO sysRoleDTO, CancellationToken cancellationToken = default)
    {
        // 删除角色时清空其菜单关联; 关联0行同样视为成功, 不阻塞角色删除流程
        if (sysRoleDTO.Id == 0) return false;

        await sysRoleMenuRepository.DeleteAsync(t => t.RoleId == sysRoleDTO.Id, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
