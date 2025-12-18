using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Enums.Loggings;
using System.ComponentModel.DataAnnotations;

namespace Core.Models.Models.Setting;

/// <summary>
/// 通用参数
/// </summary>
public sealed partial class SettingCommonParam : ObservableCacheBase, IAdaptIn<SettingCommonParam, SettingCommonParam>
{
    private double _pmtInterval = 320; // Pmt相机采集间隔320um

    /// <summary>
    /// 日志级别
    /// </summary>
    [ObservableProperty]
    private LogLevelEnum _minLogLevelEnum = LogLevelEnum.Info;

    [ObservableProperty]
    private MicroscopeLensInformation _lowMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = MicroscopeLensInformation.Default;

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

    #endregion 校准状态控制

    #region Mapper

    public SettingCommonParam AdaptIn(SettingCommonParam obj)
    {
        MinLogLevelEnum = obj.MinLogLevelEnum;
        LowMicroscopeLensInformation = obj.LowMicroscopeLensInformation;
        HighMicroscopeLensInformation = obj.HighMicroscopeLensInformation;
        DependencyEnable = obj.DependencyEnable;
        PrerequisitesEnable = obj.PrerequisitesEnable;
        IsDebugEnvironment = obj.IsDebugEnvironment;
        PmtInterval = obj.PmtInterval;
        MainLaserLightInformation = obj.MainLaserLightInformation;
        return this;
    }

    #endregion Mapper
}