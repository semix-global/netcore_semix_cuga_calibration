using CugaCalibration.Core.Models;
using Local.SQL.DB.Providers.Models.Entities.DTO;

namespace CugaCalibration.Core.Services.Interfaces;

public interface IApplicationCookieService
{
    /// <summary>
    /// 更新用户
    /// </summary>
    /// <param name="sysUserDto"></param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateCookieAsync(SysUserDto sysUserDto, CancellationToken cancellationToken);

    /// <summary>
    /// 根据视图模型查找校准项目
    /// </summary>
    /// <returns>校准项目</returns>
    CalibrationMenu? FindCalibrationItem<TViewModel>();

    /// <summary>
    /// 根据组件名称查找子菜单
    /// </summary>
    /// <returns>子菜单</returns>
    List<SysMenuDto> FindSysMenuListByRecursionComponent(string component);
}