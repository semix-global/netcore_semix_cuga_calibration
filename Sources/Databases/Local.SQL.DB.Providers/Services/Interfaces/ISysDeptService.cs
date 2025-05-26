using Local.SQL.DB.Providers.Models.Entities.DTO;

namespace Local.SQL.DB.Providers.Services.Interfaces;

public interface ISysDeptService
{
    /// <summary>
    /// 级联更新开关，使能状态才会更新
    /// </summary>
    /// <param name="isEnable"></param>
    void EnableCascadeSave(bool isEnable);

    /// <summary>
    /// 获取部门信息
    /// </summary>
    /// <param name="id">id array</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门信息</returns>
    Task<SysDeptDto?> GetAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取部门信息列表
    /// </summary>
    /// <param name="ids">id array</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门信息列表</returns>
    Task<List<SysDeptDto>> GetAsync(long[] ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// 条件查询获取部门信息列表
    /// </summary>
    /// <param name="conditionDeptDto">条件dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门信息列表</returns>
    Task<List<SysDeptDto>> GetByConditionAsync(SysDeptDto conditionDeptDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 递归条件查询获取部门信息列表
    /// </summary>
    /// <param name="conditionDeptDto">条件dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门信息列表</returns>
    Task<List<SysDeptDto>> GetByConditionAsTreeAsync(SysDeptDto conditionDeptDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 递归获取全部部门列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门信息列表</returns>
    Task<List<SysDeptDto>> GetAllAsTreeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取全部部门信息列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门信息列表</returns>
    Task<List<SysDeptDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 增加部门信息列表
    /// </summary>
    /// <param name="sysDeptDto">待新增的dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> InsertAsync(SysDeptDto sysDeptDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改部门信息列表
    /// </summary>
    /// /// <param name="sysDeptDto">待修改的dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateAsync(SysDeptDto sysDeptDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除部门信息列表(级联删除)
    /// </summary>
    /// /// <param name="sysDeptDto">待删除的dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DeleteAsync(SysDeptDto sysDeptDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除部门信息列表(递归删除)
    /// </summary>
    /// /// <param name="sysDeptDto">待删除的dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    bool Delete(SysDeptDto sysDeptDto);

    /// <summary>
    /// 构建树结构
    /// </summary>
    /// <param name="depts">部门列表</param>
    /// <returns>树结构</returns>
    List<SysDeptDto> BuildDepartmentTree(List<SysDeptDto> depts);

    /// <summary>
    /// 展开树结构
    /// </summary>
    /// <param name="rootList">部门列表</param>
    /// <returns>展开后集合</returns>
    List<SysDeptDto> FlattenDepartmentTree(List<SysDeptDto> rootList);
}