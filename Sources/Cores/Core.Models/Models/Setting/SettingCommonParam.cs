using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Enums.Loggings;
using System.ComponentModel.DataAnnotations;
using Core.Models.Models.Common.Pattern;

namespace Core.Models.Models.Setting;

/// <summary>
/// 通用参数
/// </summary>
public sealed partial class SettingCommonParam : ObservableCacheBase, IAdaptIn<SettingCommonParam, SettingCommonParam>
{
    private double _pmtInterval = 320; // Pmt相机采集间隔320um

    [ObservableProperty]
    private double _scanLineXPixelSizeLowMagLowSpeed = 0.3334390881;

    [ObservableProperty]
    private double _scanLineXPixelSizeLowMagMiddleSpeed = 0.3334390881;

    [ObservableProperty]
    private double _scanLineXPixelSizeLowMagHighSpeed = 0.3334390881;

    [ObservableProperty]
    private double _scanLineXPixelSizeMiddleMagLowSpeed = 0.3334390881;

    [ObservableProperty]
    private double _scanLineXPixelSizeMiddleMagMiddleSpeed = 0.3334390881;

    [ObservableProperty]
    private double _scanLineXPixelSizeMiddleMagHighSpeed = 0.3334390881;

    [ObservableProperty]
    private double _scanLineXPixelSizeHighMagLowSpeed = 0.3334390881;

    [ObservableProperty]
    private double _scanLineXPixelSizeHighMagMiddleSpeed = 0.3334390881;

    [ObservableProperty]
    private double _scanLineXPixelSizeHighMagHighSpeed = 0.3334390881;

    /// <summary>
    /// 日志级别
    /// </summary>
    [ObservableProperty]
    private LogLevelEnum _minLogLevelEnum = LogLevelEnum.Info;

    #region 校准状态控制

    /// <summary>
    /// 依赖关系使能
    /// </summary>
    [ObservableProperty]
    private bool _dependencyEnable;

    /// <summary>
    /// 前置条件使能
    /// </summary>
    [ObservableProperty]
    private bool _prerequisitesEnable;

    /// <summary>
    /// 是否是Debug环境
    /// </summary>
    [ObservableProperty]
    private bool _isDebugEnvironment;

    /// <summary>
    /// Pmt相机采集间隔(um)
    /// </summary>
    [Range(0, 500)]
    public double PmtInterval
    {
        get => _pmtInterval;
        set => SetProperty(ref _pmtInterval, value, true);
    }

    /// <summary>
    /// 校准暗场采集波形功率系数
    /// </summary>
    [ObservableProperty]
    private LaserLightInformation _mainLaserLightInformation = LaserLightInformation.Default;

    public double GetScanLineXPixelSize(OpticsMagTypeEnum optics, StageSpeedEnum speed) => (optics, speed) switch
    {
        (OpticsMagTypeEnum.Low, StageSpeedEnum.Low) => ScanLineXPixelSizeLowMagLowSpeed,
        (OpticsMagTypeEnum.Low, StageSpeedEnum.Middle) => ScanLineXPixelSizeLowMagMiddleSpeed,
        (OpticsMagTypeEnum.Low, StageSpeedEnum.High) => ScanLineXPixelSizeLowMagHighSpeed,
        (OpticsMagTypeEnum.Middle, StageSpeedEnum.Low) => ScanLineXPixelSizeMiddleMagLowSpeed,
        (OpticsMagTypeEnum.Middle, StageSpeedEnum.Middle) => ScanLineXPixelSizeMiddleMagMiddleSpeed,
        (OpticsMagTypeEnum.Middle, StageSpeedEnum.High) => ScanLineXPixelSizeMiddleMagHighSpeed,
        (OpticsMagTypeEnum.High, StageSpeedEnum.Low) => ScanLineXPixelSizeHighMagLowSpeed,
        (OpticsMagTypeEnum.High, StageSpeedEnum.Middle) => ScanLineXPixelSizeHighMagMiddleSpeed,
        (OpticsMagTypeEnum.High, StageSpeedEnum.High) => ScanLineXPixelSizeHighMagHighSpeed,
        _ => throw new ArgumentOutOfRangeException(nameof(GetScanLineXPixelSize), "Illegal value")
    };

    #endregion 校准状态控制

    #region Mapper

    public SettingCommonParam AdaptIn(SettingCommonParam obj)
    {
        MinLogLevelEnum = obj.MinLogLevelEnum;
        DependencyEnable = obj.DependencyEnable;
        PrerequisitesEnable = obj.PrerequisitesEnable;
        IsDebugEnvironment = obj.IsDebugEnvironment;
        PmtInterval = obj.PmtInterval;
        MainLaserLightInformation = obj.MainLaserLightInformation;
        ScanLineXPixelSizeLowMagLowSpeed = obj.ScanLineXPixelSizeLowMagLowSpeed;
        ScanLineXPixelSizeLowMagMiddleSpeed = obj.ScanLineXPixelSizeLowMagMiddleSpeed;
        ScanLineXPixelSizeLowMagHighSpeed = obj.ScanLineXPixelSizeLowMagHighSpeed;
        ScanLineXPixelSizeMiddleMagLowSpeed = obj.ScanLineXPixelSizeMiddleMagLowSpeed;
        ScanLineXPixelSizeMiddleMagMiddleSpeed = obj.ScanLineXPixelSizeMiddleMagMiddleSpeed;
        ScanLineXPixelSizeMiddleMagHighSpeed = obj.ScanLineXPixelSizeMiddleMagHighSpeed;
        ScanLineXPixelSizeHighMagLowSpeed = obj.ScanLineXPixelSizeHighMagLowSpeed;
        ScanLineXPixelSizeHighMagMiddleSpeed = obj.ScanLineXPixelSizeHighMagMiddleSpeed;
        ScanLineXPixelSizeHighMagHighSpeed = obj.ScanLineXPixelSizeHighMagHighSpeed;
        return this;
    }

    #endregion Mapper
}