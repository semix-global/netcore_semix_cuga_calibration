using FreeSql;
using Local.SQL.DB.Providers.Models.Entities;
using Local.SQL.DB.Providers.Repositories.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace Local.SQL.DB.Providers.Repositories.Implements;

[IOCAppService(ServiceType = typeof(ISysRecipeInformationRepository), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class SysRecipeInformationRepositoryImpl : BaseRepository<SysRecipeInformation, long>, ISysRecipeInformationRepository
{
    public SysRecipeInformationRepositoryImpl(UnitOfWorkManager unitOfWorkManager) : base(unitOfWorkManager.Orm)
    {
        unitOfWorkManager.Binding(this);
    }
}