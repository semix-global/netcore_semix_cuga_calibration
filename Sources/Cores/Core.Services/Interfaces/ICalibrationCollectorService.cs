using Core.Models.Enums.Collector;
using Cuga.Data.DataStruct.Optics;
using Semix.CoreLib;

namespace Core.Services.Interfaces;

public interface ICalibrationCollectorService
{
    /// <summary>
    /// 连接
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> Connect();

    /// <summary>
    /// 切换采集偏振
    /// </summary>
    /// <returns>偏振</returns>
    SxExecuteRet<CollectorPolarizationModeEnum> GetPolarizationMode();

    /// <summary>
    /// 切换采集偏振
    /// </summary>
    /// <param name="collectorPolarizationModeEnum">偏振</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetPolarizationMode(CollectorPolarizationModeEnum collectorPolarizationModeEnum);
}