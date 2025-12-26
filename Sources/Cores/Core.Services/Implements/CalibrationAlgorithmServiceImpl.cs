using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Algorithm;
using Core.Models.Exceptions;
using Core.Models.Extensions;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using HalconDotNet;
using HAlgorithm;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Algorithms.Modules;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Helpers.Files;
using Net.Utilities.Models.Geometries;
using System.IO;
using Core.Utilities;
using Rect = Net.Utilities.Models.Geometries.Rect;

namespace Core.Services.Implements;

[IOCAppService(ServiceType = typeof(ICalibrationAlgorithmService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationAlgorithmServiceImpl(
    ILogger<CalibrationAlgorithmServiceImpl> logger,
    CalibrationSetting calibrationSetting,
    AffineTransformation affineTransformation) : ICalibrationAlgorithmService
{
    private readonly Algorithm _algorithm = new();

    public string Version => Algorithm.Version;

    public double GetQuality(HImage image)
    {
        // 适应彩色和灰度图像, 方差越大, 说明图像越清晰
        _algorithm.LaplaceDefinition(image, out var meanTuple);
        using var _ = meanTuple;

        return meanTuple.D;
    }

    public double GetDarkFieldQuality(HImage image)
    {
        using var scaleImage = image.ScaleImageTo8Bit();
        // 适应彩色和灰度图像, 方差越大, 说明图像越清晰
        _algorithm.DarkLaplaceDefinition(scaleImage, out var meanTuple);
        using var _ = meanTuple;

        return meanTuple.D;
    }

    public (double XQuality, double YQuality) GetXyQuality(HImage image)
    {
        _algorithm.DarkDefinition(image, out var meanTupleY, out var meanTupleX);

        using var _1 = meanTupleX;
        using var _2 = meanTupleY;

        return (meanTupleX.D, meanTupleY.D);
    }

    public (double MtfX, double MtfY) ModulationTransferFunction(HImage image, Rect roiRect)
    {
        using var roiImage = image.ToRoi(roiRect);

        _algorithm.WuMTF(roiImage, out var mtfX, out var mtfY);

        using var _1 = mtfX;
        using var _2 = mtfY;

        return (mtfX.D, mtfY.D);
    }

    public (double Width, double Height) GetLightQuality(HImage image, Rect roiRect)
    {
        using var roiImage = image.ToRoi(roiRect);

        _algorithm.LightQuality(roiImage, out var width, out var height);

        using var _1 = width;
        using var _2 = height;

        return (width.D, height.D);
    }

    public Size GetPixelSize(HImage image, Size standardMaskSquareSize, out HImage drawingImage, out double angle)
    {
        _algorithm.CalculatePixSize(image, out var drawingImageObj, standardMaskSquareSize.Height, standardMaskSquareSize.Width, out var yTuple, out var xTuple, out var angleX);
        using var _1 = xTuple;
        using var _2 = yTuple;
        using var _3 = angleX;
        angle = angleX.D;
        drawingImage = new HImage(drawingImageObj);
        return new Size(xTuple.D, yTuple.D);
    }

    public double GetYPixelSize(DarkFieldImageDto image, double standardMaskSquareYSize)
    {
        var y = image.Image.GetHorizontalProjects();

        // 使用AMPD算法找出波峰
        var signal = Vector<double>.Build.DenseOfEnumerable(y.Select(t => -t));
        var peaks = AutomaticMPeakDetection.Ampd(signal);
        // 所有后一个减去前一个，得到差值, 然后取得均值
        var mean = peaks.Skip(1).Select((t, i) => (double)t - peaks[i]).Average();

        return standardMaskSquareYSize / mean;
    }

    public bool TryGenerateTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, HImage image, string templateFilePath, Rect rect, out HImage templateImage)
    {
        templateImage = HalconFactory.EmptyHImage;

        try
        {
            using var scaleImageTo8Bit = image.ScaleImageTo8Bit();
            var temp = algorithmTemplateTypeEnum.ToFullFilePath(templateFilePath);
            DirectoryHelper.CreateFileDirectoryIfNotExists(temp);
            FileHelper.DeleteFileIfExists(temp);

            _algorithm.HCreateModelXY(scaleImageTo8Bit, out var templateImageObj, algorithmTemplateTypeEnum.ToAlgorithmTemplateType(), templateFilePath, rect.X, rect.Y, rect.Width, rect.Height);

            templateImage = new HImage(templateImageObj);
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

    public bool TryTemplateMatchToOffset(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, HImage image, HTuple templateId, out Point markPoint, out Point offsetPoint, out double score, out double angle)
    {
        markPoint = Point.Origin;
        offsetPoint = Point.Origin;
        score = 0;
        angle = 0;

        try
        {
            using var scaleImageTo8Bit = image.ScaleImageTo8Bit();
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

            var offset = markPoint - (Point)(size / 2d);
            offsetPoint = new Point(offset.X, -offset.Y);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(new AlgorithmException(ex), "{@Name}: Try Template Math To Offset Failed", nameof(CalibrationAlgorithmServiceImpl));
            return false;
        }
    }

    public bool TryGenerateProjectionTemplate(HImage image, string templateFilePath, out HImage templateImage)
    {
        templateImage = HalconFactory.EmptyHImage;

        try
        {
            using var scaleImageTo8Bit = image.ScaleImageTo8Bit();
            templateImage = scaleImageTo8Bit.Copy();

            DirectoryHelper.CreateFileDirectoryIfNotExists($"{templateFilePath}.x.ncc");
            FileHelper.DeleteFileIfExists(templateFilePath);

            _algorithm.ProjectionCreateModel(scaleImageTo8Bit, $"{templateFilePath}.x", $"{templateFilePath}.y");

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(new AlgorithmException(ex), "{@Name}: Try Generate Template Failed", nameof(CalibrationAlgorithmServiceImpl));
            return false;
        }
    }

    public bool TryReadProjectionTemplate(string templateFilePath, out HTuple templateXId, out HTuple templateYId)
    {
        templateXId = HalconFactory.EmptyHTuple;
        templateYId = HalconFactory.EmptyHTuple;

        try
        {
            if (File.Exists($"{templateFilePath}.x.ncc") == false) throw new FileNotFoundException(nameof(templateFilePath));
            if (File.Exists($"{templateFilePath}.y.ncc") == false) throw new FileNotFoundException(nameof(templateFilePath));

            _algorithm.ProjectionReadModel($"{templateFilePath}.x", $"{templateFilePath}.y", out templateXId, out templateYId);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(new AlgorithmException(ex), "{@Name}: Try Read Template Failed", nameof(CalibrationAlgorithmServiceImpl));
            return false;
        }
    }

    public bool TryCleanProjectionTemplate(HTuple templateXId, HTuple templateYId)
    {
        try
        {
            _algorithm.ProjectionClearModel(templateXId, templateYId);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(new AlgorithmException(ex), "{@Name}: Try Clean Template Failed", nameof(CalibrationAlgorithmServiceImpl));
            return false;
        }
    }

    public bool TryProjectionTemplateMatchToOffset(HImage image, HTuple templateXId, HTuple templateYId, out Point markPoint, out Point offsetPoint)
    {
        markPoint = Point.Origin;
        offsetPoint = Point.Origin;

        try
        {
            using var scaleImageTo8Bit = image.ScaleImageTo8Bit();
            _algorithm.ProjectionFindModel(scaleImageTo8Bit, templateXId, templateYId, out var yHTuple, out var xHTuple);
            using var _1 = yHTuple;
            using var _2 = xHTuple;
            if (xHTuple.Length == 0 || yHTuple.Length == 0) return false;

            var size = scaleImageTo8Bit.GetSize();
            markPoint = new Point(xHTuple.D, yHTuple.D);

            var offset = markPoint - (Point)(size / 2d);
            offsetPoint = new Point(offset.X, -offset.Y);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(new AlgorithmException(ex), "{@Name}: Try Template Math To Offset Failed", nameof(CalibrationAlgorithmServiceImpl));
            return false;
        }
    }

    public (Size Size, long BodyBytesStartIndex, long BodyBytesLength) GetSize(byte[] rawBytes)
    {
        return RawImageFactory.GetSize(rawBytes);
    }

    public byte[] ToRawBytes(byte[] bodyBytes, Size size)
    {
        return RawImageFactory.BodyAddHeaderFooter(bodyBytes, size);
    }

    public (HImage Image, short[,] Matrix) ToImageInfo(byte[] rawBytes)
    {
        var (matrix, _) = RawImageFactory.ToMatrix(rawBytes);

        using var imageObj = _algorithm.GetDataImage(rawBytes);
        return (new HImage(imageObj), matrix);
    }

    public (HImage Image, short[,] Matrix, byte[] RawBytes) ToHorizontalFlipImageInfo(byte[] rawBytes)
    {
        var (matrix, horizontalFlipRawBytes, _) = RawImageFactory.ToHorizontalFlipMatrix(rawBytes);

        using var imageObj = _algorithm.GetDataImage(horizontalFlipRawBytes);
        return (new HImage(imageObj), matrix, horizontalFlipRawBytes);
    }

    public (List<string> DatAvg, List<string> Data) GetPmtGain(Dictionary<int, List<int>> dicPmtData, int lineValue, double minValue, double maxValue)
    {
        _algorithm.AutoPMT(dicPmtData, lineValue, minValue, maxValue, out List<string> datavge1, out List<string> data1);

        return (datavge1, data1);
    }

    public (HImage drawingImage, double CenterChannelLightDiameter, double CenterChannelHorizontalDegree, Point CenterChannelLightCenterPosition, Point ReflectedLightCenterPosition) GetOpticsObjectiveYAngleResult(HImage hazeImage, HImage shinyWaferImage, double rotateAngle)
    {
        _algorithm.CalculateTwoRegionCenter(hazeImage, shinyWaferImage, out var resultImage, rotateAngle, out var diameter, out var angle, out var dRow, out var dCol, out var row, out var col);

        var drawingImage = new HImage(resultImage);

        return (drawingImage, diameter.D, angle.D, new Point(dCol.D, dRow.D), new Point(col.D, row.D));
    }

    public (List<double> Ch1YList, List<double> Ch2YList) GetCibList(List<HImage> image)
    {
        _algorithm.ChannelFineSamePositionPoint(image.Select(t => (HObject)t).ToList(), out var ch3SubCh1, out var ch3SubCh2, out var result);
        if (result == -1) ThrowHelper.ThrowArgumentException("Get Cib List Failed");

        return ([.. ch3SubCh1], [.. ch3SubCh2]);
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
}