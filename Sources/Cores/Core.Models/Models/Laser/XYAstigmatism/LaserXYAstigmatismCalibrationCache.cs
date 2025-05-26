using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Microscope;
using Core.Models.Enums.Optics;
using Net.Utilities.Models;

namespace Core.Models.Models.Laser.XYAstigmatism;

// ReSharper disable once InconsistentNaming
public sealed partial class LaserXYAstigmatismCalibrationCache : CalibrationCacheBase
{
    /// <summary>
    /// 明场初定位倍率
    /// </summary>
    [ObservableProperty]
    private MicroscopeMagnificationEnum _microscopeMagnificationEnum;

    /// <summary>
    /// 采样率
    /// </summary>
    [ObservableProperty]
    private double _sampleRate = 1064d;

    /// <summary>
    /// mag
    /// </summary>
    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private double _chuckRadius = 150000;

    #region 点位

    [ObservableProperty]
    private Point _findPosition;

    [ObservableProperty]
    private Point _findPositionLow;

    [ObservableProperty]
    private Point _findPositionMiddle;

    [ObservableProperty]
    private Point _findPositionHigh;

    #endregion 点位

    #region find EcsX Z轴参数

    /// <summary>
    /// 暗场ECS X lowMag 默认波形下最佳ECS
    /// </summary>
    [ObservableProperty]
    private double _ecsXLowInitial;

    /// <summary>
    /// 暗场ECS X MidMag 默认波形下最佳ECS
    /// </summary>
    [ObservableProperty]
    private double _ecsXMidInitial;

    /// <summary>
    /// 暗场ECS X HighMag 默认波形下最佳ECS
    /// </summary>
    [ObservableProperty]
    private double _ecsXHighInitial;

    /// <summary>
    /// 暗场ECS upper(中心往上递增次数) lowMag
    /// </summary>
    [ObservableProperty]
    private double _ecsPositionUpperLow;

    /// <summary>
    /// 暗场ECS upper(中心往上递增次数) MiddleMag
    /// </summary>
    [ObservableProperty]
    private double _ecsPositionUpperMiddle;

    /// <summary>
    /// 暗场ECS upper(中心往上递增次数) HighMag
    /// </summary>
    [ObservableProperty]
    private double _ecsPositionUpperHigh;

    /// <summary>
    /// 暗场ECS lower(中心往下递增次数) lowMag
    /// </summary>
    [ObservableProperty]
    private double _ecsPositionLowerLow;

    /// <summary>
    /// 暗场ECS lower(中心往下递增次数) MiddleMag
    /// </summary>
    [ObservableProperty]
    private double _ecsPositionLowerMiddle;

    /// <summary>
    /// 暗场ECS lower(中心往下递增次数) HighMag
    /// </summary>
    [ObservableProperty]
    private double _ecsPositionLowerHigh;

    /// <summary>
    /// 暗场ECS lowMag Z轴步距
    /// </summary>
    [ObservableProperty]
    private double _ecsIntervalLow;

    /// <summary>
    /// 暗场ECS middleMag Z轴步距
    /// </summary>
    [ObservableProperty]
    private double _ecsIntervalMiddle;

    /// <summary>
    /// 暗场ECS highMag Z轴步距
    /// </summary>
    [ObservableProperty]
    private double _ecsIntervalHigh;

    #endregion find EcsX Z轴参数

    #region find EcsY Z轴参数

    /// <summary>
    /// 暗场ECS Y lowMag 默认波形下最佳ECS
    /// </summary>
    [ObservableProperty]
    private double _ecsYLowInitial;

    /// <summary>
    /// 暗场ECS Y MidMag 默认波形下最佳ECS
    /// </summary>
    [ObservableProperty]
    private double _ecsYMidInitial;

    /// <summary>
    /// 暗场ECS Y HighMag 默认波形下最佳ECS
    /// </summary>
    [ObservableProperty]
    private double _ecsYHighInitial;

    /// <summary>
    /// 暗场ECS Y upper(中心往上递增次数) lowMag
    /// </summary>
    [ObservableProperty]
    private double _ecsYPositionUpperLow;

    /// <summary>
    /// 暗场ECS Y upper(中心往上递增次数) MiddleMag
    /// </summary>
    [ObservableProperty]
    private double _ecsYPositionUpperMiddle;

    /// <summary>
    /// 暗场ECS Y upper(中心往上递增次数) HighMag
    /// </summary>
    [ObservableProperty]
    private double _ecsYPositionUpperHigh;

