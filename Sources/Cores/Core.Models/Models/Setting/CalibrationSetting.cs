using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Pattern;
using Local.NoSQL.DB.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Setting;

public sealed partial class CalibrationSetting : ObservableCacheBase, IAdaptIn<CalibrationSetting, CalibrationSetting>
{
    /// <summary>
    /// 通用参数
    /// </summary>
    [ObservableProperty]
    private SettingCommonParam _settingCommonParam = new();

    /// <summary>
    /// 模板匹配参数
    /// </summary>
    [ObservableProperty]
    private SettingTemplateMatchParam _settingTemplateMatchParam = new();

    /// <summary>
    /// 低倍暗场自动聚焦参数
    /// </summary>
    [ObservableProperty]
    private SettingDarkFieldAutoFocusParam _lowMagSettingDarkFieldAutoFocusParam = new();

    /// <summary>
    /// 中倍暗场自动聚焦参数
    /// </summary>
    [ObservableProperty]
    private SettingDarkFieldAutoFocusParam _middleMagSettingDarkFieldAutoFocusParam = new();

    /// <summary>
    /// 高倍暗场自动聚焦参数
    /// </summary>
    [ObservableProperty]
    private SettingDarkFieldAutoFocusParam _highMagSettingDarkFieldAutoFocusParam = new();

    /// <summary>
    /// 低倍暗场增益参数
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SettingDarkFieldGainParam> _lowMagSettingDarkFieldGainParam = [];

    /// <summary>
    /// 中倍暗场增益参数
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SettingDarkFieldGainParam> _middleMagSettingDarkFieldGainParam = [];

    /// <summary>
    /// 高倍暗场增益参数
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SettingDarkFieldGainParam> _highMagSettingDarkFieldGainParam = [];

    /// <summary>
    /// PMT使能参数
    /// </summary>
    [ObservableProperty]
    private SettingPmtConfigParam _settingPmtConfigParam = new();

    /// <summary>
    /// 配置Cuga自检使用的配置参数
    /// </summary>
    [ObservableProperty]
    private SettingRequiredCalibrationParam _settingRequiredCalibrationParam = new();

    /// <summary>
    /// 高倍暗场增益参数
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<MicroscopeMagnificationInfo> _microscopeMagnificationInfoItems = [];

    #region Mapper

    public CalibrationSetting AdaptIn(CalibrationSetting obj)
    {
        SettingCommonParam = new SettingCommonParam().AdaptIn(obj.SettingCommonParam);
        SettingTemplateMatchParam = new SettingTemplateMatchParam().AdaptIn(obj.SettingTemplateMatchParam);
        LowMagSettingDarkFieldAutoFocusParam = new SettingDarkFieldAutoFocusParam().AdaptIn(obj.LowMagSettingDarkFieldAutoFocusParam);
        MiddleMagSettingDarkFieldAutoFocusParam = new SettingDarkFieldAutoFocusParam().AdaptIn(obj.MiddleMagSettingDarkFieldAutoFocusParam);
        HighMagSettingDarkFieldAutoFocusParam = new SettingDarkFieldAutoFocusParam().AdaptIn(obj.HighMagSettingDarkFieldAutoFocusParam);
        LowMagSettingDarkFieldGainParam = [.. obj.LowMagSettingDarkFieldGainParam.Select(x => new SettingDarkFieldGainParam().AdaptIn(x))];
        MiddleMagSettingDarkFieldGainParam = [.. obj.MiddleMagSettingDarkFieldGainParam.Select(x => new SettingDarkFieldGainParam().AdaptIn(x))];
        HighMagSettingDarkFieldGainParam = [.. obj.HighMagSettingDarkFieldGainParam.Select(x => new SettingDarkFieldGainParam().AdaptIn(x))];
        SettingPmtConfigParam = new SettingPmtConfigParam().AdaptIn(obj.SettingPmtConfigParam);
        SettingRequiredCalibrationParam = new SettingRequiredCalibrationParam().AdaptIn(obj.SettingRequiredCalibrationParam);
        return obj;
    }

    #endregion Mapper
}