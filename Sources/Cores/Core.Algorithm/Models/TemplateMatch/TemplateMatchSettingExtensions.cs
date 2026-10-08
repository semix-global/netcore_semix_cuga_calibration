using algocv_sharp;
using Core.Models.Enums.Algorithm;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Setting;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Models.TemplateMatch;

public static class TemplateMatchSettingExtensions
{
    /// <param name="setting">模板匹配设置</param>
    extension(TemplateMatchSetting setting)
    {
        /// <summary>
        /// 算法库的引擎参数
        /// </summary>
        public TemplateMatchEngineParams ToEngineParams(AlgorithmEngineTypeEnum engine)
            => engine == AlgorithmEngineTypeEnum.HAlgorithm
                ? new HalconTemplateMatchParams { TemplateType = setting.Halcon.TemplateType }
                : new AlgoCvTemplateMatchParams { Method = setting.AlgoCSharp.Method.ToAlgoCvMethod(), PyramidLevels = setting.AlgoCSharp.PyramidLevels };

        /// <summary>
        /// 模板参数：按全局算法库选择对应 EngineParams，并按模板尺寸计算居中 ROI。
        /// </summary>
        /// <param name="engine">全局算法库</param>
        /// <param name="imageSize">原图尺寸</param>
        /// <param name="templateFilePath">模板文件路径</param>
        public GenerateTemplateParam ToGenerateParam(AlgorithmEngineTypeEnum engine, Size imageSize, string templateFilePath)
        {
            var templateSize = new Size((int)setting.TemplateSize, (int)setting.TemplateSize);

            return new GenerateTemplateParam
            {
                TemplateFilePath = templateFilePath,
                Roi = new Rect(
                    imageSize.Width / 2d - templateSize.Width / 2d,
                    imageSize.Height / 2d - templateSize.Height / 2d,
                    templateSize.Width,
                    templateSize.Height),
                EngineParams = setting.ToEngineParams(engine)
            };
        }

        /// <summary>
        /// 读模板参数
        /// </summary>
        public ReadTemplateParam ToReadParam(AlgorithmEngineTypeEnum engine, string templateFilePath)
            => new() { TemplateFilePath = templateFilePath, EngineParams = setting.ToEngineParams(engine) };

        /// <summary>
        /// 日志打印对象
        /// </summary>
        public object ToHtmlAnonymous(AlgorithmEngineTypeEnum engine) => new
        {
            setting.TemplateSize,
            EngineParams = setting.ToEngineParams(engine).ToHtmlAnonymous()
        };

        /// <summary>
        /// 匹配得分阈值：Halcon 按模板类型（Sharpe/Ncc）分发，AlgoCSharp 沿用 Ncc 归一化相关性阈值。
        /// </summary>
        public double ToTemplateMatchScoreThreshold(AlgorithmEngineTypeEnum engine, CalibrationSetting calibrationSetting)
            => engine == AlgorithmEngineTypeEnum.HAlgorithm
                ? setting.Halcon.TemplateType.ToTemplateMatchScoreThreshold(calibrationSetting)
                : calibrationSetting.SettingTemplateMatchParam.NccTypeTemplateMatchScoreThreshold;

        /// <summary>
        /// 模板模型文件路径：Halcon 为 .shm/.ncc，AlgoCSharp 复用 .jpg 预览图作为模型载体。
        /// </summary>
        public string ToTemplateMatchModelFilePath(AlgorithmEngineTypeEnum engine, string templateFilePath)
            => engine == AlgorithmEngineTypeEnum.HAlgorithm
                ? setting.Halcon.TemplateType.ToFullFilePath(templateFilePath)
                : CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath);

        /// <summary>
        /// 暗场模板匹配（Halcon-only）专用模板类型
        /// </summary>
        public AlgorithmTemplateTypeEnum ToHalconTemplateType() => setting.Halcon.TemplateType;
    }

    private static TemplateMatchMethod ToAlgoCvMethod(this TemplateMatchMethodEnum method) => method switch
    {
        TemplateMatchMethodEnum.TmSqdiff => TemplateMatchMethod.TmSqdiff,
        TemplateMatchMethodEnum.TmSqdiffNormed => TemplateMatchMethod.TmSqdiffNormed,
        TemplateMatchMethodEnum.TmCcorr => TemplateMatchMethod.TmCcorr,
        TemplateMatchMethodEnum.TmCcorrNormed => TemplateMatchMethod.TmCcorrNormed,
        TemplateMatchMethodEnum.TmCcoeff => TemplateMatchMethod.TmCcoeff,
        _ => TemplateMatchMethod.TmCcoeffNormed
    };
}