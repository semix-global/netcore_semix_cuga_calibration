using Core.Algorithm.Helper;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.TemplateMatch;
using Core.Algorithm.Models.TemplateMatch;
using Net.Utilities.Calibration;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Implements.Mock.AlgoCSharp;

internal sealed class AlgoCvTemplateMatchModelMock(AlgoCvTemplateMatchParams engineParams) : ITemplateMatchModel
{
    public AlgoCvTemplateMatchParams EngineParams { get; } = engineParams;

    public void Dispose()
    {
    }
}

public sealed class AlgoCvTemplateMatchEngineMock : ITemplateMatchEngine
{
    public AlgorithmResultRet<ITemplateMatchModel> Generate(BitmapImage image, GenerateTemplateParam param, Guid guid)
    {
        if (TemplateMatchEngineParams.TryGet<AlgoCvTemplateMatchParams>(param.EngineParams, out var algoCvParams, out var engineParamsError) == false)
        {
            return AlgorithmResultRet.CreateError<ITemplateMatchModel>(engineParamsError);
        }

        using var roiImage = image.ToROI(param.Roi);
        roiImage.SaveImage(AlgorithmConstantHelper.TemplatePathToTemplateImagePath(param.TemplateFilePath));
        image.SaveImage(AlgorithmConstantHelper.TemplatePathToTemplateOriginImagePath(param.TemplateFilePath));

#pragma warning disable IDISP004
        return AlgorithmResultRet.CreateSuccess<ITemplateMatchModel>(new AlgoCvTemplateMatchModelMock(algoCvParams));
#pragma warning restore IDISP004
    }

    public AlgorithmResultRet<ITemplateMatchModel> Read(ReadTemplateParam param, Guid guid)
    {
        return TemplateMatchEngineParams.TryGet<AlgoCvTemplateMatchParams>(param.EngineParams, out var algoCvParams, out var engineParamsError) == false
            ? AlgorithmResultRet.CreateError<ITemplateMatchModel>(engineParamsError)
            : AlgorithmResultRet.CreateSuccess<ITemplateMatchModel>(new AlgoCvTemplateMatchModelMock(algoCvParams));
    }

    public AlgorithmResultRet<MatchTemplateResult> Match(BitmapImage image, MatchTemplateParam param, Guid guid)
    {
        if (param.Model is not AlgoCvTemplateMatchModelMock)
        {
            return AlgorithmResultRet.CreateError<MatchTemplateResult>($"Model must be {nameof(AlgoCvTemplateMatchModelMock)}");
        }

        var markPoint = new Point(image.Width / 2d, image.Height / 2d);
        var result = new MatchTemplateResult
        {
            MarkPoint = markPoint,
            Offset = Point.Origin,
            Score = 1d,
            Angle = 0d
        };

        return result.Score >= param.ScoreThreshold
            ? AlgorithmResultRet.CreateSuccess(result)
            : AlgorithmResultRet.CreateError($"Score {result.Score} < {param.ScoreThreshold}", result);
    }
}