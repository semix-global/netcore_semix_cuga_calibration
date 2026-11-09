using algocv_sharp;
using Core.Algorithm.Helper;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.TemplateMatch;
using Core.Algorithm.Models;
using Core.Algorithm.Models.TemplateMatch;
using Core.Models.Enums.Algorithm;
using Core.Models.Helper;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.AlgoCSharp;
using Net.Utilities.Calibration;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using System.IO;

namespace Core.Algorithm.Implements.AlgoCSharp;

public sealed class AlgoCvTemplateMatchEngine(ILogger<AlgoCvTemplateMatchEngine> logger) : ITemplateMatchEngine
{
    public AlgorithmResultRet<ITemplateMatchModel> Generate(BitmapImage image, GenerateTemplateParam param, Guid guid)
    {
        if (EngineParams.TryGet<AlgoCvTemplateMatchParams>(param.EngineParams, out var algoCvParams, out var engineParamsError) == false)
        {
            return AlgorithmResultRet.CreateError<ITemplateMatchModel>(engineParamsError);
        }

        TemplateMatchEngine? engine = null;
        try
        {
            engine = CreateAlgoEngine(algoCvParams);

            using var algoImage = image.ToAlgoCVImage();
            var rect = param.Roi;
            using var cropped = algoImage.Crop((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);

            // SetTemplate 内部经 Preprocess 灰度化/滤波生成独立缓冲（不与输入共享数据），方法返回后释放 cropped 不影响后续 Match
            engine.SetTemplate(cropped);
            cropped.Save(AlgorithmConstantHelper.TemplatePathToTemplateImagePath(param.TemplateFilePath));
            image.SaveImage(AlgorithmConstantHelper.TemplatePathToTemplateOriginImagePath(param.TemplateFilePath));

            var meta = param.ToTemplateMatchMeta();
            TemplateMatchMeta.Save(param.TemplateFilePath, meta);

#pragma warning disable IDISP001
            var model = new AlgoCvTemplateMatchModel(engine, algoCvParams, meta.CenterOffset);
#pragma warning restore IDISP001

            engine = null;

            return AlgorithmResultRet.CreateSuccess<ITemplateMatchModel>(model);
        }
        catch (Exception ex)
        {
            var errorImagePath = AlgorithmLogHelper.SaveErrorImage(image, guid, nameof(Generate));
            AlgorithmLogHelper.LogAlgorithmError(logger, ex, nameof(Generate), guid, errorImagePath);
            throw;
        }
        finally
        {
            engine?.Dispose();
        }
    }

    public AlgorithmResultRet<ITemplateMatchModel> Read(ReadTemplateParam param, Guid guid)
    {
        if (EngineParams.TryGet<AlgoCvTemplateMatchParams>(param.EngineParams, out var algoCvParams, out var engineParamsError) == false)
        {
            return AlgorithmResultRet.CreateError<ITemplateMatchModel>(engineParamsError);
        }

        TemplateMatchEngine? engine = null;
        try
        {
            var templateImagePath = CalibrationConstantsHelper.TemplatePathToTemplateImagePath(param.TemplateFilePath);
            if (File.Exists(templateImagePath) == false)
            {
                return AlgorithmResultRet.CreateError<ITemplateMatchModel>($"Template image not found: {templateImagePath}");
            }

            engine = CreateAlgoEngine(algoCvParams);

            using var templateImage = new Image(templateImagePath);
            // SetTemplate 为深拷贝（Preprocess 独立缓冲），templateImage 在方法结束后释放不影响后续 Match
            engine.SetTemplate(templateImage);

#pragma warning disable IDISP001
            var model = new AlgoCvTemplateMatchModel(engine, algoCvParams, TemplateMatchMeta.Load(param.TemplateFilePath).CenterOffset);
#pragma warning restore IDISP001
            engine = null;

            return AlgorithmResultRet.CreateSuccess<ITemplateMatchModel>(model);
        }
        catch (Exception ex)
        {
            AlgorithmLogHelper.LogAlgorithmError(logger, ex, nameof(Read), guid);
            throw;
        }
        finally
        {
            engine?.Dispose();
        }
    }

    public AlgorithmResultRet<MatchTemplateResult> Match(BitmapImage image, MatchTemplateParam param, Guid guid)
    {
        if (param.Model is not AlgoCvTemplateMatchModel algoCvModel)
        {
            return AlgorithmResultRet.CreateError<MatchTemplateResult>($"Model must be {nameof(AlgoCvTemplateMatchModel)}");
        }

        try
        {
            using var algoImage = image.ToAlgoCVImage();
            var match = algoCvModel.Engine.Match(algoImage);
            var rawMarkPoint = new Point(match.position_x, match.position_y);
            // AlgoCv 角度口径即中立口径（度、屏幕逆时针为正，同 cv::getRotationMatrix2D 约定）：Result.Angle 直接透出度（双引擎统一口径），位姿换算仅做度→弧度转换；scale 为相对模板原位的命中缩放
            var pose = new TemplateMatchPose(match.angle * Math.PI / 180d, match.scale);
            var markPoint = rawMarkPoint + pose.Transform(algoCvModel.CenterOffset);
            var center = new Point(image.Width / 2d, image.Height / 2d);
            var offset = markPoint - center;
            var result = new MatchTemplateResult
            {
                MarkPoint = markPoint,
                Offset = new Point(offset.X, -offset.Y),
                Score = match.score,
                Angle = match.angle
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

    private static TemplateMatchEngine CreateAlgoEngine(AlgoCvTemplateMatchParams algoCvParams) =>
        new(new TemplateMatchConfig(ToAlgoCvTemplateMatchMethod(algoCvParams.Method), algoCvParams.PyramidLevels));

    /// <summary>algocv_sharp 枚举映射收口：中立枚举到算法库枚举的转换只允许出现在实现层（Models/Interfaces 层禁止引用 algocv_sharp）</summary>
    private static TemplateMatchMethod ToAlgoCvTemplateMatchMethod(TemplateMatchMethodEnum method) => method switch
    {
        TemplateMatchMethodEnum.TmSqdiff => TemplateMatchMethod.TmSqdiff,
        TemplateMatchMethodEnum.TmSqdiffNormed => TemplateMatchMethod.TmSqdiffNormed,
        TemplateMatchMethodEnum.TmCcorr => TemplateMatchMethod.TmCcorr,
        TemplateMatchMethodEnum.TmCcorrNormed => TemplateMatchMethod.TmCcorrNormed,
        TemplateMatchMethodEnum.TmCcoeff => TemplateMatchMethod.TmCcoeff,
        _ => TemplateMatchMethod.TmCcoeffNormed
    };

}