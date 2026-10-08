using Core.Algorithm.Helper;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.TemplateMatch;
using Core.Algorithm.Models.TemplateMatch;
using Core.Models.Enums.Algorithm;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Calibration;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Implements.Mock.Halcon;

internal sealed class HalconTemplateMatchModelMock(AlgorithmTemplateTypeEnum templateType) : ITemplateMatchModel
{
    public AlgorithmTemplateTypeEnum TemplateType { get; } = templateType;

    public void Dispose()
    {
    }
}

public sealed class HalconTemplateMatchEngineMock : ITemplateMatchEngine
{
    public AlgorithmResultRet<ITemplateMatchModel> Generate(BitmapImage image, GenerateTemplateParam param, Guid guid)
    {
        if (TemplateMatchEngineParams.TryGet<HalconTemplateMatchParams>(param.EngineParams, out var halconParams, out var engineParamsError) == false)
        {
            return AlgorithmResultRet.CreateError<ITemplateMatchModel>(engineParamsError);
        }

        using var hImage = image.ToHImage();
        using var scaleImageTo8Bit = hImage.ScaleImageTo8Bit();
        using var roiImage = scaleImageTo8Bit.ToROI(param.Roi);

        roiImage.Save(AlgorithmConstantHelper.TemplatePathToTemplateImagePath(param.TemplateFilePath));
        image.SaveImage(AlgorithmConstantHelper.TemplatePathToTemplateOriginImagePath(param.TemplateFilePath));

#pragma warning disable IDISP004
        return AlgorithmResultRet.CreateSuccess<ITemplateMatchModel>(new HalconTemplateMatchModelMock(halconParams.TemplateType));
#pragma warning restore IDISP004
    }

    public AlgorithmResultRet<ITemplateMatchModel> Read(ReadTemplateParam param, Guid guid)
    {
        return TemplateMatchEngineParams.TryGet<HalconTemplateMatchParams>(param.EngineParams, out var halconParams, out var engineParamsError) == false
            ? AlgorithmResultRet.CreateError<ITemplateMatchModel>(engineParamsError)
            : AlgorithmResultRet.CreateSuccess<ITemplateMatchModel>(new HalconTemplateMatchModelMock(halconParams.TemplateType));
    }

    public AlgorithmResultRet<MatchTemplateResult> Match(BitmapImage image, MatchTemplateParam param, Guid guid)
    {
        if (param.Model is not HalconTemplateMatchModelMock)
        {
            return AlgorithmResultRet.CreateError<MatchTemplateResult>($"Model must be {nameof(HalconTemplateMatchModelMock)}");
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
