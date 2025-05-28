using CommunityToolkit.Mvvm.ComponentModel;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Setting;

/// <summary>
/// 暗场自动聚焦参数
/// </summary>
public sealed partial class SettingDarkFieldAutoFocusParam : ObservableCacheBase, IAdaptIn<SettingDarkFieldAutoFocusParam, SettingDarkFieldAutoFocusParam>, ICloneable<SettingDarkFieldAutoFocusParam>
{
    /// <summary>
    /// Chuck暗场自动聚焦标准Ecs值
    /// </summary>
    [ObservableProperty]
    private double _chuckEcsValue = 6000;

    /// <summary>
    /// Chuck暗场自动聚焦标准Motor值
    /// </summary>
    [ObservableProperty]
    private double _chuckMotorValue;

    /// <summary>
    /// 是否启用Chuck暗场暗场自动聚焦
    /// </summary>
    [ObservableProperty]
    private bool _isEnableChuck;

    /// <summary>
    /// Dsw暗场自动聚焦标准Ecs值
    /// </summary>
    [ObservableProperty]
    private double _dswEcsValue;

    /// <summary>
    /// Dsw暗场自动聚焦标准Motor值
    /// </summary>
    [ObservableProperty]
    private double _dswMotorValue;

    /// <summary>
    /// 是否启用Dsw暗场暗场自动聚焦
    /// </summary>
    [ObservableProperty]
    private bool _isEnableDsw;

    /// <summary>
    /// Undefined暗场自动聚焦标准Ecs值
    /// </summary>
    [ObservableProperty]
    private double _undefinedEcsValue;

    /// <summary>
    /// Undefined暗场自动聚焦标准Motor值
    /// </summary>
    [ObservableProperty]
    private double _undefinedMotorValue;

    /// <summary>
    /// 是否启用Undefined暗场暗场自动聚焦
    /// </summary>
    [ObservableProperty]
    private bool _isEnableUndefined;

    /// <summary>
    /// Haze暗场自动聚焦标准Ecs值
    /// </summary>
    [ObservableProperty]
    private double _hazeEcsValue;

    /// <summary>
    /// Haze暗场自动聚焦标准Motor值
    /// </summary>
    [ObservableProperty]
    private double _hazeMotorValue;

    /// <summary>
    /// 是否启用Haze暗场暗场自动聚焦
    /// </summary>
    [ObservableProperty]
    private bool _isEnableHaze;

    /// <summary>
    /// ShinyWafer暗场自动聚焦标准Ecs值
    /// </summary>
    [ObservableProperty]
    private double _shinyWaferEcsValue;

    /// <summary>
    /// ShinyWafer暗场自动聚焦标准Motor值
    /// </summary>
    [ObservableProperty]
    private double _shinyWaferMotorValue;

    /// <summary>
    /// 是否启用ShinyWafer暗场暗场自动聚焦
    /// </summary>
    [ObservableProperty]
    private bool _isEnableShinyWafer;

    #region Mapper

    public SettingDarkFieldAutoFocusParam AdaptIn(SettingDarkFieldAutoFocusParam obj)
    {
        ChuckEcsValue = obj.ChuckEcsValue;
        ChuckMotorValue = obj.ChuckMotorValue;
        IsEnableChuck = obj.IsEnableChuck;

        DswEcsValue = obj.DswEcsValue;
        DswMotorValue = obj.DswMotorValue;
        IsEnableDsw = obj.IsEnableDsw;

        UndefinedEcsValue = obj.UndefinedEcsValue;
        UndefinedMotorValue = obj.UndefinedMotorValue;
        IsEnableUndefined = obj.IsEnableUndefined;

        HazeEcsValue = obj.HazeEcsValue;
        HazeMotorValue = obj.HazeMotorValue;
        IsEnableHaze = obj.IsEnableHaze;

        ShinyWaferEcsValue = obj.ShinyWaferEcsValue;
        ShinyWaferMotorValue = obj.ShinyWaferMotorValue;
        IsEnableShinyWafer = obj.IsEnableShinyWafer;

        return obj;
    }

    public SettingDarkFieldAutoFocusParam Clone() => new()
    {
        ChuckEcsValue = ChuckEcsValue,
        ChuckMotorValue = ChuckMotorValue,
        IsEnableChuck = IsEnableChuck,
        DswEcsValue = DswEcsValue,
        DswMotorValue = DswMotorValue,
        IsEnableDsw = IsEnableDsw,
        UndefinedEcsValue = UndefinedEcsValue,
        UndefinedMotorValue = UndefinedMotorValue,
        IsEnableUndefined = IsEnableUndefined,
        HazeEcsValue = HazeEcsValue,
        HazeMotorValue = HazeMotorValue,
        IsEnableHaze = IsEnableHaze,
        ShinyWaferEcsValue = ShinyWaferEcsValue,
        ShinyWaferMotorValue = ShinyWaferMotorValue,
        IsEnableShinyWafer = IsEnableShinyWafer
    };

    #endregion Mapper
}