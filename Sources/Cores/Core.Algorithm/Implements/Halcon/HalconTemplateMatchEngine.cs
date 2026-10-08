using Core.Algorithm.Helper;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.TemplateMatch;
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
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using System.IO;

namespace Core.Algorithm.Implements.Halcon;

public sealed class HalconTemplateMatchEngine(ILogger<HalconTemplateMatchEngine> logger) : ITemplateMatchEngine
{
    private static readonly string ErrorImageDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ErrorImages");

    private readonly HAlgorithm.Algorithm _algorithm = new();

    public AlgorithmResultRet<ITemplateMatchModel> Generate(BitmapImage image, GenerateTemplateParam param, Guid guid)
    {
        if (TemplateMatchEngineParams.TryGet<HalconTemplateMatchParams>(param.EngineParams, out var halconParams, out var engineParamsError) == false)
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

            _algorithm.HReadModel(templateType.ToAlgorithmTemplateType(), param.TemplateFilePath, out var templateId);
#pragma warning disable IDISP004
            return AlgorithmResultRet.CreateSuccess<ITemplateMatchModel>(new HalconTemplateMatchModel(templateType, templateId, _algorithm));
#pragma warning restore IDISP004
        }
        catch (Exception ex)
        {
            var errorImagePath = SaveErrorImage(image, guid, nameof(Generate));
            LogAlgorithmError(ex, nameof(Generate), guid, errorImagePath);
            throw;
        }
    }

    public AlgorithmResultRet<ITemplateMatchModel> Read(ReadTemplateParam param, Guid guid)
    {
        if (TemplateMatchEngineParams.TryGet<HalconTemplateMatchParams>(param.EngineParams, out var halconParams, out var engineParamsError) == false)
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

            _algorithm.HReadModel(templateType.ToAlgorithmTemplateType(), param.TemplateFilePath, out var templateId);
#pragma warning disable IDISP004
            return AlgorithmResultRet.CreateSuccess<ITemplateMatchModel>(new HalconTemplateMatchModel(templateType, templateId, _algorithm));
#pragma warning restore IDISP004
        }
        catch (Exception ex)
        {
            LogAlgorithmError(ex, nameof(Read), guid);
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

            var markPoint = new Point(xHTuple.D, yHTuple.D);
            var center = new Point(image.Width / 2d, image.Height / 2d);
            var offset = markPoint - center;
            var result = new MatchTemplateResult
            {
                MarkPoint = markPoint,
                Offset = new Point(offset.X, -offset.Y),
                Score = scoreHTuple.D,
                Angle = angleHTuple.D
            };

            return result.Score >= param.ScoreThreshold
                ? AlgorithmResultRet.CreateSuccess(result)
                : AlgorithmResultRet.CreateError($"Score {result.Score} < {param.ScoreThreshold}", result);
        }
        catch (Exception ex)
        {
            var errorImagePath = SaveErrorImage(image, guid, nameof(Match));
            LogAlgorithmError(ex, nameof(Match), guid, errorImagePath);
            throw;
        }
    }

    private static string SaveErrorImage(BitmapImage image, Guid guid, string methodName)
    {
        var errorImagePath = Path.Combine(ErrorImageDirectory, $"{methodName}_{guid}.jpg");
        DirectoryHelper.CreateFileDirectoryIfNotExists(errorImagePath);
        image.SaveImage(errorImagePath);
        return errorImagePath;
    }

    private void LogAlgorithmError(Exception ex, string methodName, Guid guid, string? errorImagePath = null)
    {
        if (string.IsNullOrEmpty(errorImagePath))
        {
            logger.LogHtmlError(ex, $"{methodName} Error", HtmlHeaderLevelEnum.Header6,
                new HtmlBullet(new
                {
                    ErrorMessage = ex.Message
                }), guid.LoggingHtml());
        }
        else
        {
            logger.LogHtmlError(ex, $"{methodName} Error", HtmlHeaderLevelEnum.Header6,
                new HtmlBullet(new
                {
                    ErrorImage = new HtmlImage(errorImagePath),
                    ImagePath = errorImagePath,
                    ErrorMessage = ex.Message
                }), guid.LoggingHtml());
        }
    }
}