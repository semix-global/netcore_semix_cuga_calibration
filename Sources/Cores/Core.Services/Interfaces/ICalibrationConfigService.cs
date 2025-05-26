using Core.Models.Enums.Optics;
using Semix.CoreLib;

namespace Core.Services.Interfaces;

public interface ICalibrationConfigService
{
    /// <summary>
    /// 连接
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> Connect();

    /// <summary>
    /// 获得cuga应用的校准Result文件全路径文件名
    /// </summary>
    /// <returns>result文件路径</returns>
    SxExecuteRet<string> GetCalibrationFilePath();

    /// <summary>
    /// 获取prescan的文件路径
    /// </summary>
    /// <returns>prescan的文件路径</returns>
    SxExecuteRet<string> GetPrescanFilePath(OpticsMagTypeEnum opticsMagTypeEnum);

    /// <summary>
    /// 获取chirp的文件路径
    /// </summary>
    /// <returns>chirp的文件路径</returns>
    SxExecuteRet<string> GetChirpFilePath(OpticsMagTypeEnum opticsMagTypeEnum);
}