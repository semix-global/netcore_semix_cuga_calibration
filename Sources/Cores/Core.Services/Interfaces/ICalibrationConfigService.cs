using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
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
    /// 读取设备编码
    /// </summary>
    /// <returns>设备编码</returns>
    SxExecuteRet<string> GetDeviceCode();

    /// <summary>
    /// 获得cuga应用的校准Result文件全路径文件名
    /// </summary>
    /// <returns>result文件路径</returns>
    SxExecuteRet<string> GetCalibrationFilePath();

    /// <summary>
    /// 获取prescan默认波形列表
    /// </summary>
    /// <param name="productivityInformation">产率</param>
    /// <returns>prescan波形列表</returns>
    SxExecuteRet<IReadOnlyList<PrescanAODWaveformProfile>> GetPrescanAODWaveProfiles(ProductivityInformation productivityInformation, OpticsIncidentModeEnum opticsIncidentModeEnum);

    /// <summary>
    /// 获取chirp默认波形列表
    /// </summary>
    /// <param name="productivityInformation">产率</param>
    /// <returns>chirp波形列表</returns>
    SxExecuteRet<IReadOnlyList<ChirpAODWaveformProfile>> GetChirpAODWaveProfiles(ProductivityInformation productivityInformation, OpticsIncidentModeEnum opticsIncidentModeEnum);
    
    /*/// <summary>
    /// 获取prescan默认波形列表
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetPrescanAODWaveProfiles(ProductivityInformation productivityInformation, string filePath, OpticsIncidentModeEnum opticsIncidentModeEnum);

    /// <summary>
    /// 获取chirp默认波形列表
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetChirpAODWaveProfiles(ProductivityInformation productivityInformation, string filePath, OpticsIncidentModeEnum opticsIncidentModeEnum);*/
}