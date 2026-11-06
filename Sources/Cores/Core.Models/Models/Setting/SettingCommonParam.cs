using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
using Net.Utilities.Calibration;
using Net.Utilities.Mapper.Interfaces;
using Net.Utilities.Models.Enums.Loggings;

namespace Core.Models.Models.Setting;

/// <summary>
/// 通用参数
/// </summary>
public sealed partial class SettingCommonParam : ObservableObject, IAdaptIn<SettingCommonParam, SettingCommonParam>
{
    [ObservableProperty]
    public partial LogLevelEnum MinLogLevelEnum { get; set; } = LogLevelEnum.Info;

    [ObservableProperty]
    public partial MicroscopeLensInformation LowMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial MicroscopeLensInformation HighMicroscopeLensInformation { get; set; } = MicroscopeLensInformation.Default;

    [ObservableProperty]
    public partial LaserLightInformation MainLaserLightInformation { get; set; } = LaserLightInformation.Default;

    [ObservableProperty]
    public partial CIBInformation MainCIBInformation { get; set; } = CIBInformation.Default;

    [ObservableProperty]
    public partial double MeasurePowerMeasurementMinValue { get; set; } = 0.1d;

    /// <summary>
    /// 全局默认算法引擎类型（显示域算子未显式指定引擎时的兜底选择，新校准项的初始值来源）
    /// </summary>
    [ObservableProperty]
    public partial AlgorithmEngineTypeEnum AlgorithmEngineTypeEnum { get; set; } = AlgorithmEngineTypeEnum.HAlgorithm;

    /// <summary>
    /// 全局默认引擎联动：属性变更（默认值初始化、Setting 恢复 AdaptIn、Setting 页 UI 修改）时同步写入 Utils 层全局默认，
    /// Utils 层不反向读取本配置（防止层次倒挂）
    /// </summary>
    partial void OnAlgorithmEngineTypeEnumChanged(AlgorithmEngineTypeEnum value) => AlgorithmEngineConstantHelper.Current = value;

    #region Mapper

    public SettingCommonParam AdaptIn(SettingCommonParam obj)
    {
        MinLogLevelEnum = obj.MinLogLevelEnum;
        LowMicroscopeLensInformation = obj.LowMicroscopeLensInformation.Clone();
        HighMicroscopeLensInformation = obj.HighMicroscopeLensInformation.Clone();
        MainLaserLightInformation = obj.MainLaserLightInformation.Clone();
        MainCIBInformation = obj.MainCIBInformation.Clone();
        MeasurePowerMeasurementMinValue = obj.MeasurePowerMeasurementMinValue;
        AlgorithmEngineTypeEnum = obj.AlgorithmEngineTypeEnum;

        return this;
    }

    #endregion Mapper
}