using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Models.Geometries;

namespace Core.Models.Models.Laser.XYAstigmatism;

// ReSharper disable once InconsistentNaming
public sealed partial class LaserXYAstigmatismCalibrationCache : CalibrationCacheBase
{
    [ObservableProperty]
    private MicroscopeLensInformation _microscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private CIBConfiguration _cIBConfiguration = new();

    [ObservableProperty]
    private OpticsMagTypeEnum _opticsMagTypeEnum;

    [ObservableProperty]
    private CalChipSiteModelEnum _calChipSiteModelEnum;

    [ObservableProperty]
    private Point _findPosition;

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

    #region 频率参数

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

    #endregion 频率参数

    #region 波形参数

    /// <summary>
    /// ChirpAod默认波形生成参数
    /// </summary>
    [ObservableProperty]
    private GenerateChirpAODWaveformParam _lowChirpAodDefaultDto = new();

    /// <summary>
    /// ChirpAod默认波形生成参数
    /// </summary>
    [ObservableProperty]
    private GenerateChirpAODWaveformParam _middleChirpAodDefaultDto = new();

    /// <summary>
    /// ChirpAod默认波形生成参数
    /// </summary>
    [ObservableProperty]
    private GenerateChirpAODWaveformParam _highChirpAodDefaultDto = new();

    /// <summary>
    /// 起始频率变化率 lowMag
    /// </summary>
    [ObservableProperty]
    private double _startSpectralDensityLow;

    /// <summary>
    /// 起始频率变化率 middleMag
    /// </summary>
    [ObservableProperty]
    private double _startSpectralDensityMiddle;

    /// <summary>
    /// 起始频率变化率 highMag
    /// </summary>
    [ObservableProperty]
    private double _startSpectralDensityHigh;

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
    /// XY焦距差值允许范围 highMag
    /// </summary>
    [ObservableProperty]
    private double _thresholdHigh;

    #endregion 阈值

    #region 方法

    #region ECS-X

    public (double zLimitMin, double zLimitMax, double zLimitInterval, double findInitialECS) GetEcsXParams() =>
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

    public (double zLimitMin, double zLimitMax, double zLimitInterval, double findInitialECS) GetEcsYParams() =>
        OpticsMagTypeEnum switch
        {
            OpticsMagTypeEnum.Low => (EcsYPositionUpperLow, EcsYPositionLowerLow, EcsYIntervalLow, EcsYLowInitial),
            OpticsMagTypeEnum.Middle => (EcsYPositionUpperMiddle, EcsYPositionLowerMiddle, EcsYIntervalMiddle, EcsYMidInitial),
            OpticsMagTypeEnum.High => (EcsYPositionUpperHigh, EcsYPositionLowerHigh, EcsYIntervalHigh, EcsYHighInitial),
            _ => throw new ArgumentOutOfRangeException()
        };

    public void SetEcsYParams(double value)
    {
        _ = OpticsMagTypeEnum switch
        {
            OpticsMagTypeEnum.Low => EcsYLowInitial = value,
            OpticsMagTypeEnum.Middle => EcsYMidInitial = value,
            OpticsMagTypeEnum.High => EcsYHighInitial = value,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public void SetEcsYParams()
    {
        _ = OpticsMagTypeEnum switch
        {
            OpticsMagTypeEnum.Low => (EcsYPositionUpperLow = EcsPositionUpperLow, EcsYPositionLowerLow = EcsPositionLowerLow, EcsIntervalLow = EcsYIntervalLow, EcsYLowInitial = EcsXLowInitial),
            OpticsMagTypeEnum.Middle => (EcsYPositionUpperMiddle = EcsPositionUpperMiddle, EcsYPositionLowerMiddle = EcsPositionLowerMiddle, EcsIntervalMiddle = EcsYIntervalMiddle, EcsYMidInitial = EcsXMidInitial),
            OpticsMagTypeEnum.High => (EcsYPositionUpperHigh = EcsPositionUpperHigh, EcsYPositionLowerHigh = EcsPositionLowerHigh, EcsIntervalHigh = EcsYIntervalHigh, EcsYHighInitial = EcsXHighInitial),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    #endregion ECS-Y

    #region 频率参数

    public GenerateChirpAODWaveformParam GetDefaultChirpAodProfile() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => LowChirpAodDefaultDto,
        OpticsMagTypeEnum.Middle => MiddleChirpAodDefaultDto,
        OpticsMagTypeEnum.High => HighChirpAodDefaultDto,
        _ => throw new ArgumentOutOfRangeException()
    };

    public (int increaseCount, double increaseInterval) GetFrequencyParams() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => (IncreaseCountLow, IncreaseIntervalLow),
        OpticsMagTypeEnum.Middle => (IncreaseCountMiddle, IncreaseIntervalMiddle),
        OpticsMagTypeEnum.High => (IncreaseCountHigh, IncreaseIntervalHigh),
        _ => throw new ArgumentOutOfRangeException()
    };

    public double GetStartSpectralDensity() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => StartSpectralDensityLow,
        OpticsMagTypeEnum.Middle => StartSpectralDensityMiddle,
        OpticsMagTypeEnum.High => StartSpectralDensityHigh,
        _ => throw new ArgumentOutOfRangeException()
    };

    public void SetStartSpectralDensity(double startSpectralDensity)
    {
        _ = OpticsMagTypeEnum switch
        {
            OpticsMagTypeEnum.Low => StartSpectralDensityLow = startSpectralDensity,
            OpticsMagTypeEnum.Middle => StartSpectralDensityMiddle = startSpectralDensity,
            OpticsMagTypeEnum.High => StartSpectralDensityHigh = startSpectralDensity,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    #endregion 频率参数

    public double GetThresholdParams() => OpticsMagTypeEnum switch
    {
        OpticsMagTypeEnum.Low => ThresholdLow,
        OpticsMagTypeEnum.Middle => ThresholdMiddle,
        OpticsMagTypeEnum.High => ThresholdHigh,
        _ => throw new ArgumentOutOfRangeException()
    };

    #endregion 方法
}