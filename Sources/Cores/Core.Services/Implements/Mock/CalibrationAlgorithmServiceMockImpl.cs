using Core.Models.Enums.Algorithm;
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
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using System.IO;
using Rect = Net.Utilities.Models.Geometries.Rect;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationAlgorithmService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationAlgorithmServiceMockImpl(
    ILogger<CalibrationAlgorithmServiceImpl> logger,
    CalibrationSetting calibrationSetting,
    AffineTransformation affineTransformation) : ICalibrationAlgorithmService
{
    private static readonly Random Random = new();

    private readonly CalibrationAlgorithmServiceImpl _calibrationAlgorithmServiceImpl = new(logger, calibrationSetting, affineTransformation);
    private readonly Algorithm _algorithm = new();
    private readonly bool _isUseMock = true;

    public string Version => Algorithm.Version;

    public double GetQuality(HImage image)
    {
        return Random.Next(100, 1000);
    }

    public double GetDarkFieldQuality(HImage image)
    {
        return Random.Next(100, 1000);
    }

    public (double XQuality, double YQuality) GetXyQuality(HImage image)
    {
        return (Random.Next(100, 1000), Random.Next(100, 1000));
    }

    public (double MtfX, double MtfY) ModulationTransferFunction(HImage image, Rect roiRect)
    {
        return (Random.Next(100, 1000), Random.Next(100, 1000));
    }

    public (double Width, double Height) GetLightQuality(HImage image, Rect roiRect)
    {
        return (Random.Next(100, 1000), Random.Next(100, 1000));
    }

    public Size GetPixelSize(HImage image, Size standardMaskSquareSize, out HImage drawingImage, out double angle)
    {
        var pixelSize = new Size(Random.Next(1, 10), Random.Next(1, 10));
        drawingImage = image.Copy();
        angle = Random.NextDouble();
        return pixelSize;
    }

    public double GetYPixelSize(DarkFieldImageDTO image, double standardMaskSquareYSize)
    {
        return Random.NextDouble();
    }

    public bool TryGenerateTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, HImage image, string templateFilePath, Rect rect, out HImage templateImage)
    {
        if (_isUseMock)
        {
            templateImage = image.ToRoi(rect);
            templateFilePath = algorithmTemplateTypeEnum.ToFullFilePath(templateFilePath);

            switch (algorithmTemplateTypeEnum)
            {
                case AlgorithmTemplateTypeEnum.Sharpe:
                    templateImage.SaveSharpeTemplate(templateFilePath);
                    break;

                case AlgorithmTemplateTypeEnum.Ncc:
                    templateImage.SaveNccTemplate(templateFilePath);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(algorithmTemplateTypeEnum), algorithmTemplateTypeEnum, null);
            }

            return true;
        }

        return _calibrationAlgorithmServiceImpl.TryGenerateTemplate(algorithmTemplateTypeEnum, image, templateFilePath, rect, out templateImage);
    }

    public bool TryReadTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, string templateFilePath, out HTuple templateId)
    {
        if (_isUseMock)
        {
            templateId = HalconFactory.EmptyHTuple;

            var temp = algorithmTemplateTypeEnum.ToFullFilePath(templateFilePath);
            if (File.Exists(temp) == false) throw new FileNotFoundException(nameof(templateFilePath), temp);

            _algorithm.HReadModel(algorithmTemplateTypeEnum.ToAlgorithmTemplateType(), templateFilePath, out templateId);

            return true;
        }

        return _calibrationAlgorithmServiceImpl.TryReadTemplate(algorithmTemplateTypeEnum, templateFilePath, out templateId);
    }

    public bool TryCleanTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, HTuple templateId)
    {
        if (_isUseMock) _algorithm.HClearModel(algorithmTemplateTypeEnum.ToAlgorithmTemplateType(), templateId);

        return _calibrationAlgorithmServiceImpl.TryCleanTemplate(algorithmTemplateTypeEnum, templateId);
    }

    public bool TryTemplateMatchToOffset(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, HImage image, HTuple templateId, out Point markPoint, out Point offsetPoint, out double score, out double angle)
    {
        if (_isUseMock)
        {
            score = Random.NextDouble() * 10;
            angle = Random.Next(1, 10);
            offsetPoint = new Point(Random.Next(1, 10), Random.Next(1, 10));

            var size = image.GetSize();
            markPoint = (Point)(size / 2d) + new Vector(offsetPoint.X, -offsetPoint.Y);
            return true;
        }

        return _calibrationAlgorithmServiceImpl.TryTemplateMatchToOffset(algorithmTemplateTypeEnum, image, templateId, out markPoint, out offsetPoint, out score, out angle);
    }

    public bool TryGenerateProjectionTemplate(HImage image, string templateFilePath, out HImage templateImage)
    {
        templateImage = image.Copy();
        return true;
    }

    public bool TryReadProjectionTemplate(string templateFilePath, out HTuple templateXId, out HTuple templateYId)
    {
        templateXId = HalconFactory.EmptyHTuple;
        templateYId = HalconFactory.EmptyHTuple;

        return true;
    }

    public bool TryCleanProjectionTemplate(HTuple templateXId, HTuple templateYId)
    {
        return true;
    }

    public bool TryProjectionTemplateMatchToOffset(HImage image, HTuple templateXId, HTuple templateYId, out Point markPoint, out Point offsetPoint)
    {
        offsetPoint = new Point(Random.Next(1, 10), Random.Next(1, 10));

        var size = image.GetSize();
        markPoint = (Point)(size / 2d) + new Vector(offsetPoint.X, -offsetPoint.Y);

        return true;
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

        return (RawImageFactory.CreateImage(rawBytes), matrix);
    }

    public (HImage Image, short[,] Matrix, byte[] RawBytes) ToHorizontalFlipImageInfo(byte[] rawBytes)
    {
        var (matrix, horizontalFlipRawBytes, _) = RawImageFactory.ToHorizontalFlipMatrix(rawBytes);

        return (RawImageFactory.CreateImage(horizontalFlipRawBytes), matrix, horizontalFlipRawBytes);
    }

    public (List<string> DatAvg, List<string> Data) GetPmtGain(Dictionary<int, List<int>> dicPmtData, int lineValue, double minValue, double maxValue)
    {
        return ([], []);
    }

    public (HImage drawingImage, double CenterChannelLightDiameter, double CenterChannelHorizontalDegree, Point CenterChannelLightCenterPosition, Point ReflectedLightCenterPosition) GetOpticsObjectiveYAngleResult(HImage hazeImage, HImage shinyWaferImage, double rotateAngle)
    {
        var hazeImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\fftCh3HazeTestImg.jpg");
        var shinyImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\fftCh3ShinyTestImg.jpg");

        using var hazeImg = HalconFactory.CreateImage(hazeImagePath);
        using var shinyImg = HalconFactory.CreateImage(shinyImagePath);
        _algorithm.CalculateTwoRegionCenter(hazeImg, shinyImg, out var resultImage, rotateAngle, out var diameter, out var angle, out var dRow, out var dCol, out var row, out var col);

        var drawingImage = new HImage(resultImage);

        return (drawingImage, diameter.D, angle.D, new Point(dCol.D, dRow.D), new Point(col.D, row.D));
    }

    public (List<double> Ch1YList, List<double> Ch2YList) GetCibList(List<HImage> image)
    {
        var ch1YList = new List<double>();
        var ch2YList = new List<double>();
        for (var i = 0; i < 3; i++)
        {
            var listY1 = Random.Next(1, 6);
            var listY2 = Random.Next(1, 6);
            ch1YList.Add(listY1);
            ch2YList.Add(listY2);
        }

        return (ch1YList, ch2YList);
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
        return new Point(Random.Next(1, 10), Random.Next(1, 10));
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
}