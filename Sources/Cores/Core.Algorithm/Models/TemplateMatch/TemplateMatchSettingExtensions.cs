using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Setting;
using Net.Utilities.Calibration;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Models.TemplateMatch;

public static class TemplateMatchSettingExtensions
{
    extension(TemplateMatchSetting setting)
    {
        /// <summary>
        /// 算法库的引擎参数（engine 由调用点显式传入，错库参数由引擎入口 EngineParams.TryGet 硬失败兜底）
        /// </summary>
        public EngineParams ToEngineParams(AlgorithmEngineTypeEnum engine) =>
            engine == AlgorithmEngineTypeEnum.HAlgorithm
                ? new HalconTemplateMatchParams { TemplateType = setting.Halcon.TemplateType }
                : new AlgoCvTemplateMatchParams { Method = setting.AlgoCSharp.Method, PyramidLevels = setting.AlgoCSharp.PyramidLevels };

        /// <summary>
        /// 模板参数：按传入算法库选择对应 EngineParams，并按模板尺寸计算居中 ROI。
        /// </summary>
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
        /// 模板参数：自定义 ROI（暗场模板窗口用户框选区域）
        /// </summary>
        public GenerateTemplateParam ToGenerateParam(AlgorithmEngineTypeEnum engine, Rect roi, string templateFilePath)
            => new() { TemplateFilePath = templateFilePath, Roi = roi, EngineParams = setting.ToEngineParams(engine) };

        /// <summary>
        /// 模板参数：自定义 ROI + 自定义模板中心（框选区域后手动标记的模板中心；null = 默认 ROI 中心）
        /// </summary>
        public GenerateTemplateParam ToGenerateParam(AlgorithmEngineTypeEnum engine, Rect roi, Point? templateCenter, string templateFilePath)
            => new() { TemplateFilePath = templateFilePath, Roi = roi, TemplateCenter = templateCenter, EngineParams = setting.ToEngineParams(engine) };

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
        /// 模板模型文件路径：Halcon 为 .shm/.ncc，AlgoCSharp 复用 .jpg 预览图作为模型载体。
        /// </summary>
        public string ToTemplateMatchModelFilePath(AlgorithmEngineTypeEnum engine, string templateFilePath) =>
            engine == AlgorithmEngineTypeEnum.HAlgorithm
                ? setting.Halcon.TemplateType.ToFullFilePath(templateFilePath)
                : CalibrationConstantsHelper.TemplatePathToTemplateImagePath(templateFilePath);
    }
}
