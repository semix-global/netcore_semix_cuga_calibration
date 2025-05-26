using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Repositories.Interfaces;
using Local.SQL.DB.Providers.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace Local.SQL.DB.Providers.Services.Implements;

[IOCAppService(ServiceType = typeof(ISysPostService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class SysPostServiceImpl(ISysPostRepository sysPostRepository) : ISysPostService
{
    public void EnableCascadeSave(bool isEnable)
    {
        sysPostRepository.DbContextOptions.EnableCascadeSave = isEnable;
    }

    public async Task<SysPostDto?> GetAsync(long id, CancellationToken cancellationToken)
    {
        var sysPost = await sysPostRepository.Select
            .WhereDynamic(id)
            .IncludeMany(a => a.SysUserList)
            .ToOneAsync(cancellationToken).ConfigureAwait(false);

        return sysPost is null ? null : new SysPostDto().AdaptIn(sysPost);
    }

    public async Task<List<SysPostDto>> GetAsync(long[] ids, CancellationToken cancellationToken)
    {
        var sysPostList = await sysPostRepository.Select
            .WhereDynamic(ids)
            .IncludeMany(a => a.SysUserList)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return sysPostList?.Count > 0 ? [.. sysPostList.Select(t => new SysPostDto().AdaptIn(t))] : [];
    }
}