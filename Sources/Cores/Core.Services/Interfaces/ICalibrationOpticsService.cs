using Core.Models.Enums.Optics;
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
}