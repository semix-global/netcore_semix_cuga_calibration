using Core.Models.Enums.Algorithm;
using Core.Models.Extensions;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using HalconDotNet;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using System.IO;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationAlgorithmService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationAlgorithmServiceMockImpl(
    ILogger<CalibrationAlgorithmServiceImpl> logger,
    CalibrationSetting calibrationSetting,
    AffineTransformation affineTransformation) : ICalibrationAlgorithmService
{
    private readonly CalibrationAlgorithmServiceImpl _calibrationAlgorithmServiceImpl = new(logger, calibrationSetting, affineTransformation);

    private readonly string _hazeImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\fftCh3HazeTestImg.jpg");
    private readonly string _shinyImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\fftCh3ShinyTestImg.jpg");

    public bool IsUseMock { get; set; } = true;

    public double GetQuality(BitmapImage image)
    {
        return Random.Shared.Next(100, 1000);
    }

    public double GetDarkFieldQuality(BitmapImage image)
    {
        return Random.Shared.Next(100, 1000);
    }

    public (double XQuality, double YQuality) GetXyQuality(BitmapImage image)
    {
        return (Random.Shared.Next(100, 1000), Random.Shared.Next(100, 1000));
    }

    public (double MtfX, double MtfY) ModulationTransferFunction(BitmapImage image, Rect roiRect)
    {
        return (Random.Shared.Next(100, 1000), Random.Shared.Next(100, 1000));
    }

    public BestFocus GetBestFocus(BitmapImage image, double startECS, double stopECS)
    {
        var size = image.Size;

        var xStrehlRatioPoints = Generate.LinearRangeInt32(0, 10, 300).Select(i => new Point(i, i is > 100 and < 200 ? Random.Shared.RandomDouble(0.8, 1d) : Random.Shared.RandomDouble(0, 0.3))).ToArray();

        var (_, _, _, _, xStrehlRatioFitYPredicted) = PolynomialCurve.Fit2(Vector<double>.Build.Dense([.. xStrehlRatioPoints.Select(t => t.X)]), Vector<double>.Build.Dense([.. xStrehlRatioPoints.Select(t => t.Y)]));
        var xStrehlRatioFitPoints = xStrehlRatioPoints.Index().Select(t => new Point(t.Item.X, xStrehlRatioFitYPredicted[t.Index])).ToArray();
        var xStrehlRatioColumnPoints = xStrehlRatioFitPoints.Select<Point, IReadOnlyList<Point>>(t => [new Point(t.X, t.Y - 0.1), new Point(t.X, t.Y + 0.1)]).ToArray();
        var xIntraRibbonFieldsPoints = Generate.LinearRange(-0.5, 0.1, 0.5).Select<double, IReadOnlyList<Point>>(t => [.. xStrehlRatioFitPoints.Select(tt => new Point(tt.X, tt.Y + t))]).ToArray();

        var xFieldTiltPoints = Enumerable.Range(0, 10).Select(i => new Point(i, Random.Shared.NextDouble())).ToArray();
        var (xFieldTiltFitSlope, xFieldTiltFitIntercept, xFieldTiltFitRSquared, xFieldTiltFitYPredicted) = PolynomialCurve.Fit1(Vector<double>.Build.Dense([.. xFieldTiltPoints.Select(t => t.X)]), Vector<double>.Build.Dense([.. xFieldTiltPoints.Select(t => t.Y)]));
        var xFieldTiltFitPoints = xFieldTiltPoints.Index().Select(t => new Point(t.Item.X, xFieldTiltFitYPredicted[t.Index])).ToArray();

        var yStrehlRatioPoints = Generate.LinearRangeInt32(0, 10, 300).Select(i => new Point(i, i is > 100 and < 200 ? Random.Shared.RandomDouble(0.8, 1d) : Random.Shared.RandomDouble(0, 0.3))).ToArray();

        var (_, _, _, _, yStrehlRatioFitYPredicted) = PolynomialCurve.Fit2(Vector<double>.Build.Dense([.. yStrehlRatioPoints.Select(t => t.X)]), Vector<double>.Build.Dense([.. yStrehlRatioPoints.Select(t => t.Y)]));
        var yStrehlRatioFitPoints = yStrehlRatioPoints.Index().Select(t => new Point(t.Item.X, yStrehlRatioFitYPredicted[t.Index])).ToArray();
        var yStrehlRatioColumnPoints = yStrehlRatioFitPoints.Select<Point, IReadOnlyList<Point>>(t => [new Point(t.X, t.Y - 0.5), new Point(t.X, t.Y + 0.5)]).ToArray();
        var yIntraRibbonFieldsPoints = Generate.LinearRange(-0.5, 0.1, 0.5).Select<double, IReadOnlyList<Point>>(t => [.. yStrehlRatioFitPoints.Select(tt => new Point(tt.X, tt.Y + t))]).ToArray();

        var yFieldTiltPoints = Enumerable.Range(0, 10).Select(i => new Point(i, Random.Shared.NextDouble())).ToArray();
        var (yFieldTiltFitSlope, yFieldTiltFitIntercept, yFieldTiltFitRSquared, yFieldTiltFitYPredicted) = PolynomialCurve.Fit1(Vector<double>.Build.Dense([.. yFieldTiltPoints.Select(t => t.X)]), Vector<double>.Build.Dense([.. yFieldTiltPoints.Select(t => t.Y)]));
        var yFieldTiltFitPoints = yFieldTiltPoints.Index().Select(t => new Point(t.Item.X, yFieldTiltFitYPredicted[t.Index])).ToArray();

        var bestFocus = new BestFocus
        {
            XStrehlRatioPoints = xStrehlRatioPoints,
            XStrehlRatioFitPoints = xStrehlRatioFitPoints,
            XStrehlRatioColumnPoints = xStrehlRatioColumnPoints,
            BestXStrehlRatioPoint = xStrehlRatioFitPoints.Maxima(t => t.Y).First(),
            XIntraRibbonFieldsPoints = xIntraRibbonFieldsPoints,
            XFieldTiltPoints = xFieldTiltPoints,
            XFieldTiltFitSlope = xFieldTiltFitSlope,
            XFieldTiltFitIntercept = xFieldTiltFitIntercept,
            XFieldTiltFitRSquared = xFieldTiltFitRSquared,
            XFieldTiltFitPoints = xFieldTiltFitPoints,
            YStrehlRatioPoints = yStrehlRatioPoints,
            YStrehlRatioFitPoints = yStrehlRatioFitPoints,
            YStrehlRatioColumnPoints = yStrehlRatioColumnPoints,
            BestYStrehlRatioPoint = yStrehlRatioFitPoints.Maxima(t => t.Y).First(),
            YIntraRibbonFieldsPoints = yIntraRibbonFieldsPoints,
            YFieldTiltPoints = yFieldTiltPoints,
            YFieldTiltFitSlope = yFieldTiltFitSlope,
            YFieldTiltFitIntercept = yFieldTiltFitIntercept,
            YFieldTiltFitRSquared = yFieldTiltFitRSquared,
            YFieldTiltFitPoints = yFieldTiltFitPoints
        };
        bestFocus.BestXStrehlRatioECS = startECS + bestFocus.BestXStrehlRatioPoint.X / size.Width * (stopECS - startECS);
        bestFocus.BestYStrehlRatioECS = startECS + bestFocus.BestYStrehlRatioPoint.X / size.Width * (stopECS - startECS);

        return bestFocus;
    }

    public Size GetPixelSize(BitmapImage image, Size standardMaskSquareSize, out BitmapImage drawingImage, out double angle)
    {
        var pixelSize = new Size(Random.Shared.Next(1, 10), Random.Shared.Next(1, 10));
        drawingImage = image.Copy();
        angle = Random.Shared.NextDouble();

        return pixelSize;
    }

    public double GetYPixelSize(BitmapImage image, double standardMaskSquareYSize, out BitmapImage drawingImage)
    {
        drawingImage = image.Copy();

        return Random.Shared.NextDouble();
    }

    public bool TryGenerateTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, BitmapImage image, string templateFilePath, Rect rect, out BitmapImage templateImage)
    {
        if (IsUseMock)
        {
            using var mockTemplateImage = image.ToHImage().ToRoi(rect);
            templateFilePath = algorithmTemplateTypeEnum.ToFullFilePath(templateFilePath);

            switch (algorithmTemplateTypeEnum)
            {
                case AlgorithmTemplateTypeEnum.Sharpe:
                    mockTemplateImage.SaveSharpeTemplate(templateFilePath);
                    break;

                case AlgorithmTemplateTypeEnum.Ncc:
                    mockTemplateImage.SaveNccTemplate(templateFilePath);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithmTemplateTypeEnum), algorithmTemplateTypeEnum, null);
            }

            templateImage = mockTemplateImage.ToBitmapImage();

            return true;
        }

        return _calibrationAlgorithmServiceImpl.TryGenerateTemplate(algorithmTemplateTypeEnum, image, templateFilePath, rect, out templateImage);
    }

    public bool TryReadTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, string templateFilePath, out HTuple templateId)
    {
        if (IsUseMock)
        {
            templateId = HalconFactory.EmptyHTuple;

            return true;
        }

        return _calibrationAlgorithmServiceImpl.TryReadTemplate(algorithmTemplateTypeEnum, templateFilePath, out templateId);
    }

    public bool TryCleanTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, HTuple templateId)
    {
        if (IsUseMock) return true;

        return _calibrationAlgorithmServiceImpl.TryCleanTemplate(algorithmTemplateTypeEnum, templateId);
    }

    public bool TryTemplateMatchToOffset(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, BitmapImage image, HTuple templateId, out Point markPoint, out Point offsetPoint, out double score, out double angle)
    {
        if (IsUseMock)
        {
            score = Random.Shared.NextDouble() * 10;
            angle = Random.Shared.Next(1, 10);
            offsetPoint = new Point(Random.Shared.Next(1, 10), Random.Shared.Next(1, 10));

            var size = new Size(image.Width, image.Height);
            markPoint = (Point)(size / 2d) + new Vector(offsetPoint.X, -offsetPoint.Y);

            return true;
        }

        return _calibrationAlgorithmServiceImpl.TryTemplateMatchToOffset(algorithmTemplateTypeEnum, image, templateId, out markPoint, out offsetPoint, out score, out angle);
    }

    public (BitmapImage drawingImage, double CenterChannelLightDiameter, double CenterChannelHorizontalDegree, Point CenterChannelLightCenterPosition, Point ReflectedLightCenterPosition) GetOpticsObjectiveYAngleResult(BitmapImage hazeImage, BitmapImage shinyWaferImage, double rotateAngle)
    {
        using var hazeImageTemp = new BitmapImage(_hazeImagePath);
        using var shinyWaferImageTemp = new BitmapImage(_shinyImagePath);

        return _calibrationAlgorithmServiceImpl.GetOpticsObjectiveYAngleResult(hazeImageTemp, shinyWaferImage, rotateAngle);
    }

    public Point GetChuckCenter(
        Point firstTopLeftPosition,
        Point secondTopLeftPosition,
        Point secondTopRightPosition,
        Point firstTopRightPosition,
        Point firstBottomLeftPosition,
        Point secondBottomLeftPosition,
        Point secondBottomRightPosition,
        Point firstBottomRightPosition)
    {
        return new Point(Random.Shared.Next(1, 10), Random.Shared.Next(1, 10));
    }

    public bool CalculateChuckStageMapError(
        StageMapDto stageMapDto,
        bool isXOnlyGantryError,
        Guid htmlLogUniqueId,
        int calculateContainRowMinCount,
        int calculateContainColumnMinCount,
        double alignmentThreshold,
        double gantryThreshold,
        double scaleThreshold,
        double diameter)
    {
        try
        {
            var (idealXArray, idealYArray) = stageMapDto.GetIdealArray();
            var (realXArray, realYArray, isInWaferArray, templateMathIsOkArray) = stageMapDto.GetRealArray();

            var idealXMatrix = Matrix<double>.Build.DenseOfArray(idealXArray);
            var idealYMatrix = Matrix<double>.Build.DenseOfArray(idealYArray);
            var realXMatrix = Matrix<double>.Build.DenseOfArray(realXArray);
            var realYMatrix = Matrix<double>.Build.DenseOfArray(realYArray);
            var isInWaferMatrix = Matrix<double>.Build.DenseOfArray(isInWaferArray);
            var templateMathIsOkMatrix = Matrix<double>.Build.DenseOfArray(templateMathIsOkArray);

            var (isSuccess, errorXMatrix, errorYMatrix) = affineTransformation.CalculateMatrixError(
                idealXMatrix,
                idealYMatrix,
                realXMatrix,
                realYMatrix,
                isInWaferMatrix,
                templateMathIsOkMatrix,
                isXOnlyGantryError,
                htmlLogUniqueId,
                calculateContainRowMinCount,
                calculateContainColumnMinCount: calculateContainColumnMinCount,
                diameter: diameter,
                alignmentThreshold: alignmentThreshold,
                gantryThreshold: gantryThreshold,
                scaleThreshold: scaleThreshold
            );

            for (var row = 0; row < stageMapDto.RowNumber; row++)
            {
                for (var column = 0; column < stageMapDto.ColumnNumber; column++)
                {
                    stageMapDto.ErrorMatrix[row][column] = new Point(errorXMatrix[row, column], errorYMatrix[row, column]);
                }
            }

            return isSuccess;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public StageMapDto ExpandStageMapDto(StageMapDto baseStageMap, StageMapDto mergeStageMap, Guid htmlLogUniqueId)
    {
        return affineTransformation.ExpandStageMapDto(baseStageMap, mergeStageMap, htmlLogUniqueId);
    }

    public double[] GetImageGrayYProjectionsPixels(BitmapImage image)
    {
        return _calibrationAlgorithmServiceImpl.GetImageGrayYProjectionsPixels(image);
    }

    public (Point CenterPosition, double Radius) FitCircle(IReadOnlyList<Point> points)
    {
        return _calibrationAlgorithmServiceImpl.FitCircle(points);
    }
}