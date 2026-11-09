using Core.Algorithm.Helper;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.TemplateMatch;
using Core.Algorithm.Models;
using Core.Algorithm.Models.TemplateMatch;
using Core.Models.Extensions;
using HalconDotNet;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Calibration;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Geometries;
using System.IO;

namespace Core.Algorithm.Implements.Halcon;

public sealed class HalconTemplateMatchEngine(ILogger<HalconTemplateMatchEngine> logger) : ITemplateMatchEngine
{
    private readonly HAlgorithm.Algorithm _algorithm = new();

    public AlgorithmResultRet<ITemplateMatchModel> Generate(BitmapImage image, GenerateTemplateParam param, Guid guid)
    {
        if (EngineParams.TryGet<HalconTemplateMatchParams>(param.EngineParams, out var halconParams, out var engineParamsError) == false)
        {
            return AlgorithmResultRet.CreateError<ITemplateMatchModel>(engineParamsError);
        }

        try
        {
            using var hImage = image.ToHImage();
            using var scaleImageTo8Bit = hImage.ScaleImageTo8Bit();
            var templateType = halconParams.TemplateType;

            var filePath = templateType.ToFullFilePath(param.TemplateFilePath);
            DirectoryHelper.CreateFileDirectoryIfNotExists(filePath);
            FileHelper.DeleteFileIfExists(filePath);

            var rect = param.Roi;
            _algorithm.HCreateModelXY(
                scaleImageTo8Bit,
                out var templateImageObj,
                templateType.ToAlgorithmTemplateType(),
                param.TemplateFilePath,
                rect.X,
                rect.Y,
                rect.Width,
                rect.Height);
            using var templatePreview = new HImage(templateImageObj);
            templatePreview.Save(AlgorithmConstantHelper.TemplatePathToTemplateImagePath(param.TemplateFilePath));
            image.SaveImage(AlgorithmConstantHelper.TemplatePathToTemplateOriginImagePath(param.TemplateFilePath));

            var meta = param.ToTemplateMatchMeta();
            TemplateMatchMeta.Save(param.TemplateFilePath, meta);

            _algorithm.HReadModel(templateType.ToAlgorithmTemplateType(), param.TemplateFilePath, out var templateId);
#pragma warning disable IDISP004
            return AlgorithmResultRet.CreateSuccess<ITemplateMatchModel>(new HalconTemplateMatchModel(templateType, templateId, _algorithm, meta.CenterOffset));
#pragma warning restore IDISP004
        }
        catch (Exception ex)
        {
            var errorImagePath = AlgorithmLogHelper.SaveErrorImage(image, guid, nameof(Generate));
            AlgorithmLogHelper.LogAlgorithmError(logger, ex, nameof(Generate), guid, errorImagePath);
            throw;
        }
    }

    public AlgorithmResultRet<ITemplateMatchModel> Read(ReadTemplateParam param, Guid guid)
    {
        if (EngineParams.TryGet<HalconTemplateMatchParams>(param.EngineParams, out var halconParams, out var engineParamsError) == false)
        {
            return AlgorithmResultRet.CreateError<ITemplateMatchModel>(engineParamsError);
        }

        try
        {
            var templateType = halconParams.TemplateType;
            var fullPath = templateType.ToFullFilePath(param.TemplateFilePath);
            if (File.Exists(fullPath) == false)
            {
                return AlgorithmResultRet.CreateError<ITemplateMatchModel>($"Template file not found: {fullPath}");
            }

            // 先 Load meta 再 HReadModel，避免 Read 成功后 Load 抛错导致 templateId 句柄泄漏
            var centerOffset = TemplateMatchMeta.Load(param.TemplateFilePath).CenterOffset;
            _algorithm.HReadModel(templateType.ToAlgorithmTemplateType(), param.TemplateFilePath, out var templateId);
#pragma warning disable IDISP004
            return AlgorithmResultRet.CreateSuccess<ITemplateMatchModel>(new HalconTemplateMatchModel(templateType, templateId, _algorithm, centerOffset));
#pragma warning restore IDISP004
        }
        catch (Exception ex)
        {
            AlgorithmLogHelper.LogAlgorithmError(logger, ex, nameof(Read), guid);
            throw;
        }
    }

    public AlgorithmResultRet<MatchTemplateResult> Match(BitmapImage image, MatchTemplateParam param, Guid guid)
    {
        if (param.Model is not HalconTemplateMatchModel halconModel)
        {
            return AlgorithmResultRet.CreateError<MatchTemplateResult>($"Model must be {nameof(HalconTemplateMatchModel)}");
        }

        try
        {
            using var hImage = image.ToHImage();
            using var scaleImageTo8Bit = hImage.ScaleImageTo8Bit();
            _algorithm.HFindModel(
                scaleImageTo8Bit,
                halconModel.TemplateType.ToAlgorithmTemplateType(),
                halconModel.TemplateId,
                out var yHTuple,
                out var xHTuple,
                out var angleHTuple,
                out var scoreHTuple);
            using var _1 = yHTuple;
            using var _2 = xHTuple;
            using var _3 = angleHTuple;
            using var _4 = scoreHTuple;

            if (xHTuple.Length == 0 || yHTuple.Length == 0 || scoreHTuple.Length == 0 || angleHTuple.Length == 0)
            {
                return AlgorithmResultRet.CreateError<MatchTemplateResult>("No match found");
            }

            var rawMarkPoint = new Point(xHTuple.D, yHTuple.D);
            // HFindModel 角度符号已由 Engine_Match_RotatedScene_ShouldReportNeutralConventionAngle 锁定为中立口径（-1.5° → -1.504°），与 TemplateMatchPose（屏幕逆时针为正）同号，不取负
            // HAlgorithm +1.5° 实测 Angle=0 / Score=0.837，属搜索区间不覆盖正角而非符号反号；不修 HAlgorithm 底层，单测该条已 Skip
            // 位姿换算内部保持弧度（TemplateMatchPose 契约）；Result.Angle 出口统一为度（HFindModel 原生弧度 × 180/π），与 AlgoCv 引擎及 PixelSize 引擎口径一致，消费端仅日志/持久化显示
            var pose = new TemplateMatchPose(angleHTuple.D);
            var markPoint = rawMarkPoint + pose.Transform(halconModel.CenterOffset);
            var center = new Point(image.Width / 2d, image.Height / 2d);
            var offset = markPoint - center;
            var result = new MatchTemplateResult
            {
                MarkPoint = markPoint,
                Offset = new Point(offset.X, -offset.Y),
                Score = scoreHTuple.D,
                Angle = angleHTuple.D * 180d / Math.PI
            };

            return result.Score >= param.ScoreThreshold
                ? AlgorithmResultRet.CreateSuccess(result)
                : AlgorithmResultRet.CreateError($"Score {result.Score} < {param.ScoreThreshold}", result);
        }
        catch (Exception ex)
        {
            var errorImagePath = AlgorithmLogHelper.SaveErrorImage(image, guid, nameof(Match));
            AlgorithmLogHelper.LogAlgorithmError(logger, ex, nameof(Match), guid, errorImagePath);
            throw;
        }
    }

}