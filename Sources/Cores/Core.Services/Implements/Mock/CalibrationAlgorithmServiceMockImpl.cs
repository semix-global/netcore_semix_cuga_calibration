using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using HalconDotNet;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.MVVM;
using System.IO;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationAlgorithmService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed partial class CalibrationAlgorithmServiceMockImpl(
    CalibrationSetting calibrationSetting,
    AffineTransformation affineTransformation) : ICalibrationAlgorithmService
{
    private readonly CalibrationAlgorithmServiceImpl _calibrationAlgorithmServiceImpl = new(HostApplication.GetRequiredService<ILogger<CalibrationAlgorithmServiceImpl>>(), calibrationSetting, affineTransformation);

    private readonly string _hazeImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\fftCh3HazeTestImg.jpg");
    private readonly string _shinyImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\fftCh3ShinyTestImg.jpg");

    public bool IsUseMock { get; set; } = true;

    public void InitialAlgorithmEngine()
    {
        _calibrationAlgorithmServiceImpl.InitialAlgorithmEngine();
    }

    public double GetQuality(BitmapImage image, Guid guid, AlgorithmEngineTypeEnum algorithmEngineTypeEnum)
    {
        return Random.Shared.Next(100, 1000);
    }

    public double GetDarkFieldQuality(BitmapImage image, Guid guid)
    {
        return Random.Shared.Next(100, 1000);
    }

    public (double XQuality, double YQuality) GetXyQuality(BitmapImage image, Guid guid)
    {
        return (Random.Shared.Next(100, 1000), Random.Shared.Next(100, 1000));
    }

    public (double MtfX, double MtfY) ModulationTransferFunction(BitmapImage image, Rect roiRect, Guid guid)
    {
        return (Random.Shared.Next(100, 1000), Random.Shared.Next(100, 1000));
    }

    public (BitmapImage drawingImage, double CenterChannelLightDiameter, double CenterChannelHorizontalDegree, Point CenterChannelLightCenterPosition, Point ReflectedLightCenterPosition) GetOpticsObjectiveYAngleResult(BitmapImage hazeImage, BitmapImage shinyWaferImage, double rotateAngle, Guid guid)
    {
        using var hazeImageTemp = new BitmapImage(_hazeImagePath);
        using var shinyWaferImageTemp = new BitmapImage(_shinyImagePath);

        return _calibrationAlgorithmServiceImpl.GetOpticsObjectiveYAngleResult(hazeImageTemp, shinyWaferImage, rotateAngle, guid);
    }

    public (double StartImageYPixel, double EndImageYPixel) GetOpticsROOSResult(BitmapImage image, out HImage drawImage)
    {
        var result = _calibrationAlgorithmServiceImpl.GetOpticsROOSResult(image, out var hImage);
        using var _ = hImage;
        drawImage = new HImage(hImage);
        return (result.StartImageYPixel, result.EndImageYPixel);
        // drawImage = image.ToHImage().CopyImage();
        //
        // return (Random.Shared.Next(0, 500), Random.Shared.Next(500, 1000));
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

    public bool CalculateChuckStageMapError(StageMapDto stageMapDto,
        bool isXOnlyGantryError,
        Guid guid,
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
            guid,
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

    public StageMapDto ExpandStageMapDto(StageMapDto baseStageMap, StageMapDto mergeStageMap, Guid htmlLogUniqueId)
    {
        return affineTransformation.ExpandStageMapDto(baseStageMap, mergeStageMap, htmlLogUniqueId);
    }

    public double[] GetImageGrayYProjectionsPixels(BitmapImage image, Guid guid)
    {
        return _calibrationAlgorithmServiceImpl.GetImageGrayYProjectionsPixels(image, guid);
    }

    public (Point CenterPosition, double Radius) FitCircle(IReadOnlyList<Point> points)
    {
        return _calibrationAlgorithmServiceImpl.FitCircle(points);
    }
}