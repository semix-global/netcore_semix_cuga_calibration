using Core.Models.Enums.Optics;
using Core.Models.Models.Common.Pattern;
using Semix.CoreLib;

namespace Core.Services.Interfaces;

public interface ICalibrationOpticsService
{
    /// <summary>
    /// 连接
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> Connect();

    /// <summary>
    /// 获取cuga配置的产率列表
    /// </summary>
    /// <returns>cuga配置的产率列表</returns>
    SxExecuteRet<IReadOnlyList<ProductivityInformation>> GetProductivityInformations();

    /// <summary>
    /// 获取Relay电极位置
    /// </summary>
    /// <param name="opticsIlluminationModeEnum">照明光入射方式</param>
    /// <returns>Relay电极位置mm</returns>
    SxExecuteRet<double> GetRelayMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum);

    /// <summary>
    /// 设置Relay电极位置
    /// </summary>
    /// <param name="opticsIlluminationModeEnum">照明光入射方式</param>
    /// <param name="value">Relay电极位置mm</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetRelayMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value);

    /// <summary>
    /// 切换照明OD滤光片
    /// </summary>
    /// <param name="isEnable">是否开启</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleODFilter(bool isEnable);

    /// <summary>
    /// 切换照明切趾
    /// </summary>
    /// <returns>偏振</returns>
    SxExecuteRet<OpticsApodizationModeEnum> GetApodizationMode();

    /// <summary>
    /// 切换照明切趾
    /// </summary>
    /// <param name="opticsApodizationModeEnum">偏振</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetApodizationMode(OpticsApodizationModeEnum opticsApodizationModeEnum);

    /// <summary>
    /// 切换照明偏振
    /// </summary>
    /// <returns>偏振</returns>
    SxExecuteRet<OpticsPolarizationModeEnum> GetPolarizationMode();

    /// <summary>
    /// 切换照明偏振
    /// </summary>
    /// <param name="opticsPolarizationModeEnum">偏振</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetPolarizationMode(OpticsPolarizationModeEnum opticsPolarizationModeEnum);
}