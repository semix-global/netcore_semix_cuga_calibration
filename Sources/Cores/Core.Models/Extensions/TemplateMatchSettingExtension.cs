using Core.Models.Models.Setting;
using Net.Utilities.Calibration;

namespace Core.Models.Extensions;

public static class TemplateMatchSettingExtension
{
    /// <summary>
    /// 匹配得分阈值：引擎真值取自 calibrationSetting 全局设置；
    /// Halcon 按模板类型（Sharpe/Ncc）分发，AlgoCSharp 沿用 Ncc 归一化相关性阈值。
    /// </summary>
    public static double ToTemplateMatchScoreThreshold(this TemplateMatchSetting setting, CalibrationSetting calibrationSetting)
    {
        var engine = calibrationSetting.SettingTemplateMatchParam.AlgorithmEngineTypeEnum;

        return engine == AlgorithmEngineTypeEnum.HAlgorithm
            ? setting.Halcon.TemplateType.ToTemplateMatchScoreThreshold(calibrationSetting)
            : calibrationSetting.SettingTemplateMatchParam.NccTypeTemplateMatchScoreThreshold;
    }
}
