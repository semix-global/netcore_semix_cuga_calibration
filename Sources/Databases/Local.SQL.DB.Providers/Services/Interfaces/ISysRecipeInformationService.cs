using Local.SQL.DB.Providers.Models.Entities.DTO;

namespace Local.SQL.DB.Providers.Services.Interfaces;

public interface ISysRecipeInformationService
{
    /// <summary>
    /// 条件查询获取配方信息列表
    /// </summary>
    /// <param name="conditionRecipeInfoDto">条件dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配方信息列表</returns>
    Task<List<SysRecipeInformationDto>> GetByConditionAsync(SysRecipeInformationDto conditionRecipeInfoDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取全部配方信息列表
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配方信息列表</returns>
    Task<List<SysRecipeInformationDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 增加配方信息列表
    /// </summary>
    /// <param name="sysRecipeInformationDto">待新增的dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> InsertAsync(SysRecipeInformationDto sysRecipeInformationDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改配方信息列表
    /// </summary>
    /// /// <param name="sysRecipeInformationDto">待修改的dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> UpdateAsync(SysRecipeInformationDto sysRecipeInformationDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除配方信息列表
    /// </summary>
    /// /// <param name="sysRecipeInformationDto">待删除的dto</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>是否成功</returns>
    Task<bool> DeleteAsync(SysRecipeInformationDto sysRecipeInformationDto, CancellationToken cancellationToken = default);
}