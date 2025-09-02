using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
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
    /// 获取prescan波形列表
    /// </summary>
    /// <returns>prescan波形列表</returns>
    SxExecuteRet<IReadOnlyList<PrescanAODWaveformProfile>> GetPrescanAODWaveProfileList(OpticsMagTypeEnum opticsMagTypeEnum);

    /// <summary>
    /// 获取chirp波形列表
    /// </summary>
    /// <returns>chirp波形列表</returns>
    SxExecuteRet<IReadOnlyList<ChirpAODWaveformProfile>> GetChirpAODWaveProfileList(OpticsMagTypeEnum opticsMagTypeEnum);
}