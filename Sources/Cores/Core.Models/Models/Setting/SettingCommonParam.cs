using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Enums.Loggings;

namespace Core.Models.Models.Setting;

/// <summary>
/// 通用参数
/// </summary>
public sealed partial class SettingCommonParam : ObservableObject, IAdaptIn<SettingCommonParam, SettingCommonParam>
{
    [ObservableProperty]
    private LogLevelEnum _minLogLevelEnum = LogLevelEnum.Info;

    [ObservableProperty]
    private MicroscopeLensInformation _lowMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _mainLaserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private double _pMTInterval = 320;

    [ObservableProperty]
    private double _measurePowerMeasurementMinValue = 0.1;

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

    #endregion 校准状态控制

    #region Mapper

    public SettingCommonParam AdaptIn(SettingCommonParam obj)
    {
        MinLogLevelEnum = obj.MinLogLevelEnum;
        LowMicroscopeLensInformation = obj.LowMicroscopeLensInformation;
        HighMicroscopeLensInformation = obj.HighMicroscopeLensInformation;
        MainLaserLightInformation = obj.MainLaserLightInformation;
        PMTInterval = obj.PMTInterval;
        MeasurePowerMeasurementMinValue = obj.MeasurePowerMeasurementMinValue;
        DependencyEnable = obj.DependencyEnable;
        PrerequisitesEnable = obj.PrerequisitesEnable;
        IsDebugEnvironment = obj.IsDebugEnvironment;

        return this;
    }

    #endregion Mapper
}