using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.DarkField;
using Net.Utilities.Models;
using Semix.CoreLib;

namespace Core.Services.Interfaces;

/// <summary>
/// AutoFocus自动聚焦服务
/// </summary>
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
    SxExecuteRet<(Point PD1, Point PD2)> GetLaserBeamPosition();

    /// <summary>
    /// 获取原点坐标
    /// </summary>
    /// <returns>原点PD1 PD2的坐标</returns>
    SxExecuteRet<(Point PD1, Point PD2)> GetLaserOriginPosition();

    /// <summary>
    /// 调整反射镜让光路的偏移量减小
    /// </summary>
    /// <returns>调整是否成功</returns>
    SxExecuteRet<bool> AdjustmentOfReflector(bool isEnable);

    #endregion 光路同心校准

    #region 激光功率

    #region 获取激光功率

    /// <summary>
    /// 读取台面功率计的光强值
    /// </summary>
    /// <returns>返回台面功率计的光强值</returns>
    SxExecuteRet<double> GetLaserPowerMeterLightIntensity();

    #endregion 获取激光功率

    #region 扫描线功率文件读取和设置

    /// <summary>
    /// 读取(寄存器数量, 补0个数, 波形幅值列表)
    /// </summary>
    /// <param name="filePath">波形幅值文件</param>
    /// <param name="coefficient">波形功率系数(1表示100%, 0表示0%)</param>
    /// <returns>扫描线功率</returns>
    SxExecuteRet<DarkFieldPrescanDto> ReadPrescanByFile(string filePath, double coefficient);

    /// <summary>
    /// 设置上传的功率PrescanList
    /// </summary>
    /// <param name="darkFieldPrescanDto">波形幅值列表</param>
    /// <param name="prescanRateList">波形比例列表</param>
    /// <returns>上传的功率PrescanList</returns>
    SxExecuteRet<DarkFieldPrescanDto> SetPrescanByRate(DarkFieldPrescanDto darkFieldPrescanDto, List<double> prescanRateList);

    #endregion 扫描线功率文件读取和设置

    #region 设置扫描线功率

    /// <summary>
    ///设置mag
    /// </summary>
    /// <param name="yOpticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SendOpticsMagType(OpticsMagTypeEnum yOpticsMagTypeEnum);

    /// <summary>
    /// 下发默认扫描线功率给cuga
    /// </summary>
    /// <param name="yOpticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="coefficient">波形功率系数(1表示100%, 0表示0%)</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SendPrescanByCoefficient(OpticsMagTypeEnum yOpticsMagTypeEnum, double coefficient);

    /// <summary>
    /// 设置饱和值
    /// </summary>
    /// <param name="val">饱和值</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SendSaturationValue(double val);

    /// <summary>
    /// 下发扫描线功率给cuga
    /// </summary>
    /// <param name="darkFieldPrescanDto">扫描线功率</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SendPrescanByList(DarkFieldPrescanDto darkFieldPrescanDto);

    #endregion 设置扫描线功率

    #endregion 激光功率

    #region Chirp AOD

    /// <summary>
    /// 通过chirpAod波形文件路径命名，获得dto参数
    /// </summary>
    /// <param name="filePath">chirpAod波形文件路径</param>
    /// <returns>ChirpAOD波形</returns>
    public SxExecuteRet<DarkFieldChirpAodWaveDto> ReadChirpAodByCustomFile(string filePath);

    /// <summary>
    /// 通过chirpAod cuga默认波形文件路径命名，获得dto参数
    /// </summary>
    /// <param name="filePath">chirpAod波形文件路径</param>
    /// <returns>ChirpAOD波形</returns>
    public SxExecuteRet<DarkFieldChirpAodWaveDto> ReadChirpAodByConfigFile(string filePath);

    /// <summary>
    /// 从当前的波形文件的路径中查找是否存在和输入音包长度和输入变化率相同的波形文件
    /// </summary>
    /// <param name="currentDarkFieldChirpAodWaveDto">当前的波形文件</param>
    /// <param name="rateChange">变化率</param>
    /// <returns>符合条件的ChirpAOD波形</returns>
    SxExecuteRet<DarkFieldChirpAodWaveDto> GetChirpAodByChangeRateFromFile(DarkFieldChirpAodWaveDto currentDarkFieldChirpAodWaveDto, double rateChange);

    /// <summary>
    /// 下发Chirp AOD波形给cuga
    /// </summary>
    /// <param name="darkFieldChirpAodDto">扫描线功率</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SendChirpAodByList(DarkFieldChirpAodWaveDto darkFieldChirpAodDto);

    #endregion Chirp AOD

    #region 暗场相机PMT CIB

    /// <summary>
    /// 读取PMT8 Channel3 传感器信号, 例如: 高mag为一列1000个信号点的值
    /// </summary>
    /// <returns>PMT信号</returns>
    SxExecuteRet<List<double>> GetSensorPmtValueList();

    /// <summary>
    /// 读取任意PMT Channel 传感器信号, 例如: 高mag为一列1000个信号点的值
    /// </summary>
    /// <returns>PMT信号</returns>
    SxExecuteRet<List<double>> GetAnyPmtValueList(int pmtId, int channel);

    /// <summary>
    /// 获取PMT数值
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<List<DarkFieldPmtDataDto>> GetPmtDataList();

    /// <summary>
    /// 将45个光斑的PMTGain数据下发给CIB
    /// </summary>
    /// <param name="pmtData">数据1</param>
    ///  <param name="igData">数据2</param>
    ///  <param name="pmtId">PMTid</param>
    ///  <param name="channel">通道ID</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SendPmtGainToCib(List<string> pmtData, List<string> igData, int pmtId, int channel);

    /// <summary>
    /// 获取第1到15号光斑的CH1,CH2,CH3的CIB采样值
    /// </summary>
    /// <returns>返回第1到15号(PMT id, 光斑的CH1,CH2,CH3的CIB采样值集合)</returns>
    SxExecuteRet<List<DarkFieldPmtDelayDto>> GetCibSamplePmtDelayList();

    /// <summary>
    /// 将第1到15号光斑的CH1,CH2,CH3的CIB采样值重新写入
    /// </summary>
    /// <param name="darkFieldPmtDelayDtoList">返回第1到15号光斑缺陷坐标集合</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetCibSamplePmtDelayList(List<DarkFieldPmtDelayDto> darkFieldPmtDelayDtoList);

    /// <summary>
    /// 切换偏振
    /// </summary>
    /// <param name="opticsPolarizationTypeEnum">偏振</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleOpticsPolarization(OpticsPolarizationTypeEnum opticsPolarizationTypeEnum);

    /// <summary>
    /// 切换扫描模式
    /// </summary>
    /// <param name="opticsAodWorkingModeEnum">扫描模式</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum opticsAodWorkingModeEnum);

    /// <summary>
    /// 设置AOD延迟的值, 并切换Mag
    /// <param name="yOpticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="prescanAodDelay">Prescan AOD延迟</param>
    /// <param name="chirpAodDelay">Chirp AOD延迟</param>
    /// </summary>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetAodDelayValue(OpticsMagTypeEnum yOpticsMagTypeEnum, double prescanAodDelay, double chirpAodDelay);

    /// <summary>
    /// 切换自动增益
    /// </summary>
    /// <param name="enable">是否自动增益</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleEnableAutoGain(bool enable);

    /// <summary>
    /// 切换L0K
    /// </summary>
    /// <param name="enable">是否自动L0k</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> ToggleEnableL0K(bool enable);

    /// <summary>
    /// 设置增益
    /// </summary>
    /// <param name="gain">增益</param>
    /// <returns>是否成功</returns>
    SxExecuteRet<bool> SetGain(double gain);

    #endregion 暗场相机PMT CIB

    #region 暗场采图

    /// <summary>
    /// 获取暗场图片的Y像素高度
    /// </summary>
    /// <param name="yOpticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <returns>图片的Y像素高度</returns>
    SxExecuteRet<int> GetDarkFieldLineScanImageYPixelHeight(OpticsMagTypeEnum yOpticsMagTypeEnum);

    /// <summary>
    /// 获取暗场图片的X SizePerPixel um/pixel
    /// </summary>
    /// <param name="yOpticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="xStageSpeedEnum">X方向线扫描速度</param>
    /// <returns>单位um/pixel(XSizePerPixel)</returns>
    SxExecuteRet<double> GetDarkFieldLineScanImageXSizePerPixel(OpticsMagTypeEnum yOpticsMagTypeEnum, StageSpeedEnum xStageSpeedEnum);

    /// <summary>
    /// 获取暗场图片列表
    /// </summary>
    /// <param name="position">位置</param>
    /// <param name="xWidthPixel">图片X像素宽度</param>
    /// <param name="yOpticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="xStageSpeedEnum">X像素宽度方向线扫描速度</param>
    /// <param name="pmtId">暗场相机 PMT id</param>
    /// <param name="stageCoordinateSystemEnum">暗场采图坐标系系统</param>
    /// <param name="isAutoFocus">是否开启自动聚焦</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <param name="customPrescanAod">(是否自定义PrescanAOD波形,波形功率系数(1表示100%, 0表示0%))</param>
    /// <param name="isCustomChirpAod">是否自定义ChirpAOD波形</param>
    /// <returns>暗场图片列表</returns>
    SxExecuteRet<List<DarkFieldImageDto>> GetDarkFieldLineScanImageList(
        Point position,
        int xWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward,
        (bool IsCustomPrescanAod, double? Coefficient) customPrescanAod,
        bool isCustomChirpAod);

    /// <summary>
    /// 获取暗场图片列表
    /// </summary>
    /// <param name="startPosition">起点位置</param>
    /// <param name="endPosition">终点位置</param>
    /// <param name="yOpticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="xStageSpeedEnum">X像素宽度方向线扫描速度</param>
    /// <param name="pmtId">暗场相机 PMT id</param>
    /// <param name="stageCoordinateSystemEnum">暗场采图坐标系系统</param>
    /// <param name="isAutoFocus">是否开启自动聚焦</param>
    /// <param name="isForward">是否是正向扫图还是反向扫图</param>
    /// <param name="customPrescanAod">(是否自定义PrescanAOD波形,波形功率系数(1表示100%, 0表示0%))</param>
    /// <param name="isCustomChirpAod">是否自定义ChirpAOD波形</param>
    /// <returns>暗场图片列表</returns>
    SxExecuteRet<List<DarkFieldImageDto>> GetDarkFieldLineScanImageList(
        Point startPosition,
        Point endPosition,
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward,
        (bool IsCustomPrescanAod, double? Coefficient) customPrescanAod,
        bool isCustomChirpAod);

    /// <summary>
    ///获得暗场一行长图片对应位置切割后的三个通道图片
    /// </summary>
    /// <param name="machinePositionList">机械坐标集合（分割区域中心点），stageMap使用时输入ideaPosition集合</param>
    /// <param name="xWidthPixel">图片X像素宽度</param>
    /// <param name="yOpticsMagTypeEnum">图片Y像素高度mag类型</param>
    /// <param name="xStageSpeedEnum">X像素宽度方向线扫描速度</param>
    /// <param name="pmtId">暗场相机 PMT id</param>
    /// <param name="stageCoordinateSystemEnum">暗场采图坐标系系统</param>
    /// <param name="isAutoFocus">是否开启自动聚焦</param>
    /// <param name="coefficient">波形功率系数(1表示100%, 0表示0%)</param>
    /// <returns>明场位置，切割后三个通道图片</returns>
    SxExecuteRet<List<List<DarkFieldImageDto>>> GetChuckDarkFieldRowLineScanImageList(
        List<Point> machinePositionList,
        int xWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        (bool IsCustomPrescanAod, double? Coefficient) customPrescanAod,
        bool isCustomChirpAod);

    #endregion 暗场采图
}