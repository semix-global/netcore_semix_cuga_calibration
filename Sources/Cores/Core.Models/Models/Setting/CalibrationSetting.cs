using CommunityToolkit.Mvvm.ComponentModel;
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
    /// 低倍暗场增益参数
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SettingDarkFieldGainParam> _settingDarkFieldGainParam = [];

    /// <summary>
    /// PMT使能参数
    /// </summary>
    [ObservableProperty]
    private SettingPmtConfigParam _settingPmtConfigParam = new();

    /// <summary>
    /// 配置Cuga自检使用的配置参数
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<SettingRequiredCalibrationParam> _settingRequiredCalibrationParamList = [];

    #region Mapper

    public CalibrationSetting AdaptIn(CalibrationSetting obj)
    {
        SettingCommonParam = new SettingCommonParam().AdaptIn(obj.SettingCommonParam);
        SettingTemplateMatchParam = new SettingTemplateMatchParam().AdaptIn(obj.SettingTemplateMatchParam);
        SettingDarkFieldGainParam = [.. obj.SettingDarkFieldGainParam.Select(x => new SettingDarkFieldGainParam().AdaptIn(x))];
        SettingPmtConfigParam = new SettingPmtConfigParam().AdaptIn(obj.SettingPmtConfigParam);
        SettingRequiredCalibrationParamList = [.. obj.SettingRequiredCalibrationParamList.Select(x => new SettingRequiredCalibrationParam().AdaptIn(x))];

        return obj;
    }

    #endregion Mapper
}