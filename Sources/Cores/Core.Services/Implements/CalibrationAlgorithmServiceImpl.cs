using Core.Models.Enums.Algorithm;
using Core.Models.Exceptions;
using Core.Models.Extensions;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Core.Utilities;
using HalconDotNet;
using HAlgorithm;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Extensions;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Algorithms.Modules.CurveFitting;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Geometries;
using System.IO;
using Rect = Net.Utilities.Models.Geometries.Rect;

namespace Core.Services.Implements;

[IOCAppService(ServiceType = typeof(ICalibrationAlgorithmService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationAlgorithmServiceImpl(
    ILogger<CalibrationAlgorithmServiceImpl> logger,
    CalibrationSetting calibrationSetting,
    AffineTransformation affineTransformation) : ICalibrationAlgorithmService
{
    private readonly Algorithm _algorithm = new();

    public double GetQuality(BitmapImage image)
    {
        // 适应彩色和灰度图像, 方差越大, 说明图像越清晰
        using var hImage = image.ToHImage();
        _algorithm.LaplaceDefinition(hImage, out var meanTuple);
        using var _ = meanTuple;

        return meanTuple.D;
    }

    public double GetDarkFieldQuality(BitmapImage image)
    {
        using var hImage = image.ToHImage();
        _algorithm.DarkLaplaceDefinition(hImage, out var meanTuple);
        using var _ = meanTuple;

        return meanTuple.D;
    }

    public (double XQuality, double YQuality) GetXyQuality(BitmapImage image)
    {
        using var hImage = image.ToHImage();
        _algorithm.DarkDefinition(hImage, out var meanTupleY, out var meanTupleX);

        using var _1 = meanTupleX;
        using var _2 = meanTupleY;

        return (meanTupleX.D, meanTupleY.D);
    }

    public (double MtfX, double MtfY) ModulationTransferFunction(BitmapImage image, Rect roiRect)
    {
        using var hImage = image.ToHImage();
        using var roiImage = hImage.ToRoi(roiRect);

        _algorithm.WuMTF(roiImage, out var mtfX, out var mtfY);

        using var _1 = mtfX;
        using var _2 = mtfY;

        return (mtfX.D, mtfY.D);
    }

    public BestFocus GetBestFocus(BitmapImage image, double startECS, double stopECS)
    {
        using var hImage = image.ToHImage();
        var size = hImage.GetSize();

        #region 算法调用

        _algorithm.STLR_kla(
            hImage,
            out var hvXListHTuple,
            out var hvXRatioMaxHTuple,
            out var hvXRatioMeanHTuple,
            out var hvXRatioMinHTuple,
            out var hvYRatioMaxHTuple,
            out var hvYRatioMeanHTuple,
            out var hvYRatioMinHTuple,
            out var hvXValuesHTuple,
            out var hvIndXHTuple,
            out var hvXMaxHTuple,
            out var hvYValuesHTuple,
            out var hvIndYHTuple,
            out var hvYMaxHTuple,
            out var hvPlotXHTuple,
            out var hvPlotYHTuple,
            out var xStrehlList,
            out var yStrehlList,
            out var hvRowBeginXHTuple,
            out var hvColBeginXHTuple,
            out var hvRowEndXHTuple,
            out var hvColEndXHTuple,
            out var hvKxHTuple,
            out var hvRowBeginYHTuple,
            out var hvColBeginYHTuple,
            out var hvRowEndYHTuple,
            out var hvColEndYHTuple,
            out var hvKyHTuple,
            out var hvPercentMeanHTuple);

        using var _0 = hvXListHTuple;
        using var _1 = hvXRatioMaxHTuple;
        using var _2 = hvXRatioMeanHTuple;
        using var _3 = hvXRatioMinHTuple;
        using var _4 = hvYRatioMaxHTuple;
        using var _5 = hvYRatioMeanHTuple;
        using var _6 = hvYRatioMinHTuple;
        using var _7 = hvXValuesHTuple;
        using var _8 = hvIndXHTuple;
        using var _9 = hvXMaxHTuple;
        using var _10 = hvYValuesHTuple;
        using var _11 = hvIndYHTuple;
        using var _12 = hvYMaxHTuple;
        using var _13 = hvPlotXHTuple;
        using var _14 = hvPlotYHTuple;
        using var _15 = hvRowBeginXHTuple;
        using var _16 = hvColBeginXHTuple;
        using var _17 = hvRowEndXHTuple;
        using var _18 = hvColEndXHTuple;
        using var _19 = hvKxHTuple;
        using var _20 = hvRowBeginYHTuple;
        using var _21 = hvColBeginYHTuple;
        using var _22 = hvRowEndYHTuple;
        using var _23 = hvColEndYHTuple;
        using var _24 = hvKyHTuple;
        using var _25 = hvPercentMeanHTuple;

        #endregion

        var xs = Generate.LinearRangeInt32(0, hvXListHTuple.Length - 1);
        var xFieldTiltPoints = Generate.LinearRangeInt32(0, hvPlotXHTuple.Length - 1).Select(t => new Point(t, hvPlotXHTuple[t].D)).ToArray();
        var (xFieldTiltFitSlope, xFieldTiltFitIntercept, xFieldTiltFitRSquared, xFieldTiltFitYPredicted) = PolynomialCurve.Fit1(Vector<double>.Build.Dense([.. xFieldTiltPoints.Select(t => t.X)]), Vector<double>.Build.Dense([.. xFieldTiltPoints.Select(t => t.Y)]));
        var xFieldTiltFitPoints = xFieldTiltPoints.Index().Select(t => new Point(t.Item.X, xFieldTiltFitYPredicted[t.Index])).ToArray();

        var yFieldTiltPoints = Generate.LinearRangeInt32(0, hvPlotYHTuple.Length - 1).Select(t => new Point(t, hvPlotYHTuple[t].D)).ToArray();
        var (yFieldTiltFitSlope, yFieldTiltFitIntercept, yFieldTiltFitRSquared, yFieldTiltFitYPredicted) = PolynomialCurve.Fit1(Vector<double>.Build.Dense([.. yFieldTiltPoints.Select(t => t.X)]), Vector<double>.Build.Dense([.. yFieldTiltPoints.Select(t => t.Y)]));
        var yFieldTiltFitPoints = yFieldTiltPoints.Index().Select(t => new Point(t.Item.X, yFieldTiltFitYPredicted[t.Index])).ToArray();

        var bestFocus = new BestFocus
        {
            XStrehlRatioPoints = [.. xs.Select(t => new Point(hvXListHTuple[t].D, hvXRatioMeanHTuple[t].D))],
            XStrehlRatioFitPoints = [.. xs.Select(t => new Point(hvXListHTuple[t].D, hvXValuesHTuple[t].D))],
            XStrehlRatioColumnPoints = [.. xs.Select<int, IReadOnlyList<Point>>(t => [new Point(hvXListHTuple[t].D, hvXRatioMinHTuple[t].D), new Point(hvXListHTuple[t].D, hvXRatioMaxHTuple[t].D)])],
            BestXStrehlRatioPoint = new Point(hvIndXHTuple.D, hvXMaxHTuple.D),
            XIntraRibbonFieldsPoints = [.. xStrehlList.Select<double[], IReadOnlyList<Point>>(t => [.. xs.Select(tt => new Point(hvXListHTuple[tt].D, t[tt]))])],
            XFieldTiltPoints = xFieldTiltPoints,
            XFieldTiltFitSlope = xFieldTiltFitSlope,
            XFieldTiltFitIntercept = xFieldTiltFitIntercept,
            XFieldTiltFitRSquared = xFieldTiltFitRSquared,
            XFieldTiltFitPoints = xFieldTiltFitPoints,
            YStrehlRatioPoints = [.. xs.Select(t => new Point(hvXListHTuple[t].D, hvYRatioMeanHTuple[t].D))],
            YStrehlRatioFitPoints = [.. xs.Select(t => new Point(hvXListHTuple[t].D, hvYValuesHTuple[t].D))],
            YStrehlRatioColumnPoints = [.. xs.Select<int, IReadOnlyList<Point>>(t => [new Point(hvXListHTuple[t].D, hvYRatioMinHTuple[t].D), new Point(hvXListHTuple[t].D, hvYRatioMaxHTuple[t].D)])],
            BestYStrehlRatioPoint = new Point(hvIndYHTuple.D, hvYMaxHTuple.D),
            YIntraRibbonFieldsPoints = [.. yStrehlList.Select<double[], IReadOnlyList<Point>>(t => [.. xs.Select(tt => new Point(hvXListHTuple[tt].D, t[tt]))])],
            YFieldTiltPoints = yFieldTiltPoints,
            YFieldTiltFitSlope = yFieldTiltFitSlope,
            YFieldTiltFitIntercept = yFieldTiltFitIntercept,
            YFieldTiltFitRSquared = yFieldTiltFitRSquared,
            YFieldTiltFitPoints = yFieldTiltFitPoints,
            SpotAreaPercentMean = hvPercentMeanHTuple.D
        };

        bestFocus.BestXStrehlRatioECS = startECS + bestFocus.BestXStrehlRatioPoint.X / size.Width * (stopECS - startECS);
        bestFocus.BestYStrehlRatioECS = startECS + bestFocus.BestYStrehlRatioPoint.X / size.Width * (stopECS - startECS);
        bestFocus.IsAlgorithmOk = true;

        return bestFocus;
    }

    public Size GetPixelSize(BitmapImage image, Size standardMaskSquareSize, out BitmapImage drawingImage, out double angle)
    {
        using var hImage = image.ToHImage();
        _algorithm.CalculatePixSize(hImage, out var drawingImageObj, standardMaskSquareSize.Height, standardMaskSquareSize.Width, out var yTuple, out var xTuple, out var angleX);
        using var _1 = xTuple;
        using var _2 = yTuple;
        using var _3 = angleX;
        angle = angleX.D;

        using var drawingHImage = new HImage(drawingImageObj);
        drawingImage = drawingHImage.ToBitmapImage();
        return new Size(xTuple.D, yTuple.D);
    }

    public double GetYPixelSize(BitmapImage image, double standardMaskSquareYSize, out BitmapImage drawingImage)
    {
        using var hImage = image.ToHImage();
        var data = hImage.RAW16BitsPerPixelToMatrix();
        var matrix = Matrix<double>.Build.DenseOfArray(data);
        var projectionYs = matrix.RowSums() / matrix.ColumnCount;

        // 使用AMPD算法找出波峰
        var signal = Vector<double>.Build.DenseOfEnumerable(projectionYs.Select(t => -t));
        var peaks = AutomaticMPeakDetection.Ampd(signal);

        var xDifferences = peaks
            .Zip(peaks.Skip(1), (prev, next) => (double)next - prev)
            .ToArray();
        var (indexes, filterXDifferences) = Filter.MAD(xDifferences);

        // 所有后一个减去前一个，得到差值, 然后取得均值
        var mean = filterXDifferences.Average();

        using var drawHImage = hImage.DrawLines(
            [
                .. indexes
                    .SelectMany(t => (int[])[t, t + 1])
                    .Distinct()
                    .OrderBy(t => t)
                    .Select(index => (new Point(0, peaks[index]), new Point(image.Width, peaks[index])))
            ],
            5);
        drawingImage = drawHImage.ToBitmapImage();

        return standardMaskSquareYSize / mean;
    }

    public bool TryGenerateTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, BitmapImage image, string templateFilePath, Rect rect, out BitmapImage templateImage)
    {
        templateImage = BitmapImage.Random(2448, 2048, 10);

        try
        {
            using var hImage = image.ToHImage();
            using var scaleImageTo8Bit = hImage.ScaleImageTo8Bit();
            var temp = algorithmTemplateTypeEnum.ToFullFilePath(templateFilePath);
            DirectoryHelper.CreateFileDirectoryIfNotExists(temp);
            FileHelper.DeleteFileIfExists(temp);

            _algorithm.HCreateModelXY(scaleImageTo8Bit, out var templateImageObj, algorithmTemplateTypeEnum.ToAlgorithmTemplateType(), templateFilePath, rect.X, rect.Y, rect.Width, rect.Height);

            using var drawingHImage = new HImage(templateImageObj);

            templateImage.Dispose();

            templateImage = drawingHImage.ToBitmapImage();

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(new AlgorithmException(ex), "{@Name}: Try Generate Template Failed", nameof(CalibrationAlgorithmServiceImpl));
            return false;
        }
    }

    public bool TryReadTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, string templateFilePath, out HTuple templateId)
    {
        templateId = HalconFactory.EmptyHTuple;

        try
        {
            var temp = algorithmTemplateTypeEnum.ToFullFilePath(templateFilePath);
            if (File.Exists(temp) == false) throw new FileNotFoundException(nameof(templateFilePath), temp);

            _algorithm.HReadModel(algorithmTemplateTypeEnum.ToAlgorithmTemplateType(), templateFilePath, out templateId);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(new AlgorithmException(ex), "{@Name}: Try Read Template Failed", nameof(CalibrationAlgorithmServiceImpl));
            return false;
        }
    }

    public bool TryCleanTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, HTuple templateId)
    {
        try
        {
            _algorithm.HClearModel(algorithmTemplateTypeEnum.ToAlgorithmTemplateType(), templateId);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(new AlgorithmException(ex), "{@Name}: Try Clean Template Failed", nameof(CalibrationAlgorithmServiceImpl));
            return false;
        }
    }

    public bool TryTemplateMatchToOffset(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, BitmapImage image, HTuple templateId, out Point markPoint, out Point offsetPoint, out double score, out double angle)
    {
        markPoint = Point.Origin;
        offsetPoint = Point.Origin;
        score = 0;
        angle = 0;

        try
        {
            using var hImage = image.ToHImage();
            using var scaleImageTo8Bit = hImage.ScaleImageTo8Bit();
            _algorithm.HFindModel(scaleImageTo8Bit, algorithmTemplateTypeEnum.ToAlgorithmTemplateType(), templateId, out var yHTuple, out var xHTuple, out var angleHTuple, out var scoreHTuple);
            using var _1 = yHTuple;
            using var _2 = xHTuple;
            using var _3 = angleHTuple;
            using var _4 = scoreHTuple;
            if (xHTuple.Length == 0 || yHTuple.Length == 0 || scoreHTuple.Length == 0 || angleHTuple.Length == 0) return false;

            var size = scaleImageTo8Bit.GetSize();
            markPoint = new Point(xHTuple.D, yHTuple.D);
            score = scoreHTuple.D;
            angle = angleHTuple.D;

            var templateMatchScoreThreshold = algorithmTemplateTypeEnum.ToTemplateMatchScoreThreshold(calibrationSetting);
            var tryGetMatchPosition = score >= templateMatchScoreThreshold;
            if (tryGetMatchPosition == false)
            {
                //logger.LogWarning("{@Name} Error: Match Score is Less Than Threshold {@MatchScoreThreshold} > {@Score}", nameof(CalibrationAlgorithmServiceImpl), templateMatchScoreThreshold, score);
                return false;
            }

            var offset = markPoint - (Point)((Size)size / 2d);
            offsetPoint = new Point(offset.X, -offset.Y);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(new AlgorithmException(ex), "{@Name}: Try Template Math To Offset Failed", nameof(CalibrationAlgorithmServiceImpl));
            return false;
        }
    }

    public (BitmapImage drawingImage, double CenterChannelLightDiameter, double CenterChannelHorizontalDegree, Point CenterChannelLightCenterPosition, Point ReflectedLightCenterPosition) GetOpticsObjectiveYAngleResult(BitmapImage hazeImage, BitmapImage shinyWaferImage, double rotateAngle)
    {
        using var hHazeImage = hazeImage.ToHImage();
        using var hShinyWaferImage = shinyWaferImage.ToHImage();
        _algorithm.CalculateTwoRegionCenter(hHazeImage, hShinyWaferImage, out var resultImage, rotateAngle, out var diameter, out var angle, out var dRow, out var dCol, out var row, out var col);

        using var drawingHImage = new HImage(resultImage);
        var drawingImage = drawingHImage.ToBitmapImage();

        return (drawingImage, diameter.D, angle.D, new Point(dCol.D, dRow.D), new Point(col.D, row.D));
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
        double[] xArray = [firstTopLeftPosition.X, secondTopLeftPosition.X, secondTopRightPosition.X, firstTopRightPosition.X, firstBottomLeftPosition.X, secondBottomLeftPosition.X, secondBottomRightPosition.X, firstBottomRightPosition.X];
        double[] yArray = [firstTopLeftPosition.Y, secondTopLeftPosition.Y, secondTopRightPosition.Y, firstTopRightPosition.Y, firstBottomLeftPosition.Y, secondBottomLeftPosition.Y, secondBottomRightPosition.Y, firstBottomRightPosition.Y];
        _algorithm.WaferCenterCalculate(xArray, yArray, out var xHTuple, out var yHTuple);
        using var _1 = xHTuple;
        using var _2 = yHTuple;

        var centerX = xHTuple.D;
        var centerY = yHTuple.D;

        return new Point(centerX, centerY);
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
            calculateContainRowMinCount: calculateContainRowMinCount,
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

    public StageMapDto ExpandStageMapDto(StageMapDto baseStageMap, StageMapDto mergeStageMap, Guid htmlLogUniqueId)
    {
        return affineTransformation.ExpandStageMapDto(baseStageMap, mergeStageMap, htmlLogUniqueId);
    }

    public double[] GetImageGrayYProjectionsPixels(BitmapImage image)
    {
        using var hImage = image.ToHImage();
        _algorithm.LightSpot(hImage, out var yValue);

        return yValue.ToDArr();
    }

    public (Point CenterPosition, double Radius) FitCircle(IReadOnlyList<Point> points)
    {
        _algorithm.FindCircle(points.Select(t => t.X).ToList(), points.Select(t => t.Y).ToList(), out var yValue, out var xValue, out var radius);

        return (new Point(xValue.D, yValue.D), radius.D);
    }
}