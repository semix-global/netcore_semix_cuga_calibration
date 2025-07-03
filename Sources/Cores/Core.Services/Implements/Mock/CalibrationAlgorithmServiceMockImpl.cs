using Core.Models.Enums.Algorithm;
using Core.Models.Extensions;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.StageMap;
using Core.Services.Interfaces;
using HalconDotNet;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using System.IO;
using Rect = Net.Utilities.Models.Geometries.Rect;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationAlgorithmService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationAlgorithmServiceMockImpl(AffineTransformation affineTransformation) : ICalibrationAlgorithmService
{
    private static readonly Random Random = new();

    public string Version => HAlgorithm.Algorithm.Version;

    public double GetQuality(HObject image)
    {
        return Random.Next(100, 1000);
    }

    public double GetDarkFieldQuality(HObject image)
    {
        return Random.Next(100, 1000);
    }

    public (double XQuality, double YQuality) GetXyQuality(HObject image)
    {
        return (Random.Next(100, 1000), Random.Next(100, 1000));
    }

    public (double MtfX, double MtfY) ModulationTransferFunction(HObject image, Rect roiRect)
    {
        return (Random.Next(100, 1000), Random.Next(100, 1000));
    }

    public Size GetPixelSize(HObject image, Size standardMaskSquareSize, out HObject drawingImage, out double angle)
    {
        var pixelSize = new Size(Random.Next(1, 10), Random.Next(1, 10));
        drawingImage = HalconHelper.Copy(image);
        angle = Random.NextDouble();
        return pixelSize;
    }

    public double GetYPixelSize(DarkFieldImageDto image, double standardMaskSquareYSize)
    {
        return Random.NextDouble();
    }

    public bool TryGenerateTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, HObject image, string templateFilePath, Rect rect, out HObject templateImage)
    {
        templateImage = HalconHelper.ToRoi(image, rect);
        templateFilePath = algorithmTemplateTypeEnum.ToFullFilePath(templateFilePath);

        switch (algorithmTemplateTypeEnum)
        {
            case AlgorithmTemplateTypeEnum.Sharpe:
                HalconHelper.SaveSharpeTemplate(templateImage, templateFilePath);
                break;

            case AlgorithmTemplateTypeEnum.Ncc:
                HalconHelper.SaveNccTemplate(templateImage, templateFilePath);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(algorithmTemplateTypeEnum), algorithmTemplateTypeEnum, null);
        }

        return true;
    }

    public bool TryReadTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, string templateFilePath, out HTuple templateId)
    {
        templateFilePath = algorithmTemplateTypeEnum.ToFullFilePath(templateFilePath);
        if (File.Exists(templateFilePath) == false) throw new FileNotFoundException(nameof(templateFilePath), templateFilePath);

        templateId = algorithmTemplateTypeEnum switch
        {
            AlgorithmTemplateTypeEnum.Sharpe => HalconHelper.ReadSharpeTemplate(templateFilePath),
            AlgorithmTemplateTypeEnum.Ncc => HalconHelper.ReadNccTemplate(templateFilePath),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithmTemplateTypeEnum), algorithmTemplateTypeEnum, null)
        };

        return true;
    }

    public bool TryCleanTemplate(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, HTuple templateId)
    {
        switch (algorithmTemplateTypeEnum)
        {
            case AlgorithmTemplateTypeEnum.Sharpe:
                HalconHelper.CleanSharpeTemplate(templateId);
                break;

            case AlgorithmTemplateTypeEnum.Ncc:
                HalconHelper.CleanNccTemplate(templateId);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(algorithmTemplateTypeEnum), algorithmTemplateTypeEnum, null);
        }

        return true;
    }

    public bool TryTemplateMatchToOffset(AlgorithmTemplateTypeEnum algorithmTemplateTypeEnum, HObject image, HTuple templateId, out Point markPoint, out Point offsetPoint, out double score, out double angle)
    {
        score = Random.NextDouble() * 10;
        angle = Random.Next(1, 10);
        offsetPoint = new Point(Random.Next(1, 10), Random.Next(1, 10));

        var size = HalconHelper.GetSize(image);
        markPoint = (Point)(size / 2d) + new Vector(offsetPoint.X, -offsetPoint.Y);

        return true;
    }

    public bool TryGenerateProjectionTemplate(HObject image, string templateFilePath, out HObject templateImage)
    {
        templateImage = HalconHelper.Copy(image);
        return true;
    }

    public bool TryReadProjectionTemplate(string templateFilePath, out HTuple templateXId, out HTuple templateYId)
    {
        templateXId = HalconHelper.EmptyHTuple;
        templateYId = HalconHelper.EmptyHTuple;

        return true;
    }

    public bool TryCleanProjectionTemplate(HTuple templateXId, HTuple templateYId)
    {
        return true;
    }

    public bool TryProjectionTemplateMatchToOffset(HObject image, HTuple templateXId, HTuple templateYId, out Point markPoint, out Point offsetPoint)
    {
        offsetPoint = new Point(Random.Next(1, 10), Random.Next(1, 10));

        var size = HalconHelper.GetSize(image);
        markPoint = (Point)(size / 2d) + new Vector(offsetPoint.X, -offsetPoint.Y);

        return true;
    }

    public (Size Size, int BodyBytesStartIndex, int BodyBytesLength) GetSize(byte[] rawBytes)
    {
        return RawImageHelper.GetSize(rawBytes);
    }

    public byte[] ToRawBytes(byte[] bodyBytes, Size size)
    {
        return RawImageHelper.BodyAddHeaderFooter(bodyBytes, size);
    }

    public (HObject Image, short[,] Matrix) ToImageInfo(byte[] rawBytes)
    {
        var (matrix, _) = RawImageHelper.ToMatrix(rawBytes);

        return (RawImageHelper.ToHObject(rawBytes), matrix);
    }

    public (HObject Image, short[,] Matrix, byte[] RawBytes) ToHorizontalFlipImageInfo(byte[] rawBytes)
    {
        var (matrix, horizontalFlipRawBytes, _) = RawImageHelper.ToHorizontalFlipMatrix(rawBytes);

        return (RawImageHelper.ToHObject(horizontalFlipRawBytes), matrix, horizontalFlipRawBytes);
    }

    public (List<string> DatAvg, List<string> Data) GetPmtGain(Dictionary<int, List<int>> dicPmtData, int lineValue, double minValue, double maxValue)
    {
        return ([], []);
    }

    public (List<double> Ch1YList, List<double> Ch2YList) GetCibList(List<HObject> image)
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
                htmlLogUniqueId,
                calculateContainRowMinCout: calculateContainRowMinCount,
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
        catch (Exception ex)
        {
            return false;
        }
    }

    public StageMapDto ExpandStageMapDto(StageMapDto baseStageMap, StageMapDto mergeStageMap, Guid htmlLogUniqueId)
    {
        return affineTransformation.ExpandStageMapDto(baseStageMap, mergeStageMap, htmlLogUniqueId);
    }
}