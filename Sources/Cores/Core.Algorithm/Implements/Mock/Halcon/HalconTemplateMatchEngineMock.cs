using Core.Algorithm.Helper;
using Core.Algorithm.Interfaces;
using Core.Algorithm.Interfaces.TemplateMatch;
using Core.Algorithm.Models;
using Core.Algorithm.Models.TemplateMatch;
using Core.Models.Enums.Algorithm;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Calibration;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Implements.Mock.Halcon;

internal sealed class HalconTemplateMatchModelMock(
    AlgorithmTemplateTypeEnum templateType,
    Point centerOffset = default) : ITemplateMatchModel
{
    public AlgorithmTemplateTypeEnum TemplateType { get; } = templateType;

    public Point CenterOffset { get; } = centerOffset;

    public void Dispose()
    {
    }
}

public sealed class HalconTemplateMatchEngineMock : ITemplateMatchEngine
{
    public AlgorithmResultRet<ITemplateMatchModel> Generate(BitmapImage image, GenerateTemplateParam param, Guid guid)
    {
        if (EngineParams.TryGet<HalconTemplateMatchParams>(param.EngineParams, out var halconParams, out var engineParamsError) == false)
        {
            return AlgorithmResultRet.CreateError<ITemplateMatchModel>(engineParamsError);
        }

        using var hImage = image.ToHImage();
        using var scaleImageTo8Bit = hImage.ScaleImageTo8Bit();
        using var roiImage = scaleImageTo8Bit.ToROI(param.Roi);

        roiImage.Save(AlgorithmConstantHelper.TemplatePathToTemplateImagePath(param.TemplateFilePath));
        image.SaveImage(AlgorithmConstantHelper.TemplatePathToTemplateOriginImagePath(param.TemplateFilePath));

        var meta = param.ToTemplateMatchMeta();
        TemplateMatchMeta.Save(param.TemplateFilePath, meta);

#pragma warning disable IDISP004
        return AlgorithmResultRet.CreateSuccess<ITemplateMatchModel>(new HalconTemplateMatchModelMock(halconParams.TemplateType, meta.CenterOffset));
#pragma warning restore IDISP004
    }

    public AlgorithmResultRet<ITemplateMatchModel> Read(ReadTemplateParam param, Guid guid)
    {
        return EngineParams.TryGet<HalconTemplateMatchParams>(param.EngineParams, out var halconParams, out var engineParamsError) == false
            ? AlgorithmResultRet.CreateError<ITemplateMatchModel>(engineParamsError)
            : AlgorithmResultRet.CreateSuccess<ITemplateMatchModel>(new HalconTemplateMatchModelMock(halconParams.TemplateType, TemplateMatchMeta.Load(param.TemplateFilePath).CenterOffset));
    }

    public AlgorithmResultRet<MatchTemplateResult> Match(BitmapImage image, MatchTemplateParam param, Guid guid)
    {
        if (param.Model is not HalconTemplateMatchModelMock model)
        {
            return AlgorithmResultRet.CreateError<MatchTemplateResult>($"Model must be {nameof(HalconTemplateMatchModelMock)}");
        }

        var markPoint = new Point(image.Width / 2d, image.Height / 2d) + new TemplateMatchPose(0d).Transform(model.CenterOffset);
        var offset = markPoint - new Point(image.Width / 2d, image.Height / 2d);
        var result = new MatchTemplateResult
        {
            MarkPoint = markPoint,
            Offset = new Point(offset.X, -offset.Y),
            Score = 1d,
            Angle = 0d
        };

        return result.Score >= param.ScoreThreshold
            ? AlgorithmResultRet.CreateSuccess(result)
            : AlgorithmResultRet.CreateError($"Score {result.Score} < {param.ScoreThreshold}", result);
    }
}