using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Mapper.Interfaces;

namespace Core.Models.Models.Setting;

/// <summary>
/// 模板匹配参数
/// </summary>
public sealed partial class SettingTemplateMatchParam : ObservableObject, IAdaptIn<SettingTemplateMatchParam, SettingTemplateMatchParam>
{
    /// <summary>
    /// Sharpe匹配得分阈值
    /// </summary>
    [ObservableProperty]
    public partial double SharpeTypeTemplateMatchScoreThreshold { get; set; } = 0.5;

    /// <summary>
    /// Ncc匹配得分阈值
    /// </summary>
    [ObservableProperty]
    public partial double NccTypeTemplateMatchScoreThreshold { get; set; } = 0.8;

    #region Mapper

    public SettingTemplateMatchParam AdaptIn(SettingTemplateMatchParam obj)
    {
        SharpeTypeTemplateMatchScoreThreshold = obj.SharpeTypeTemplateMatchScoreThreshold;
        NccTypeTemplateMatchScoreThreshold = obj.NccTypeTemplateMatchScoreThreshold;

        return obj;
    }

    #endregion Mapper
}