using FreeSql;
using Local.SQL.DB.Providers.Models.Entities;
using Local.SQL.DB.Providers.Repositories.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;

namespace Local.SQL.DB.Providers.Repositories.Implements;

[IOCAppService(ServiceType = typeof(ISysRoleMenuRepository), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class SysRoleMenuRepositoryImpl : BaseRepository<SysRoleMenu, long>, ISysRoleMenuRepository
{
    public SysRoleMenuRepositoryImpl(UnitOfWorkManager unitOfWorkManager) : base(unitOfWorkManager.Orm)
    {
        unitOfWorkManager.Binding(this);
    }
}