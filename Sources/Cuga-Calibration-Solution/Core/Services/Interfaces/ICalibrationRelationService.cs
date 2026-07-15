namespace CugaCalibration.Core.Services.Interfaces;

public interface ICalibrationRelationService
{
    /// <summary>
    /// 前置条件校验
    /// </summary>
    /// <param name="viewModelType">Calibration ViewModel Type</param>
    /// <returns>是否成功</returns>
    bool DependenciesValidate(Type viewModelType);

    /// <summary>
    /// 树形结构扁平化，刷新ApplicationCookies
    /// </summary>
    /// <param name="message">异常信息</param>
    /// <returns>是否成功</returns>
    bool RefreshRelationConfigCookies(out string message);
}