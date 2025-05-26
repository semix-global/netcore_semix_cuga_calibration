using Local.SQL.DB.Providers.Models.Entities.DTO;
using Local.SQL.DB.Providers.Repositories.Interfaces;
using Local.SQL.DB.Providers.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Constants;
using Net.Utilities.Enums;

namespace Local.SQL.DB.Providers.Services.Implements;

[IOCAppService(ServiceType = typeof(ISysDeptService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class SysDeptServiceImpl(ISysDeptRepository sysDeptRepository) : ISysDeptService
{
    public void EnableCascadeSave(bool isEnable)
    {
        sysDeptRepository.DbContextOptions.EnableCascadeSave = isEnable;
    }

    public async Task<SysDeptDto?> GetAsync(long id, CancellationToken cancellationToken)
    {
        var sysDept = await sysDeptRepository.Select
            .WhereDynamic(id)
            .IncludeMany(a => a.SysUserList)
            .IncludeMany(a => a.SysRoleList)
            .ToOneAsync(cancellationToken).ConfigureAwait(false);

        return sysDept is null ? null : new SysDeptDto().AdaptIn(sysDept);
    }

    public async Task<List<SysDeptDto>> GetAsync(long[] ids, CancellationToken cancellationToken)
    {
        var sysDeptList = await sysDeptRepository.Select
            .WhereDynamic(ids)
            .IncludeMany(a => a.SysUserList)
            .IncludeMany(a => a.SysRoleList)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return sysDeptList?.Count > 0 ? [.. sysDeptList.Select(t => new SysDeptDto().AdaptIn(t))] : [];
    }

    public async Task<List<SysDeptDto>> GetByConditionAsync(SysDeptDto conditionDeptDto, CancellationToken cancellationToken = default)
    {
        var sysDept = await sysDeptRepository.Select
            .WhereIf(conditionDeptDto.Name != string.Empty, a => a.Name == conditionDeptDto.Name)
            .WhereIf(conditionDeptDto.Leader != string.Empty, a => a.LeaderUserId == conditionDeptDto.LeaderUserId)
            .WhereIf(conditionDeptDto?.IsEnabled != null, a => a.IsEnabled == conditionDeptDto!.IsEnabled)
            .IncludeMany(a => a.SysUserList)
            .IncludeMany(a => a.SysRoleList)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return sysDept?.Count > 0 ? [.. sysDept.Select(t => new SysDeptDto().AdaptIn(t))] : [];
    }

    public async Task<List<SysDeptDto>> GetByConditionAsTreeAsync(SysDeptDto conditionDeptDto, CancellationToken cancellationToken = default)
    {
        var sysDept = await sysDeptRepository.Select
            .WhereIf(conditionDeptDto.Name != string.Empty, a => a.Name == conditionDeptDto.Name)
            .WhereIf(conditionDeptDto?.IsEnabled != null, a => a.IsEnabled == conditionDeptDto!.IsEnabled)
            .IncludeMany(a => a.SysUserList)
            .IncludeMany(a => a.SysRoleList)
            .AsTreeCte()
            .ToTreeListAsync(cancellationToken).ConfigureAwait(false);
        return sysDept?.Count > 0 ? [.. sysDept.Select(t => new SysDeptDto().AdaptIn(t))] : [];
    }

    public async Task<List<SysDeptDto>> GetAllAsTreeAsync(CancellationToken cancellationToken = default)
    {
        var sysDeptList = await sysDeptRepository.Select
            .Include(a => a.Parent)
            .ToTreeListAsync(cancellationToken).ConfigureAwait(false);
        return sysDeptList?.Count > 0 ? [.. sysDeptList.Select(t => new SysDeptDto().AdaptIn(t))] : [];
    }

    public async Task<List<SysDeptDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var sysDeptList = await sysDeptRepository.Select
            .Include(a => a.Parent)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return sysDeptList?.Count > 0 ? [.. sysDeptList.Select(t => new SysDeptDto().AdaptIn(t))] : [];
    }

    public async Task<bool> InsertAsync(SysDeptDto sysDeptDto, CancellationToken cancellationToken = default)
    {
        var addAsync = await sysDeptRepository.InsertOrUpdateAsync(sysDeptDto.AdaptTo(), cancellationToken).ConfigureAwait(false);
        return addAsync is not null;
    }

    public async Task<bool> UpdateAsync(SysDeptDto sysDeptDto, CancellationToken cancellationToken = default)
    {
        var updateAsync = await sysDeptRepository.UpdateAsync(sysDeptDto.AdaptTo(), cancellationToken).ConfigureAwait(false);
        return updateAsync == 1;
    }

    public Task<bool> DeleteAsync(SysDeptDto sysDeptDto, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public bool Delete(SysDeptDto sysDeptDto)
    {
        var deleteListAsync = sysDeptRepository
            .Where(t => t.Id == sysDeptDto.Id)
            .AsTreeCte()
            .ToUpdate()
            .Set(a => a.IsDeleted, true)
            .ExecuteAffrows(); //软删除结点下的所有记录
        return deleteListAsync > 0;
    }

    public List<SysDeptDto> BuildDepartmentTree(List<SysDeptDto> depts)
    {
        var returnList = new List<SysDeptDto>();
        if (depts.Count == 0)
            return returnList;
        foreach (var dept in depts.Where(dept => dept.ParentId == ConstantHelper.NegValue))
        {
            dept.Depth = 0;
            RecursionFn(depts, dept);
            returnList.Add(dept);
        }

        return returnList;

        void RecursionFn(List<SysDeptDto> list, SysDeptDto sysDeptDto, int depth = 0)
        {
            // 得到子节点列表
            var childList = list.Where(p => p.ParentId == sysDeptDto.Id).ToList();
            childList.ForEach(t => t.Depth = depth + 1);
            sysDeptDto.ChildList = childList;

            foreach (var item in childList.Where(item => list.Any(p => p.ParentId == item.Id)))
            {
                RecursionFn(list, item, depth + 1);
            }
        }
    }

    public List<SysDeptDto> FlattenDepartmentTree(List<SysDeptDto> rootList)
    {
        var flattenListResult = new List<SysDeptDto>();
        foreach (var rootdept in rootList)
        {
            var resultList = new List<SysDeptDto>();
            Flatten(rootdept, resultList);
            flattenListResult.AddRange(resultList);
        }

        return flattenListResult;

        static void Flatten(SysDeptDto node, List<SysDeptDto> list)
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