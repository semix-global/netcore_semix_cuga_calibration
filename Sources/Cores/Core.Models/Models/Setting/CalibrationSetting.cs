using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models.Models.Setting.CalibrationRelationConfig;
using Local.SQL.Cache.Providers.Bases;
using Net.Utilities.Mapper.Interfaces;
using System.Collections.ObjectModel;

namespace Core.Models.Models.Setting;

public sealed partial class CalibrationSetting : ObservableCacheBase, IAdaptIn<CalibrationSetting, CalibrationSetting>
{
    /// <summary>
    /// 通用参数
    /// </summary>
    [ObservableProperty]
    public partial SettingCommonParam SettingCommonParam { get; set; } = new();

    /// <summary>
    /// 模板匹配参数
    /// </summary>
    [ObservableProperty]
    public partial SettingTemplateMatchParam SettingTemplateMatchParam { get; set; } = new();

    /// <summary>
    /// 配置Cuga自检使用的配置参数
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<SettingRequiredCalibrationParam> SettingRequiredCalibrationParamList { get; set; } = [];

    /// <summary>
    /// 配置Cuga依赖禁用关系
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<SettingCalibrationRelationParam> SettingCalibrationRelationParams { get; set; } = [];

    #region Mapper

    public CalibrationSetting AdaptIn(CalibrationSetting obj)
    {
        SettingCommonParam = new SettingCommonParam().AdaptIn(obj.SettingCommonParam);
        SettingTemplateMatchParam = new SettingTemplateMatchParam().AdaptIn(obj.SettingTemplateMatchParam);
        SettingRequiredCalibrationParamList = [.. obj.SettingRequiredCalibrationParamList.Select(x => new SettingRequiredCalibrationParam().AdaptIn(x))];
        SettingCalibrationRelationParams = [.. obj.SettingCalibrationRelationParams.Select(x => new SettingCalibrationRelationParam().AdaptIn(x))];

        return obj;
    }

    #endregion Mapper
}