    /// <summary>
    /// 暗场ECS Y lower(中心往下递增次数) lowMag
    /// </summary>
    [ObservableProperty]
    private double _ecsYPositionLowerLow;

    /// <summary>
    /// 暗场ECS Y lower(中心往下递增次数) MiddleMag
    /// </summary>
    [ObservableProperty]
    private double _ecsYPositionLowerMiddle;

    /// <summary>
    /// 暗场ECS Y lower(中心往下递增次数) HighMag
    /// </summary>
    [ObservableProperty]
    private double _ecsYPositionLowerHigh;

    /// <summary>
    /// 暗场ECS Y lowMag Z轴步距
    /// </summary>
    [ObservableProperty]
    private double _ecsYIntervalLow;

    /// <summary>
    /// 暗场ECS Y middleMag Z轴步距
    /// </summary>
    [ObservableProperty]
    private double _ecsYIntervalMiddle;

    /// <summary>
    /// 暗场ECS Y highMag Z轴步距
    /// </summary>
    [ObservableProperty]
    private double _ecsYIntervalHigh;

    #endregion find EcsY Z轴参数

    #region ChipAOD路径

    /// <summary>
    /// 默认波形文件路径 Low
    /// </summary>
    [ObservableProperty]
    private string _chirpAodFilePathLow = string.Empty;

    /// <summary>
    /// 默认波形文件路径 Middle
    /// </summary>
    [ObservableProperty]
    private string _chirpAodFilePathMiddle = string.Empty;

    /// <summary>
    /// 默认波形文件路径 High
    /// </summary>
    [ObservableProperty]
    private string _chirpAodFilePathHigh = string.Empty;

    #endregion ChipAOD路径

    #region 频率参数

    /// <summary>
    /// 初始chirp AOD波形频率 lowMag
    /// </summary>
    [ObservableProperty]
    private double _defaultAodFrequenceLow;

    /// <summary>
    /// 初始chirp AOD波形频率 middleMag
    /// </summary>
    [ObservableProperty]
    private double _defaultAodFrequenceMiddle;

    /// <summary>
    /// 初始chirp AOD波形频率 lowHigh
    /// </summary>
    [ObservableProperty]
    private double _defaultAodFrequenceHigh;

    /// <summary>
    /// Chirp AOD波形频率增长次数 lowMag
    /// </summary>
    [ObservableProperty]
    private int _increaseCountLow;

    /// <summary>
    /// Chirp AOD波形频率增长次数 middleMag
    /// </summary>
    [ObservableProperty]
    private int _increaseCountMiddle;

    /// <summary>
    /// Chirp AOD波形频率增长次数 HighMag
    /// </summary>
    [ObservableProperty]
    private int _increaseCountHigh;

    /// <summary>
    /// Chirp AOD波形频率增长步距 lowMag
    /// </summary>
    [ObservableProperty]
    private double _increaseIntervalLow;

    /// <summary>
    /// Chirp AOD波形频率增长步距 middleMag
    /// </summary>
    [ObservableProperty]
    private double _increaseIntervalMiddle;

    /// <summary>
    /// Chirp AOD波形频率增长步距 HighMag
    /// </summary>
    [ObservableProperty]
    private double _increaseIntervalHigh;

    /// <summary>
    /// 补0个数Low
    /// </summary>
    [ObservableProperty]
    private short _zeroNumLow;

    /// <summary>
    /// 补0个数Middle
    /// </summary>
    [ObservableProperty]
    private short _zeroNumMiddle;

    /// <summary>
    /// 补0个数High
    /// </summary>
    [ObservableProperty]
    private short _zeroNumHigh;

    #endregion 频率参数

    #region FindEcsY频率参数

    /// <summary>
    /// 补0个数Low Y
    /// </summary>
    [ObservableProperty]
    private short _zeroNumLowY;

    /// <summary>
    /// 补0个数Middle Y
    /// </summary>
    [ObservableProperty]
    private short _zeroNumMiddleY;

    /// <summary>
    /// 补0个数High Y
    /// </summary>
    [ObservableProperty]
    private short _zeroNumHighY;

    #endregion FindEcsY频率参数

    #region 波形参数

    [ObservableProperty]
    private short _regNum;

    /// <summary>
    /// 起始chirp AOD波形中心 lowMag
    /// </summary>
    [ObservableProperty]
    private double _centerFrequenceLow;

