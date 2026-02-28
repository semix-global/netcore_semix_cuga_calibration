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
    private LogLevelEnum _minLogLevelEnum = LogLevelEnum.Info;

    [ObservableProperty]
    private MicroscopeLensInformation _lowMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private MicroscopeLensInformation _highMicroscopeLensInformation = MicroscopeLensInformation.Default;

    [ObservableProperty]
    private LaserLightInformation _mainLaserLightInformation = LaserLightInformation.Default;

    [ObservableProperty]
    private double _pMTInterval = 320d;

    [ObservableProperty]
    private CIBInformation _mainCIBInformation = CIBInformation.Default;

    [ObservableProperty]
    private double _measurePowerMeasurementMinValue = 0.1d;

    #region Mapper

    public SettingCommonParam AdaptIn(SettingCommonParam obj)
    {
        MinLogLevelEnum = obj.MinLogLevelEnum;
        LowMicroscopeLensInformation = obj.LowMicroscopeLensInformation.Clone();
        HighMicroscopeLensInformation = obj.HighMicroscopeLensInformation.Clone();
        MainLaserLightInformation = obj.MainLaserLightInformation.Clone();
        PMTInterval = obj.PMTInterval;
        MainCIBInformation = obj.MainCIBInformation.Clone();
        MeasurePowerMeasurementMinValue = obj.MeasurePowerMeasurementMinValue;

        return this;
    }

    #endregion Mapper
}