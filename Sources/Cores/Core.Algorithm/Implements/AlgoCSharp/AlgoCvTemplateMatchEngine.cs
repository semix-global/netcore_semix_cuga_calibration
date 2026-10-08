using algocv_sharp;
using Core.Algorithm.Helper;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.TemplateMatch;
using Core.Algorithm.Models.TemplateMatch;
using Core.Models.Helper;
using Microsoft.Extensions.Logging;
using Net.Utilities.Calibration;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using System.IO;

namespace Core.Algorithm.Implements.AlgoCSharp;

public sealed class AlgoCvTemplateMatchEngine(ILogger<AlgoCvTemplateMatchEngine> logger) : ITemplateMatchEngine
{
    private static readonly string ErrorImageDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ErrorImages");

    public AlgorithmResultRet<ITemplateMatchModel> Generate(BitmapImage image, GenerateTemplateParam param, Guid guid)
    {
        if (TemplateMatchEngineParams.TryGet<AlgoCvTemplateMatchParams>(param.EngineParams, out var algoCvParams, out var engineParamsError) == false)
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

            engine.SetTemplate(cropped);
            cropped.Save(AlgorithmConstantHelper.TemplatePathToTemplateImagePath(param.TemplateFilePath));
            image.SaveImage(AlgorithmConstantHelper.TemplatePathToTemplateOriginImagePath(param.TemplateFilePath));

#pragma warning disable IDISP001
            var model = new AlgoCvTemplateMatchModel(engine, algoCvParams);
#pragma warning restore IDISP001

            engine = null;

            return AlgorithmResultRet.CreateSuccess<ITemplateMatchModel>(model);
        }
        catch (Exception ex)
        {
            var errorImagePath = SaveErrorImage(image, guid, nameof(Generate));
            LogAlgorithmError(ex, nameof(Generate), guid, errorImagePath);
            throw;
        }
        finally
        {
            engine?.Dispose();
        }
    }

    public AlgorithmResultRet<ITemplateMatchModel> Read(ReadTemplateParam param, Guid guid)
    {
        if (TemplateMatchEngineParams.TryGet<AlgoCvTemplateMatchParams>(param.EngineParams, out var algoCvParams, out var engineParamsError) == false)
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
            engine.SetTemplate(templateImage);

#pragma warning disable IDISP001
            var model = new AlgoCvTemplateMatchModel(engine, algoCvParams);
#pragma warning restore IDISP001
            engine = null;

            return AlgorithmResultRet.CreateSuccess<ITemplateMatchModel>(model);
        }
        catch (Exception ex)
        {
            LogAlgorithmError(ex, nameof(Read), guid);
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
            var markPoint = new Point(match.position_x, match.position_y);
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
            var errorImagePath = SaveErrorImage(image, guid, nameof(Match));
            LogAlgorithmError(ex, nameof(Match), guid, errorImagePath);
            throw;
        }
    }

    private static TemplateMatchEngine CreateAlgoEngine(AlgoCvTemplateMatchParams algoCvParams) => new(algoCvParams.ToTemplateMatchConfig());

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