    /// <summary>
    /// 起始chirp AOD波形中心 middleMag
    /// </summary>
    [ObservableProperty]
    private double _centerFrequenceMiddle;

    /// <summary>
    /// 起始chirp AOD波形中心 lowHigh
    /// </summary>
    [ObservableProperty]
    private double _centerFrequenceHigh;

    /// <summary>
    /// 起始chirp AOD 音包长度 lowMag
    /// </summary>
    [ObservableProperty]
    private double _soundPackageLengthLow;

    /// <summary>
    /// 起始chirp AOD 音包长度 middleMag
    /// </summary>
    [ObservableProperty]
    private double _soundPackageLengthMiddle;

    /// <summary>
    /// 起始chirp AOD 音包长度 lowHigh
    /// </summary>
    [ObservableProperty]
    private double _soundPackageLengthHigh;

    #endregion 波形参数

    #region 阈值

    /// <summary>
    /// XY焦距差值允许范围 lowMag
    /// </summary>
    [ObservableProperty]
    private double _thresholdLow;

    /// <summary>
    /// XY焦距差值允许范围 middleMag
    /// </summary>
    [ObservableProperty]
    private double _thresholdMiddle;

    /// <summary>
    /// XY焦距差值允许范围 lowHigh
    /// </summary>
    [ObservableProperty]
    private double _thresholdHigh;

    /// <summary>
    /// Review XY焦距差值允许范围 lowMag
    /// </summary>
    [ObservableProperty]
    private double _thresholdLowReview;

    /// <summary>
    /// Review XY焦距差值允许范围 middleMag
    /// </summary>
    [ObservableProperty]
    private double _thresholdMiddleReview;

    /// <summary>
    /// Review XY焦距差值允许范围 lowHigh
    /// </summary>
    [ObservableProperty]
    private double _thresholdHighReview;

    #endregion 阈值

    #region 结果

    /// <summary>
    /// lowMag Ecs结果，x=x得分最高高度值，y=y得分最高高度值
    /// </summary>
    [ObservableProperty]
    private Point _findEcsLow;

    /// <summary>
    /// middleMag Ecs结果，x=x得分最高高度值，y=y得分最高高度值
    /// </summary>
    [ObservableProperty]
    private Point _findEcsMiddle;

    /// <summary>
    /// highMag Ecs结果，x=x得分最高高度值，y=y得分最高高度值
    /// </summary>
    [ObservableProperty]
    private Point _findEcsHigh;

    /// <summary>
    /// lowMag 清晰度最高结果
    /// </summary>
    [ObservableProperty]
    private double _findQualityLow;

    /// <summary>
    /// MiddleMag 清晰度最高结果
    /// </summary>
    [ObservableProperty]
    private double _findQualityMiddle;

    /// <summary>
    /// HighMag 清晰度最高结果
    /// </summary>
    [ObservableProperty]
    private double _findQualityHigh;

    /// <summary>
    /// lowMag xyEcs差值最小的频率增量
    /// </summary>
    [ObservableProperty]
    private double _findFrequenceLow;

    /// <summary>
    /// MiddleMag xyEcs差值最小的频率增量
    /// </summary>
    [ObservableProperty]
    private double _findFrequenceMiddle;

    /// <summary>
    /// HighMag xyEcs差值最小的频率增量
    /// </summary>
    [ObservableProperty]
    private double _findFrequenceHigh;

    /// <summary>
    /// lowMag xyEcs最小差值
    /// </summary>
    [ObservableProperty]
    private double _findEcsErrorLow;

    /// <summary>
    /// MiddleMag xyEcs最小差值
    /// </summary>
    [ObservableProperty]
    private double _findEcsErrorMiddle;

    /// <summary>
    /// HighMag xyEcs最小差值
    /// </summary>
    [ObservableProperty]
    private double _findEcsErrorHigh;

    /// <summary>
    /// 生成AOD的波形
    /// </summary>
    [ObservableProperty]
    private List<(double, double)>? _aodWaveSignal;

    /// <summary>
    /// 生成AOD的傅里叶变化后波形
    /// </summary>
    [ObservableProperty]
    private List<(double, double)>? _aodWaveSignalFourier;

    /// <summary>
    /// 关于ecs-quality的结果散点图
    /// </summary>
    [ObservableProperty]
    private List<(double, double)>? _ecsPlotList;

    #endregion 结果

    /// <summary>
    /// 窗口是否获取成功
    /// </summary>
    [ObservableProperty]
    private bool _isGetWindow = true;

    #region 方法

    #region 点位

