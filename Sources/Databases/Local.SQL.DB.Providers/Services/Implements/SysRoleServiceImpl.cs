using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Repositories.Interfaces;
using Local.SQL.DB.Providers.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace Local.SQL.DB.Providers.Services.Implements;

[IOCAppService(ServiceType = typeof(ISysRoleService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class SysRoleServiceImpl(ISysRoleRepository sysRoleRepository) : ISysRoleService
{
    public void EnableCascadeSave(bool isEnable)
    {
        sysRoleRepository.DbContextOptions.EnableCascadeSave = isEnable;
    }

    public async Task<SysRoleDto?> GetAsync(long id, CancellationToken cancellationToken)
    {
        var sysRole = await sysRoleRepository.Select
            .WhereDynamic(id)
            .IncludeMany(a => a.SysDeptList)
            .IncludeMany(a => a.SysMenuList)
            .IncludeMany(a => a.SysUserList)
            .ToOneAsync(cancellationToken).ConfigureAwait(false);

        return sysRole is null ? null : new SysRoleDto().AdaptIn(sysRole);
    }

    public async Task<List<SysRoleDto>> GetAsync(long[] ids, CancellationToken cancellationToken)
    {
        var sysRoleList = await sysRoleRepository.Select
            .WhereDynamic(ids)
            .IncludeMany(a => a.SysDeptList)
            .IncludeMany(a => a.SysMenuList)
            .IncludeMany(a => a.SysUserList)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return sysRoleList?.Count > 0 && ids.Length > 0 ? [.. sysRoleList.Select(t => new SysRoleDto().AdaptIn(t))] : [];
    }

    public async Task<List<SysRoleDto>> GetByConditionAsync(SysRoleDto conditionRoleDto, CancellationToken cancellationToken = default)
    {
        var sysRolesList = await sysRoleRepository.Select
            .WhereIf(conditionRoleDto.Id != 0, a => a.Id == conditionRoleDto.Id)
            .WhereIf(conditionRoleDto.Name != string.Empty, a => a.Name == conditionRoleDto.Name)
            .WhereIf(conditionRoleDto.DataScopeEnum != 0, a => a.DataScopeEnum == conditionRoleDto.DataScopeEnum)
            .IncludeMany(a => a.SysDeptList)
            .IncludeMany(a => a.SysMenuList)
            .IncludeMany(a => a.SysUserList)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        //.ToSql();
        //throw new NotImplementedException();
        return sysRolesList?.Count > 0 ? [.. sysRolesList.Select(t => new SysRoleDto().AdaptIn(t))] : [];
    }

    public async Task<List<SysRoleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sysRolesList = await sysRoleRepository.Select
            .IncludeMany(a => a.SysDeptList)
            .IncludeMany(a => a.SysMenuList)
            .IncludeMany(a => a.SysUserList)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return sysRolesList?.Count > 0 ? [.. sysRolesList.Select(t => new SysRoleDto().AdaptIn(t))] : [];
    }

    public async Task<bool> InsertAsync(SysRoleDto sysRoleDto, CancellationToken cancellationToken = default)
    {
        var addAsync = await sysRoleRepository.InsertOrUpdateAsync(sysRoleDto.AdaptTo(), cancellationToken).ConfigureAwait(false);
        return addAsync is not null;
    }

    public async Task<bool> UpdateAsync(SysRoleDto sysRoleDto, CancellationToken cancellationToken = default)
    {
        var updateAsync = await sysRoleRepository.UpdateAsync(sysRoleDto.AdaptTo(), cancellationToken).ConfigureAwait(false);
        return updateAsync == 1;
    }

    public async Task<bool> DeleteAsync(SysRoleDto sysRoleDto, CancellationToken cancellationToken = default)
    {
        var deleteListAsync = await sysRoleRepository.DeleteCascadeByDatabaseAsync(t => t == sysRoleDto.AdaptTo(), cancellationToken).ConfigureAwait(false);
        return deleteListAsync.Count > 0;
    }
}