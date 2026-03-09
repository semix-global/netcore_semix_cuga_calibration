using Core.Models.Enums.Optics;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;

namespace Core.Services.Interfaces;

public interface ICalibrationLaserService
{
    /// <summary>
    /// 连接
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> Connect();

    #region 光路同心校准

    /// <summary>
    /// 获取激光四象限PD的坐标
    /// </summary>
    /// <returns>PD1:四象限PD1的坐标，PD2:四象限PD2的坐标</returns>
    SxExecuteRet<(Point PD1Point, Point PD2Point)> GetLaserBeamPoint();

    /// <summary>
    /// 获取原点坐标
    /// </summary>
    /// <returns>原点PD1 PD2的坐标</returns>
    SxExecuteRet<(Point PD1Point, Point PD2Point)> GetLaserBeamOriginPoint();

    /// <summary>
    /// 调整反射镜让光路的偏移量减小
    /// </summary>
    /// <returns>调整是否成功</returns>
    SxExecuteRet<bool> AdjustBeamStabilizer(bool isEnable);

    #endregion 光路同心校准

    #region 激光功率

    /// <summary>
    /// 读取台面功率计的光强值
    /// </summary>
    /// <returns>返回台面功率计的光强值</returns>
    SxExecuteRet<double> GetOpticalMeasurePower();

    /// <summary>
    /// 获取cuga配置的激光光强的信息列表
    /// </summary>
    /// <returns>cuga配置的激光光强信息列表</returns>
    SxExecuteRet<IReadOnlyList<LaserLightInformation>> GetLaserLightInformations();

    #endregion 激光功率

    #region 任意波形发生器Arbitrary Waveform Generator

    /// <summary>
    /// 设置照明mag
    /// </summary>
    /// <param name="productivityInformation">产率</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleOpticsMagType(ProductivityInformation productivityInformation);

    /// <summary>
    /// 切换照明扫描模式
    /// </summary>
    /// <param name="opticsAODWorkingModeEnum">扫描模式</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum opticsAODWorkingModeEnum);

    /// <summary>
    /// 设置AOD延迟的值, 并切换Mag
    /// </summary>
    /// <param name="productivityInformation">产率</param>
    /// <param name="prescanAODDelay">Prescan AOD延迟</param>
    /// <param name="chirpAODDelay">Chirp AOD延迟</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetAODDelayValue(ProductivityInformation productivityInformation, double prescanAODDelay, double chirpAODDelay);

    /// <summary>
    /// 下发PrescanAOD波形
    /// </summary>
    /// <param name="productivityInformation">产率</param>
    /// <param name="coefficient">波形功率系数(1表示100%, 0表示0%)</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetDefaultPrescanAODWaveProfileByCoefficient(ProductivityInformation productivityInformation, double coefficient);

    /// <summary>
    /// 下发PrescanAOD波形
    /// </summary>
    /// <param name="opticsIlluminationModeEnum">照明光入射方式</param>
    /// <param name="prescanAODWaveProfiles">prescanAOD波形</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetPrescanAODWaveProfiles(OpticsIlluminationModeEnum opticsIlluminationModeEnum, IReadOnlyList<PrescanAODWaveformProfile> prescanAODWaveProfiles);

    /// <summary>
    /// 下发ChirpAOD波形
    /// </summary>
    /// <param name="productivityInformation">产率</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetDefaultChirpAODWaveProfile(ProductivityInformation productivityInformation);

    /// <summary>
    /// 下发ChirpAOD波形
    /// </summary>
    /// <param name="opticsIlluminationModeEnum">照明光入射方式</param>
    /// <param name="chirpAODWaveProfiles">chirpAOD波形</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetChirpAODWaveProfiles(OpticsIlluminationModeEnum opticsIlluminationModeEnum, IReadOnlyList<ChirpAODWaveformProfile> chirpAODWaveProfiles);

    #endregion 任意波形发生器Arbitrary Waveform Generator
}