    public Point SetBrightFieldPosition() =>
        OpticsMagTypeEnum switch
        {
            OpticsMagTypeEnum.Low => FindPositionLow = FindPosition,
            OpticsMagTypeEnum.Middle => FindPositionMiddle = FindPosition,
            OpticsMagTypeEnum.High => FindPositionHigh = FindPosition,
            _ => throw new ArgumentOutOfRangeException()
        };

    public Point GetFindPosition() =>
        OpticsMagTypeEnum switch
        {
            OpticsMagTypeEnum.Low => FindPositionLow,
            OpticsMagTypeEnum.Middle => FindPositionMiddle,
            OpticsMagTypeEnum.High => FindPositionHigh,

            _ => throw new ArgumentOutOfRangeException()
        };

    #endregion 点位

    #region ECS-X

    public (double ZLimitMin, double ZLimitMax, double ZLimitInterval, double FindInitialECS) GetEcsXParams() =>
        OpticsMagTypeEnum switch
        {
            OpticsMagTypeEnum.Low => (EcsPositionUpperLow, EcsPositionLowerLow, EcsIntervalLow, EcsXLowInitial),
            OpticsMagTypeEnum.Middle => (EcsPositionUpperMiddle, EcsPositionLowerMiddle, EcsIntervalMiddle, EcsXMidInitial),
            OpticsMagTypeEnum.High => (EcsPositionUpperHigh, EcsPositionLowerHigh, EcsIntervalHigh, EcsXHighInitial),
            _ => throw new ArgumentOutOfRangeException()
        };

