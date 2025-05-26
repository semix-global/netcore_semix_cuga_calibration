using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Repositories.Interfaces;
using Local.SQL.DB.Providers.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace Local.SQL.DB.Providers.Services.Implements;

[IOCAppService(ServiceType = typeof(ISysRecipeInformationService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class SysRecipeInformationServiceImpl(ISysRecipeInformationRepository sysRecipeInformationRepository) : ISysRecipeInformationService
{
    public async Task<List<SysRecipeInformationDto>> GetByConditionAsync(SysRecipeInformationDto conditionRecipeInfoDto, CancellationToken cancellationToken = default)
    {
        var sysRecipeInfoList = await sysRecipeInformationRepository.Select
            .WhereIf(conditionRecipeInfoDto.RecipeDbName != string.Empty, a => a.RecipeDbName == conditionRecipeInfoDto.RecipeDbName)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        //.ToSql();
        //throw new NotImplementedException();
        return sysRecipeInfoList?.Count > 0 ? [.. sysRecipeInfoList.Select(t => new SysRecipeInformationDto().AdaptIn(t))] : [];
    }

    public async Task<List<SysRecipeInformationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sysRecipeInformationList = await sysRecipeInformationRepository.Select
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return sysRecipeInformationList?.Count > 0 ? [.. sysRecipeInformationList.Select(t => new SysRecipeInformationDto().AdaptIn(t))] : [];
    }

    public async Task<bool> InsertAsync(SysRecipeInformationDto sysRecipeInformationDto, CancellationToken cancellationToken = default)
    {
        var addAsync = await sysRecipeInformationRepository.InsertOrUpdateAsync(sysRecipeInformationDto.AdaptTo(), cancellationToken).ConfigureAwait(false);
        return addAsync is not null;
    }

    public async Task<bool> UpdateAsync(SysRecipeInformationDto sysRecipeInformationDto, CancellationToken cancellationToken = default)
    {
        var updateAsync = await sysRecipeInformationRepository.UpdateAsync(sysRecipeInformationDto.AdaptTo(), cancellationToken).ConfigureAwait(false);
        return updateAsync == 1;
    }

    public async Task<bool> DeleteAsync(SysRecipeInformationDto sysRecipeInformationDto, CancellationToken cancellationToken = default)
    {
        var updateAsync = await sysRecipeInformationRepository.DeleteAsync(sysRecipeInformationDto.AdaptTo(), cancellationToken).ConfigureAwait(false);
        return updateAsync == 1;
    }
}