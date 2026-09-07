using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Config;
using Core.Models.Models.Common.Pattern;
using Local.SQL.DB.Providers.Models.Entities.DTO;
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
    /// 登录设备
    /// </summary>
    /// <param name="user">用户</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户</returns>
    Task<SxExecuteRet<SysUserDTO>> LoginAsync(SysUserDTO user, CancellationToken cancellationToken);

    /// <summary>
    /// 读取设备编码
    /// </summary>
    /// <returns>设备编码</returns>
    SxExecuteRet<string> GetDeviceCode();

    /// <summary>
    /// 读取设备CUGA版本
    /// </summary>
    /// <returns>设备CUGA版本</returns>
    SxExecuteRet<string> GetDeviceCUGAVersion();

    /// <summary>
    /// 获得cuga应用的校准Result文件全路径文件名
    /// </summary>
    /// <returns>result文件路径</returns>
    SxExecuteRet<string> GetCalibrationFilePath();

    /// <summary>
    /// 获得cuga注册的用户信息
    /// </summary>
    /// <returns>result文件路径</returns>
    SxExecuteRet<IReadOnlyList<SysUserDTO>> GetRegisteredUsersInformation();

    /// <summary>
    /// 获取prescan默认波形列表
    /// </summary>
    /// <param name="productivityInformation">产率</param>
    /// <returns>prescan波形列表</returns>
    SxExecuteRet<IReadOnlyList<PrescanAODWaveformProfile>> GetPrescanAODWaveProfiles(ProductivityInformation productivityInformation);

    /// <summary>
    /// 获取chirp默认波形列表
    /// </summary>
    /// <param name="productivityInformation">产率</param>
    /// <returns>chirp波形列表</returns>
    SxExecuteRet<IReadOnlyList<ChirpAODWaveformProfile>> GetChirpAODWaveProfiles(ProductivityInformation productivityInformation);

    /// <summary>
    /// 设置prescan默认波形列表
    /// </summary>
    /// <param name="productivityInformation">产率</param>
    /// <param name="filePath">波形文件路径</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetPrescanAODWaveformConfiguration(ProductivityInformation productivityInformation, string filePath);

    /// <summary>
    /// 设置chirp默认波形列表
    /// </summary>
    /// <param name="productivityInformation">产率</param>
    /// <param name="filePath">波形文件路径</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetChirpAODWaveformConfiguration(ProductivityInformation productivityInformation, string filePath);

    /// <summary>
    /// cuga的硬件状态配置
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<HardwareStateConfig> LoadHardwareConfigs();

    /// <summary>
    /// 获取算法配置的原始图像高度
    /// </summary>
    /// <param name="opticsIlluminationModeEnum">照明光入射方式</param>
    /// <param name="productivityInformation">产率</param>
    /// <returns>图像像素高度</returns>
    SxExecuteRet<(double originalHeight, double StartYPixel, double EndYPixel)> GetDefaultImageYPixelHeight(ProductivityInformation productivityInformation);
}