    public double SetEcsXParams(double value) => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => EcsXLowInitial = value,
        OpticsMagTypeEnum.Middle => EcsXMidInitial = value,
        OpticsMagTypeEnum.High => EcsXHighInitial = value,
        _ => throw new ArgumentOutOfRangeException()
    };

    public double GetInitialEcsX() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => EcsXLowInitial,
        OpticsMagTypeEnum.Middle => EcsXMidInitial,
        OpticsMagTypeEnum.High => EcsXHighInitial,
        _ => throw new ArgumentOutOfRangeException()
    };

    #endregion ECS-X

    #region ECS-Y

    public (double ZLimitMin, double ZLimitMax, double ZLimitInterval, double FindInitialECS) GetEcsYParams() =>
        OpticsMagTypeEnum switch
        {
            OpticsMagTypeEnum.Low => (EcsYPositionUpperLow, EcsYPositionLowerLow, EcsYIntervalLow, EcsYLowInitial),
            OpticsMagTypeEnum.Middle => (EcsYPositionUpperMiddle, EcsYPositionLowerMiddle, EcsYIntervalMiddle, EcsYMidInitial),
            OpticsMagTypeEnum.High => (EcsYPositionUpperHigh, EcsYPositionLowerHigh, EcsYIntervalHigh, EcsYHighInitial),
            _ => throw new ArgumentOutOfRangeException()
        };

    public double SetEcsYParams(double value) => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => EcsYLowInitial = value,
        OpticsMagTypeEnum.Middle => EcsYMidInitial = value,
        OpticsMagTypeEnum.High => EcsYHighInitial = value,
        _ => throw new ArgumentOutOfRangeException()
    };

    public (double, double, double, double) SetEcsYParams() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => (EcsYPositionUpperLow = EcsPositionUpperLow, EcsYPositionLowerLow = EcsPositionLowerLow, EcsIntervalLow = EcsYIntervalLow, EcsYLowInitial = EcsXLowInitial),
        OpticsMagTypeEnum.Middle => (EcsYPositionUpperMiddle = EcsPositionUpperMiddle, EcsYPositionLowerMiddle = EcsPositionLowerMiddle, EcsIntervalMiddle = EcsYIntervalMiddle, EcsYMidInitial = EcsXMidInitial),
        OpticsMagTypeEnum.High => (EcsYPositionUpperHigh = EcsPositionUpperHigh, EcsYPositionLowerHigh = EcsPositionLowerHigh, EcsIntervalHigh = EcsYIntervalHigh, EcsYHighInitial = EcsXHighInitial),
        _ => throw new ArgumentOutOfRangeException()
    };

    public double GetInitialEcsY() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => EcsYLowInitial,
        OpticsMagTypeEnum.Middle => EcsYMidInitial,
        OpticsMagTypeEnum.High => EcsYHighInitial,
        _ => throw new ArgumentOutOfRangeException()
    };

    #endregion ECS-Y

    #region ChipAOD路径

    public string GetChirpAodFilePath() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => ChirpAodFilePathLow,
        OpticsMagTypeEnum.Middle => ChirpAodFilePathMiddle,
        OpticsMagTypeEnum.High => ChirpAodFilePathHigh,
        _ => throw new ArgumentOutOfRangeException()
    };

    public string SetChirpAodFilePath(string value) => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => ChirpAodFilePathLow = value,
        OpticsMagTypeEnum.Middle => ChirpAodFilePathMiddle = value,
        OpticsMagTypeEnum.High => ChirpAodFilePathHigh = value,
        _ => throw new ArgumentOutOfRangeException()
    };

    #endregion ChipAOD路径

    #region 频率参数

    public double GetDefaultChirpAodFrequence() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => DefaultAodFrequenceLow,
        OpticsMagTypeEnum.Middle => DefaultAodFrequenceMiddle,
        OpticsMagTypeEnum.High => DefaultAodFrequenceHigh,
        _ => throw new ArgumentOutOfRangeException()
    };

    public (int IncreaseCount, double IncreaseInterval) GetFrequenceParams() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => (IncreaseCountLow, IncreaseIntervalLow),
        OpticsMagTypeEnum.Middle => (IncreaseCountMiddle, IncreaseIntervalMiddle),
        OpticsMagTypeEnum.High => (IncreaseCountHigh, IncreaseIntervalHigh),
        _ => throw new ArgumentOutOfRangeException()
    };

    public (double CenterFrequence, double SoundPackageLengthm, short ZeroNum) GetInitialChirpAodWaveParams() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => (CenterFrequenceLow, SoundPackageLengthLow, ZeroNumLowY),
        OpticsMagTypeEnum.Middle => (CenterFrequenceMiddle, SoundPackageLengthMiddle, ZeroNumMiddleY),
        OpticsMagTypeEnum.High => (CenterFrequenceHigh, SoundPackageLengthHigh, ZeroNumHighY),
        _ => throw new ArgumentOutOfRangeException()
    };

    public (double, double, short) SetInitialChirpAodWaveParams(double centerFrequence, double soundPackageLength) => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => (CenterFrequenceLow = centerFrequence, SoundPackageLengthLow = soundPackageLength, ZeroNumLowY = ZeroNumLow),
        OpticsMagTypeEnum.Middle => (CenterFrequenceMiddle = centerFrequence, SoundPackageLengthMiddle = soundPackageLength, ZeroNumMiddleY = ZeroNumMiddle),
        OpticsMagTypeEnum.High => (CenterFrequenceHigh = centerFrequence, SoundPackageLengthHigh = soundPackageLength, ZeroNumHighY = ZeroNumHigh),
        _ => throw new ArgumentOutOfRangeException()
    };

    public short GetChirpAodDefaultWaveZeroNum() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => ZeroNumLow,
        OpticsMagTypeEnum.Middle => ZeroNumMiddle,
        OpticsMagTypeEnum.High => ZeroNumHigh,
        _ => throw new ArgumentOutOfRangeException()
    };

    #endregion 频率参数

    #region FindEcsY波形参数

    public short GetChirpAodChangedWaveZeroNum() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => ZeroNumLowY,
        OpticsMagTypeEnum.Middle => ZeroNumMiddleY,
        OpticsMagTypeEnum.High => ZeroNumHighY,
        _ => throw new ArgumentOutOfRangeException()
    };

    #endregion FindEcsY波形参数

    public double GetThresholdParams() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => ThresholdLow,
        OpticsMagTypeEnum.Middle => ThresholdMiddle,
        OpticsMagTypeEnum.High => ThresholdHigh,
        _ => throw new ArgumentOutOfRangeException()
    };

    public double GetReviewThresholdParams() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => ThresholdLowReview,
        OpticsMagTypeEnum.Middle => ThresholdMiddleReview,
        OpticsMagTypeEnum.High => ThresholdHighReview,
        _ => throw new ArgumentOutOfRangeException()
    };

    public short SetChirpAodRegNum(short count) => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => RegNum = (short)(ZeroNumLowY + count),
        OpticsMagTypeEnum.Middle => RegNum = (short)(ZeroNumMiddleY + count),
        OpticsMagTypeEnum.High => RegNum = (short)(ZeroNumHighY + count),
        _ => throw new ArgumentOutOfRangeException()
    };

    #endregion 方法
}