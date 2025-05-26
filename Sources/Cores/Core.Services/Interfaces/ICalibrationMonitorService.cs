using Semix.CoreLib;

namespace Core.Services.Interfaces;

public interface ICalibrationMonitorService
{
    /// <summary>
    /// 连接
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> Connect();

    #region 温度监测

    /// <summary>
    /// 监测暗场CIB温度
    /// </summary>
    /// <returns>温度值</returns>
    SxExecuteRet<double> GetCIBCurrentTemperature();

    /// <summary>
    /// 监测明场相机温度
    /// </summary>
    /// <returns>温度值</returns>
    SxExecuteRet<double> GetReviewCameraCurrentTemperature();

    /// <summary>
    /// 监测X轴温度
    /// </summary>
    /// <returns>温度</returns>
    SxExecuteRet<double> GetXAxisCurrentTemperature();

    /// <summary>
    /// 监测Y轴温度
    /// </summary>
    /// <returns>温度</returns>
    SxExecuteRet<double> GetYAxisCurrentTemperature();

    #endregion 温度监测
}