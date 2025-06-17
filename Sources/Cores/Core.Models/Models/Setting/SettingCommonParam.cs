using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Enums;
using Net.Utilities.Mapper.Interfaces;
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
    /// 依赖关系使能
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
    private double _mainCoefficient;

    #endregion 校准状态控制

    #region Mapper

    public SettingCommonParam AdaptIn(SettingCommonParam obj)
    {
        MinLogLevelEnum = obj.MinLogLevelEnum;
        DependencyEnable = obj.DependencyEnable;
        PrerequisitesEnable = obj.PrerequisitesEnable;
        IsDebugEnvironment = obj.IsDebugEnvironment;
        PmtInterval = obj.PmtInterval;
        MainCoefficient = obj.MainCoefficient;
        return this;
    }

    #endregion Mapper
}