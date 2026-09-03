using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Common.Pattern;
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

    #region Mapper

    public SettingCommonParam AdaptIn(SettingCommonParam obj)
    {
        MinLogLevelEnum = obj.MinLogLevelEnum;
        LowMicroscopeLensInformation = obj.LowMicroscopeLensInformation.Clone();
        HighMicroscopeLensInformation = obj.HighMicroscopeLensInformation.Clone();
        MainLaserLightInformation = obj.MainLaserLightInformation.Clone();
        MainCIBInformation = obj.MainCIBInformation.Clone();
        MeasurePowerMeasurementMinValue = obj.MeasurePowerMeasurementMinValue;

        return this;
    }

    #endregion Mapper
}