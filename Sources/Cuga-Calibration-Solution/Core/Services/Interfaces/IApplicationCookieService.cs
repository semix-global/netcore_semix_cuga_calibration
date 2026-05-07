using Core.Models.Models;
using Core.Models.Models.CIB.LineCentricity;
using Core.Models.Models.Common.Cookies;
using Core.Models.Models.Common.Pattern;
using Local.SQL.DB.Providers.Models.Entities.DTO;
using Net.Utilities.Models.Geometries;

namespace CugaCalibration.Core.Services.Interfaces;

public interface IApplicationCookieService
{
    /// <summary>
    /// 更新用户
    /// </summary>
    /// <param name="sysUserDto"></param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateCookieAsync(SysUserDTO sysUserDto, CancellationToken cancellationToken);

    /// <summary>
    /// 根据视图模型查找校准项目
    /// </summary>
    /// <returns>校准项目</returns>
    CalibrationMenu? FindCalibrationItem<TViewModel>();

    /// <summary>
    /// 根据组件名称查找子菜单
    /// </summary>
    /// <returns>子菜单</returns>
    List<SysMenuDTO> FindSysMenuListByRecursionComponent(string component);

    /// <summary>
    /// 获得光斑暗场中心相对偏差值（晶圆坐标系）
    /// </summary>
    /// <param name="result"></param>
    /// <param name="productivityInformation"></param>
    /// <returns></returns>
    IReadOnlyCollection<(int Pmt, Point Offset)> GetLineCentricityMachineOffsetList(IReadOnlyCollection<CIBLineCentricityDTO> result, ProductivityInformation productivityInformation);
}