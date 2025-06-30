using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Repositories.Interfaces;
using Local.SQL.DB.Providers.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;

namespace Local.SQL.DB.Providers.Services.Implements;

[IOCAppService(ServiceType = typeof(ISysMenuService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class SysMenuServiceImpl(ISysMenuRepository sysMenuRepository) : ISysMenuService
{
    public void EnableCascadeSave(bool isEnable)
    {
        sysMenuRepository.DbContextOptions.EnableCascadeSave = isEnable;
    }

    public async Task<SysMenuDto?> GetAsync(long id, CancellationToken cancellationToken)
    {
        var sysMenu = await sysMenuRepository.Select
            .WhereDynamic(id)
            .IncludeMany(a => a.SysRoleList)
            .ToOneAsync(cancellationToken).ConfigureAwait(false);

        return sysMenu is null ? null : new SysMenuDto().AdaptIn(sysMenu);
    }

    public async Task<List<SysMenuDto>> GetAsync(long[] ids, CancellationToken cancellationToken)
    {
        var sysMenuList = await sysMenuRepository.Select
            .WhereDynamic(ids)
            .IncludeMany(a => a.SysRoleList)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return sysMenuList?.Count > 0 ? [.. sysMenuList.Select(t => new SysMenuDto().AdaptIn(t))] : [];
    }

    public async Task<List<SysMenuDto>> GetByConditionAsync(SysMenuDto conditionMenuDto, CancellationToken cancellationToken = default)
    {
        var sysMenu = await sysMenuRepository.Select
            .Where(a => a.MenuTypeEnum == conditionMenuDto.MenuTypeEnum)
            .WhereIf(conditionMenuDto.Name != string.Empty, a => a.Name == conditionMenuDto.Name)
            .WhereIf(conditionMenuDto?.IsEnabled != null, a => a.IsEnabled == conditionMenuDto!.IsEnabled)
            .WhereIf(conditionMenuDto!.Component != string.Empty, a => a.Component.Contains(conditionMenuDto.Component))
            .WhereIf(conditionMenuDto.Perms != string.Empty, a => a.Perms!.Contains(conditionMenuDto.Perms!))
            .IncludeMany(a => a.SysRoleList)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return sysMenu?.Count > 0 ? [.. sysMenu.Select(t => new SysMenuDto().AdaptIn(t))] : [];
    }

    public async Task<List<SysMenuDto>> GetByConditionAsTreeAsync(SysMenuDto conditionMenuDto, CancellationToken cancellationToken = default)
    {
        var sysMenu = await sysMenuRepository.Select
            .WhereIf(conditionMenuDto.Name != string.Empty, a => a.Name == conditionMenuDto.Name)
            .WhereIf(conditionMenuDto?.IsEnabled != null, a => a.IsEnabled == conditionMenuDto!.IsEnabled)
            .IncludeMany(a => a.SysRoleList)
            .AsTreeCte()
            .ToTreeListAsync(cancellationToken).ConfigureAwait(false);
        return sysMenu?.Count > 0 ? [.. sysMenu.Select(t => new SysMenuDto().AdaptIn(t))] : [];
    }

    public async Task<List<SysMenuDto>> GetAllAsTreeAsync(CancellationToken cancellationToken = default)
    {
        var sysMenuList = await sysMenuRepository.Select
            .Include(a => a.Parent)
            .ToTreeListAsync(cancellationToken).ConfigureAwait(false);
        return sysMenuList?.Count > 0 ? [.. sysMenuList.Select(t => new SysMenuDto().AdaptIn(t))] : [];
    }

    public async Task<List<SysMenuDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var sysMenuList = await sysMenuRepository.Select
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return sysMenuList?.Count > 0 ? [.. sysMenuList.Select(t => new SysMenuDto().AdaptIn(t))] : [];
    }

    public List<SysMenuDto> BuildMenuTree(List<SysMenuDto> menus)
    {
        var returnList = new List<SysMenuDto>();

        foreach (var menu in menus.Where(menu => menu.ParentId == Constants.NegInt32Value))
        {
            RecursionFn(menus, menu);
            returnList.Add(menu);
        }

        return returnList;

        static void RecursionFn(List<SysMenuDto> list, SysMenuDto sysMenuDto, int depth = 0)
        {
            // 得到子节点列表
            var childList = list.Where(p => p.ParentId == sysMenuDto.Id).ToList();
            childList.ForEach(t => t.Depth = depth + 1);
            sysMenuDto.ChildList = childList;

            foreach (var item in childList.Where(item => list.Any(p => p.ParentId == item.Id)))
            {
                RecursionFn(list, item, depth + 1);
            }
        }
    }

    public List<SysMenuDto> FlattenMenuTree(List<SysMenuDto> rootList)
    {
        var flattenListResult = new List<SysMenuDto>();
        foreach (var rootdept in rootList)
        {
            var resultList = new List<SysMenuDto>();
            Flatten(rootdept, resultList);
            flattenListResult.AddRange(resultList);
        }

        return flattenListResult;

        static void Flatten(SysMenuDto node, List<SysMenuDto> list)
        {
            list.Add(node);
            if (node.ChildList != null)
            {
                foreach (var child in node.ChildList)
                {
                    Flatten(child, list);
                }
            }
        }
    }
}