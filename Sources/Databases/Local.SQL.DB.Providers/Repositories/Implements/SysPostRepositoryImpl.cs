using FreeSql;
using Local.SQL.DB.Providers.Models.Entities;
using Local.SQL.DB.Providers.Repositories.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace Local.SQL.DB.Providers.Repositories.Implements;

[IOCAppService(ServiceType = typeof(ISysPostRepository), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class SysPostRepositoryImpl : BaseRepository<SysPost, long>, ISysPostRepository
{
    public SysPostRepositoryImpl(UnitOfWorkManager unitOfWorkManager) : base(unitOfWorkManager.Orm)
    {
        unitOfWorkManager.Binding(this);
    }
}