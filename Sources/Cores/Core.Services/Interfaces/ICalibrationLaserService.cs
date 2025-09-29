using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.DarkField;
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
    SxExecuteRet<double> GetOpticalPowerMeter();

    /// <summary>
    /// 获取cuga配置的激光光强的信息列表
    /// </summary>
    /// <returns>cuga配置的激光光强信息列表</returns>
    SxExecuteRet<IReadOnlyList<LaserLightInformation>> GetLaserLightInformationList();

    /// <summary>
    /// 功率等级和激光光强的信息互转
    /// </summary>
    /// <param name="level">功率等级</param>
    /// <returns>激光光强的信息</returns>
    SxExecuteRet<LaserLightInformation> LevelToLaserLightInformation(double level);

    /// <summary>
    /// 功率系数和激光光强的信息互转
    /// </summary>
    /// <param name="coefficient">功率系数</param>
    /// <returns>激光光强的信息</returns>
    SxExecuteRet<LaserLightInformation> CoefficientToLaserLightInformation(double coefficient);

    #endregion 激光功率

    #region 任意波形发生器Arbitrary Waveform Generator

    /// <summary>
    /// 设置mag
    /// </summary>
    /// <param name="opticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleOpticsMagType(OpticsMagTypeEnum opticsMagTypeEnum);

    /// <summary>
    /// 切换扫描模式
    /// </summary>
    /// <param name="opticsAodWorkingModeEnum">扫描模式</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleOpticsAODWorkingMode(OpticsAodWorkingModeEnum opticsAodWorkingModeEnum);

    /// <summary>
    /// 切换偏振
    /// </summary>
    /// <param name="opticsPolarizationTypeEnum">偏振</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleOpticsPolarization(OpticsPolarizationTypeEnum opticsPolarizationTypeEnum);

    /// <summary>
    /// 设置AOD延迟的值, 并切换Mag
    /// <param name="opticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="prescanAodDelay">Prescan AOD延迟</param>
    /// <param name="chirpAodDelay">Chirp AOD延迟</param>
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetAODDelayValue(OpticsMagTypeEnum opticsMagTypeEnum, double prescanAodDelay, double chirpAodDelay);

    /// <summary>
    /// 下发默认扫描线功率给cuga
    /// </summary>
    /// <param name="opticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="coefficient">波形功率系数(1表示100%, 0表示0%)</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetDefaultPrescanAODWaveProfileByCoefficient(OpticsMagTypeEnum opticsMagTypeEnum, double coefficient);

    /// <summary>
    /// 下发PrescanAod波形给cuga
    /// </summary>
    /// <param name="prescanAODWaveProfiles">prescanAOD波形</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetPrescanAODWaveProfiles(IReadOnlyList<PrescanAODWaveformProfile> prescanAODWaveProfiles);

    /// <summary>
    /// 下发ChirpAOD波形给cuga
    /// </summary>
    /// <param name="opticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetDefaultChirpAODWaveProfile(OpticsMagTypeEnum opticsMagTypeEnum);

    /// <summary>
    /// 下发ChirpAOD波形给cuga
    /// </summary>
    /// <param name="chirpAODWaveProfiles">chirpAOD波形</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetChirpAODWaveProfiles(IReadOnlyList<ChirpAODWaveformProfile> chirpAODWaveProfiles);

    #region 波形生成

    /// <summary>
    /// 根据prescan参数生成prescan波形列表
    /// </summary>
    /// <param name="generatePrescanAODWaveformParam">prescan参数</param>
    /// <returns>prescan波形列表</returns>
    SxExecuteRet<IReadOnlyList<PrescanAODWaveformProfile>> GeneratePrescanAodWaves(GeneratePrescanAODWaveformParam generatePrescanAODWaveformParam);

    /// <summary>
    /// 根据chirp参数生成chirp波形列表
    /// </summary>
    /// <param name="generateChirpAODWaveformParam">chirp参数</param>
    /// <returns>chirp波形列表</returns>
    SxExecuteRet<IReadOnlyList<ChirpAODWaveformProfile>> GenerateChirpAodWaves(GenerateChirpAODWaveformParam generateChirpAODWaveformParam);

    #endregion 波形生成

    #endregion 任意波形发生器Arbitrary Waveform Generator

    #region 暗场相机CIB

    #region Control

    /// <summary>
    /// 设置CIB采集模式
    /// 所有PMT Id, 所有Channel Id: (PMT Id: -1, channelId : -1)<br />
    /// 当前PMT Id, 所有Channel Id: (PMT Id: > 0, channelId : -1)<br />
    /// 当前PMT Id, 当前Channel Id: (PMT Id: > 0, channelId : > 0)
    /// </summary>
    /// <param name="cIbConfiguration"></param>
    /// <param name="pmtId"></param>
    /// <param name="channelId"></param>
    /// <returns></returns>
    SxExecuteRet<bool> ToggleCIBControlTypeAndProfileType(CIBConfiguration cIbConfiguration, int pmtId, int channelId);

    /// <summary>
    /// 切换自动增益<br/>
    /// 所有PMT Id, 所有Channel Id: (PMT Id: -1, channelId : -1)<br />
    /// 当前PMT Id, 所有Channel Id: (PMT Id: > 0, channelId : -1)<br />
    /// 当前PMT Id, 当前Channel Id: (PMT Id: > 0, channelId : > 0)
    /// </summary>
    /// <param name="enable">是否自动增益</param>
    /// <param name="pmtId">PMT ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleEnableAutoGainControl(bool enable, int pmtId, int channelId);

    /// <summary>
    /// 切换Log反差模式<br/>
    /// 所有PMT Id, 所有Channel Id: (PMT Id: -1, channelId : -1)<br />
    /// 当前PMT Id, 所有Channel Id: (PMT Id: > 0, channelId : -1)<br />
    /// 当前PMT Id, 当前Channel Id: (PMT Id: > 0, channelId : > 0)
    /// </summary>
    /// <param name="cibProfileModeEnum">数据显示模式</param>
    /// <param name="pmtId">PMT ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleProfileMode(CIBProfileModeEnum cibProfileModeEnum, int pmtId, int channelId);

    /// <summary>
    /// 切换Mark模式<br/>
    /// 所有PMT Id, 所有Channel Id: (PMT Id: -1, channelId : -1)<br />
    /// 当前PMT Id, 所有Channel Id: (PMT Id: > 0, channelId : -1)<br />
    /// 当前PMT Id, 当前Channel Id: (PMT Id: > 0, channelId : > 0)
    /// </summary>
    /// <param name="enable">是否Mark模式</param>
    /// <param name="pmtId">PMT ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleEnableMarkMode(bool enable, int pmtId, int channelId);

    /// <summary>
    /// 切换所有PMT L0K<br/>
    /// 所有PMT Id, 所有Channel Id: (PMT Id: -1, channelId : -1)<br />
    /// 当前PMT Id, 所有Channel Id: (PMT Id: > 0, channelId : -1)<br />
    /// 当前PMT Id, 当前Channel Id: (PMT Id: > 0, channelId : > 0)
    /// </summary>
    /// <param name="enable">是否自动L0k</param>
    /// <param name="pmtId">PMT ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleEnableL0K(bool enable, int pmtId, int channelId);

    /// <summary>
    /// 设置增益<br/>
    /// 所有PMT Id, 所有Channel Id: (PMT Id: -1, channelId : -1)<br />
    /// 当前PMT Id, 所有Channel Id: (PMT Id: > 0, channelId : -1)<br />
    /// 当前PMT Id, 当前Channel Id: (PMT Id: > 0, channelId : > 0)
    /// </summary>
    /// <param name="gain">增益</param>
    /// <param name="pmtId">PMT ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetGain(double gain, int pmtId, int channelId);

    /// <summary>
    /// 设置饱和值
    /// </summary>
    /// <param name="saturation">饱和值</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetSaturation(double saturation);

    #endregion Control

    #region CIB 数据

    /// <summary>
    /// 获取CIB ID列表
    /// </summary>
    /// <returns>CIB ID列表</returns>
    SxExecuteRet<IReadOnlyList<(int PmtId, bool IsUsed, IReadOnlyList<int> ChannelIdList)>> GetCIBConfigList();

    /// <summary>
    /// 读取任意PMT Channel 数据, 不支持群发
    /// </summary>
    /// <param name="count">同一个PMT Sense Channel数据的数量</param>
    /// <param name="pmtId">PMT ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <returns>PMT Channel 数据</returns>
    SxExecuteRet<IReadOnlyList<IReadOnlyList<double>>> GetCIBOfPMTDataList(int count, int pmtId, int channelId);

    /// <summary>
    /// 获取PMT数值, 不支持群发
    /// </summary>
    /// <returns>获取PMT数值</returns>
    SxExecuteRet<IReadOnlyList<DarkFieldPmtDataDto>> GetCIBOfPMTDataList();

    /// <summary>
    /// 读取任意PMT Sense Channel 数据, 不支持群发
    /// </summary>
    /// <param name="count">同一个PMT Sense Channel数据的数量</param>
    /// <param name="pmtId">PMT ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <returns>PMT Sense Channel 多次数据</returns>
    SxExecuteRet<IReadOnlyList<IReadOnlyList<double>>> GetCIBOfSenseDataList(int count, int pmtId, int channelId);

    #endregion CIB 数据

    /// <summary>
    /// 获取第1到15号光斑的CH1,CH2,CH3的CIB采样值
    /// </summary>
    /// <returns>返回第1到15号(PMT id, 光斑的CH1,CH2,CH3的CIB采样值集合)</returns>
    SxExecuteRet<IReadOnlyList<DarkFieldPmtDelayDto>> GetCIBDelayList();

    /// <summary>
    /// 将第1到15号光斑的CH1,CH2,CH3的CIB采样值重新写入
    /// </summary>
    /// <param name="darkFieldPmtDelayDtoList">返回第1到15号光斑缺陷坐标集合</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetCIBDelayList(IReadOnlyList<DarkFieldPmtDelayDto> darkFieldPmtDelayDtoList);

    /// <summary>
    /// 下发CIB增益波形给cuga
    /// </summary>
    /// <param name="gainList">PMT增益电压值</param>
    /// <param name="pmtId">PMT ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetCIBChirp(IReadOnlyList<double> gainList, int pmtId, int channelId);

    /// <summary>
    /// 将45个光斑的PMTGain数据下发给CIB
    /// </summary>
    /// <param name="pmtData">数据1</param>
    /// <param name="igData">数据2</param>
    /// <param name="pmtId">PMT ID</param>
    /// <param name="channelId">Channel ID</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SendPMTGain(List<string> pmtData, List<string> igData, int pmtId, int channelId);

    #endregion 暗场相机CIB

    #region 暗场采图

    /// <summary>
    /// 自动聚焦
    /// </summary>
    /// <param name="calChipSiteModelEnum">CalChip模式</param>
    /// <param name="pmtId">光斑ID</param>
    /// <param name="coefficient">波形功率系数(1表示100%, 0表示0%) null表示用cuga配置值</param>
    /// <param name="point">位置 null表示用cuga配置值</param>
    /// <returns>RTFC返回AfEcs和Af电机值</returns>
    SxExecuteRet<(double Ecs, double AfMotor)> RuntimeAfCalibration(
        CalChipSiteModelEnum calChipSiteModelEnum,
        int pmtId,
        double? coefficient = null,
        Point? point = null);

    /// <summary>
    /// 获取暗场图片的Y像素高度
    /// </summary>
    /// <param name="opticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="isCuttingPixelHeight">是否是不切割像素高度</param>
    /// <returns>图片的Y像素高度</returns>
    SxExecuteRet<int> GetDarkFieldLineScanImageYPixelHeight(OpticsMagTypeEnum opticsMagTypeEnum, bool isCuttingPixelHeight);

    /// <summary>
    /// 获取暗场图片列表
    /// </summary>
    /// <param name="position">位置</param>
    /// <param name="xWidthPixel">图片X像素宽度</param>
    /// <param name="opticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="xStageSpeedEnum">X像素宽度方向线扫描速度</param>
    /// <param name="pmtId">暗场相机 PMT id</param>
    /// <param name="stageCoordinateSystemEnum">暗场采图坐标系系统</param>
    /// <param name="isAutoFocus">是否开启自动聚焦</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <returns>暗场图片列表</returns>
    SxExecuteRet<List<DarkFieldImageDto>> GetDarkFieldLineScanImageList(
        Point position,
        int xWidthPixel,
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward);

    /// <summary>
    /// 获取暗场图片列表
    /// </summary>
    /// <param name="startPosition">起点位置</param>
    /// <param name="endPosition">终点位置</param>
    /// <param name="opticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="xStageSpeedEnum">X像素宽度方向线扫描速度</param>
    /// <param name="pmtId">暗场相机 PMT id</param>
    /// <param name="stageCoordinateSystemEnum">暗场采图坐标系系统</param>
    /// <param name="isAutoFocus">是否开启自动聚焦</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <returns>暗场图片列表</returns>
    SxExecuteRet<List<DarkFieldRawScanImageDto>> GetDarkFieldLineScanImageList(
        Point startPosition,
        Point endPosition,
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward);

    /// <summary>
    ///获得暗场一行长图片对应位置切割后的三个通道图片
    /// </summary>
    /// <param name="machinePositionList">机械坐标集合（分割区域中心点），stageMap使用时输入ideaPosition集合</param>
    /// <param name="xWidthPixel">图片X像素宽度</param>
    /// <param name="xPixelSize">图片X像素尺寸</param>
    /// <param name="opticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="xStageSpeedEnum">X像素宽度方向线扫描速度</param>
    /// <param name="pmtId">暗场相机 PMT id</param>
    /// <param name="stageCoordinateSystemEnum">暗场采图坐标系系统</param>
    /// <param name="isAutoFocus">是否开启自动聚焦</param>
    /// <returns>明场位置，切割后三个通道图片</returns>
    SxExecuteRet<List<List<DarkFieldImageDto>>> GetChuckDarkFieldRowLineScanImageList(
        List<Point> machinePositionList,
        int xWidthPixel,
        double xPixelSize,
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus);

    #endregion 暗场采图

    #region DOE

    /// <summary>
    /// 读取DOE当前角度值
    /// </summary>
    /// <returns>返回角度</returns>
    SxExecuteRet<double> ReadDOECurrentAngle();

    /// <summary>
    /// 下发DOE旋转角度
    /// </summary>
    /// <param name="angle">角度</param>
    /// <returns>返回是否下发成功</returns>
    SxExecuteRet<bool> SetDOEAngle(double angle);

    #endregion
}