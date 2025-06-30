using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Models.Exceptions;
using Local.SQL.DB.Providers.Repositories.Interfaces;
using Local.SQL.DB.Providers.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers;

namespace Local.SQL.DB.Providers.Services.Implements;

[IOCAppService(ServiceType = typeof(ISysUserService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class SysUserServiceImpl(
    ISysUserRepository sysUserRepository,
    ISysRoleService sysRoleService) : ISysUserService
{
    public void EnableCascadeSave(bool isEnable)
    {
        sysUserRepository.DbContextOptions.EnableCascadeSave = isEnable;
    }

    public async Task<SysUserDto> LoginAsync(SysUserDto user, CancellationToken cancellationToken)
    {
        user = user.Clone();

        if (string.IsNullOrWhiteSpace(user.UserName) || string.IsNullOrWhiteSpace(user.Password))
            throw new LoginException("The account or password cannot be empty!");

        user.Password = EncryptUtils.Encrypt32(user.Password);

        var sysUser = await sysUserRepository
            .Select
            .Where(t => t.UserName == user.UserName && t.Password == user.Password)
            .ToOneAsync(cancellationToken).ConfigureAwait(false) ?? throw new LoginException("The account or password is incorrect!");
        if (sysUser.IsDeleted || sysUser.IsEnabled == false) throw new LoginException("The account has been deactivated and login is prohibited!");

        var sysUserDto = await GetAsync(sysUser.Id, cancellationToken).ConfigureAwait(false) ?? throw new DbException();

        sysUserDto.LoginDate = DateTime.Now;
        var isSuccess = await UpdateAsync(sysUserDto, cancellationToken).ConfigureAwait(false);
        if (isSuccess == false) throw new LoginException("Login time write failed");

        return sysUserDto;
    }

    public async Task<SysUserDto?> GetAsync(long id, CancellationToken cancellationToken)
    {
        var sysUser = await sysUserRepository.Select
            .WhereDynamic(id)
            .Include(a => a.SysDept)
            .IncludeMany(a => a.SysPostList)
            .IncludeMany(a => a.SysRoleList)
            .ToOneAsync(cancellationToken).ConfigureAwait(false);
        if (sysUser is null) return null;

        var sysUserDto = new SysUserDto().AdaptIn(sysUser);
        sysUserDto.SysRoleList = await sysRoleService.GetAsync([.. sysUser.SysRoleList.Select(t => t.Id)], cancellationToken).ConfigureAwait(false);

        return sysUserDto;
    }

    public async Task<List<SysUserDto>> GetAsync(long[] ids, CancellationToken cancellationToken)
    {
        var sysUserList = await sysUserRepository.Select
            .WhereDynamic(ids)
            .Include(a => a.SysDept)
            .IncludeMany(a => a.SysPostList)
            .IncludeMany(a => a.SysRoleList)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return sysUserList?.Count > 0 ? [.. sysUserList.Select(t => new SysUserDto().AdaptIn(t))] : [];
    }

    public async Task<List<SysUserDto>> GetByConditionAsync(SysUserDto conditionUserDto, CancellationToken cancellationToken = default)
    {
        var sysUserList = await sysUserRepository.Select
            .WhereIf(conditionUserDto.Id != 0, a => a.Id == conditionUserDto.Id)
            .WhereIf(conditionUserDto.UserName != string.Empty, a => a.UserName == conditionUserDto.UserName)
            .WhereIf(conditionUserDto.DeptId > 0, a => a.DeptId == conditionUserDto.DeptId)
            .WhereIf(conditionUserDto.SexEnum != null, a => a.SexEnum == conditionUserDto.SexEnum)
            .WhereIf(conditionUserDto.IsEnabled != null, a => a.IsEnabled == conditionUserDto.IsEnabled)
            .Include(a => a.SysDept)
            .IncludeMany(a => a.SysPostList)
            .IncludeMany(a => a.SysRoleList)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        //.ToSql();
        //throw new NotImplementedException();
        return sysUserList?.Count > 0 ? [.. sysUserList.Select(t => new SysUserDto().AdaptIn(t))] : [];
    }

    public async Task<List<SysUserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sysUserList = await sysUserRepository.Select
            .Include(a => a.SysDept)
            .IncludeMany(a => a.SysPostList)
            .IncludeMany(a => a.SysRoleList)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return sysUserList?.Count > 0 ? [.. sysUserList.Select(t => new SysUserDto().AdaptIn(t))] : [];
    }

    public async Task<bool> InsertAsync(SysUserDto sysUserDto, CancellationToken cancellationToken = default)
    {
        var addAsync = await sysUserRepository.InsertOrUpdateAsync(sysUserDto.AdaptTo(), cancellationToken).ConfigureAwait(false);
        return addAsync is not null;
    }

    public async Task<bool> UpdateAsync(SysUserDto sysUserDto, CancellationToken cancellationToken = default)
    {
        var updateAsync = await sysUserRepository.UpdateAsync(sysUserDto.AdaptTo(), cancellationToken).ConfigureAwait(false);
        return updateAsync == 1;
    }

    public async Task<bool> DeleteAsync(SysUserDto sysUserDto, CancellationToken cancellationToken = default)
    {
        var deleteListAsync = await sysUserRepository.DeleteCascadeByDatabaseAsync(t => t == sysUserDto.AdaptTo(), cancellationToken).ConfigureAwait(false);
        return deleteListAsync.Count > 0;
    }
}