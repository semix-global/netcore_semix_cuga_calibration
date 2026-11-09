using algocv_sharp;
using CommunityToolkit.Diagnostics;
using Core.Algorithm.Helper;
using Core.Models;
using Core.Models.Models.Common.StageMap;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using HalconDotNet;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Net.Utilities.Algorithms.AlgoCSharp;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Nlog.Entities.HtmlElements;
using Net.Utilities.Nlog.Extensions;
using Net.Utilities.WPF.MVVM;
using System.IO;
using Constants = Net.Utilities.Models.Constants;
using Rect = Net.Utilities.Models.Geometries.Rect;

namespace Core.Services.Implements;

[IOCAppService(ServiceType = typeof(ICalibrationAlgorithmService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed partial class CalibrationAlgorithmServiceImpl(
    ILogger<CalibrationAlgorithmServiceImpl> logger,
    CalibrationSetting calibrationSetting,
    AffineTransformation affineTransformation) : ICalibrationAlgorithmService
{
    private bool _isAlgorithmEngineInitialized = false;
    private readonly HAlgorithm.Algorithm _algorithm = new();

    public void InitialAlgorithmEngine()
    {
        if (_isAlgorithmEngineInitialized) return;

        var options = HostApplication.GetRequiredService<IOptions<ApplicationSetting>>();
        AlgoCv.InitAlgoCv(
            Path.Combine(options.Value.AlgorithmLogDirectory, $"{Constants.DateTimeFormat}"),
            options.Value.AlgorithmLogLevel,
            0f,
            0f);
        AlgoCv.CheckStructSize();
        _isAlgorithmEngineInitialized = true;
    }

    public double GetQuality(BitmapImage image, Guid guid, AlgorithmEngineTypeEnum algorithmEngineTypeEnum)
    {
        try
        {
            return algorithmEngineTypeEnum switch
            {
                AlgorithmEngineTypeEnum.HAlgorithm => GetHAlgorithemQuality(image),
                AlgorithmEngineTypeEnum.AlgoCSharp => GetAlgoCVSharpQuality(image),
                _ => ThrowHelper.ThrowNotSupportedException<double>()
            };
        }
        catch (Exception ex)
        {
            var errorImagePath = AlgorithmLogHelper.SaveErrorImage(image, guid, nameof(GetQuality));
            AlgorithmLogHelper.LogAlgorithmError(logger, ex, nameof(GetQuality), guid, errorImagePath);
            throw;
        }
    }

    private double GetHAlgorithemQuality(BitmapImage image)
    {
        using var hImage = image.ToHImage();
        _algorithm.LaplaceDefinition(hImage, out var meanTuple);
        using var _ = meanTuple;
        return meanTuple.D;
    }

    private double GetAlgoCVSharpQuality(BitmapImage image)
    {
        // Focus 评价默认 tenengrad 模式；BF 图为常规格式
        using var algoImage = image.ToAlgoCVImage();
        return FocusEngine.EvaluateFocus(algoImage, "tenengrad");
    }

    public double GetDarkFieldQuality(BitmapImage image, Guid guid)
    {
        try
        {
            using var hImage = image.ToHImage();
            _algorithm.DarkLaplaceDefinition(hImage, out var meanTuple);
            using var _ = meanTuple;

            return meanTuple.D;
        }
        catch (Exception ex)
        {
            var errorImagePath = AlgorithmLogHelper.SaveErrorImage(image, guid, nameof(GetDarkFieldQuality));
            AlgorithmLogHelper.LogAlgorithmError(logger, ex, nameof(GetDarkFieldQuality), guid, errorImagePath);
            throw;
        }
    }

    public (double XQuality, double YQuality) GetXyQuality(BitmapImage image, Guid guid)
    {
        try
        {
            using var hImage = image.ToHImage();
            _algorithm.DarkDefinition(hImage, out var meanTupleY, out var meanTupleX);

            using var _1 = meanTupleX;
            using var _2 = meanTupleY;

            return (meanTupleX.D, meanTupleY.D);
        }
        catch (Exception ex)
        {
            var errorImagePath = AlgorithmLogHelper.SaveErrorImage(image, guid, nameof(GetXyQuality));
            AlgorithmLogHelper.LogAlgorithmError(logger, ex, nameof(GetXyQuality), guid, errorImagePath);
            throw;
        }
    }

    public (double MtfX, double MtfY) ModulationTransferFunction(BitmapImage image, Rect roiRect, Guid guid)
    {
        try
        {
            using var hImage = image.ToHImage();
            using var roiImage = hImage.ToROI(roiRect);

            _algorithm.WuMTF(roiImage, out var mtfX, out var mtfY);

            using var _1 = mtfX;
            using var _2 = mtfY;

            return (mtfX.D, mtfY.D);
        }
        catch (Exception ex)
        {
            var errorImagePath = AlgorithmLogHelper.SaveErrorImage(image, guid, nameof(ModulationTransferFunction));
            AlgorithmLogHelper.LogAlgorithmError(logger, ex, nameof(ModulationTransferFunction), guid, errorImagePath);
            throw;
        }
    }

    public (BitmapImage drawingImage, double CenterChannelLightDiameter, double CenterChannelHorizontalDegree, Point CenterChannelLightCenterPosition, Point ReflectedLightCenterPosition) GetOpticsObjectiveYAngleResult(BitmapImage hazeImage, BitmapImage shinyWaferImage, double rotateAngle, Guid guid)
    {
        try
        {
            using var hHazeImage = hazeImage.ToHImage();
            using var hShinyWaferImage = shinyWaferImage.ToHImage();
            _algorithm.CalculateTwoRegionCenter(hHazeImage, hShinyWaferImage, out var resultImage, rotateAngle, out var diameter, out var angle, out var dRow, out var dCol, out var row, out var col);

            using var drawingHImage = new HImage(resultImage);
            var drawingImage = drawingHImage.ToBitmapImage();

            return (drawingImage, diameter.D, angle.D, new Point(dCol.D, dRow.D), new Point(col.D, row.D));
        }
        catch (Exception ex)
        {
            var hazeErrorImagePath = AlgorithmLogHelper.SaveErrorImage(hazeImage, guid, $"{nameof(GetOpticsObjectiveYAngleResult)}_Haze");
            var shinyErrorImagePath = AlgorithmLogHelper.SaveErrorImage(shinyWaferImage, guid, $"{nameof(GetOpticsObjectiveYAngleResult)}_Shiny");
            logger.LogHtmlError(ex, $"{nameof(GetOpticsObjectiveYAngleResult)} Error", HtmlHeaderLevelEnum.Header3,
                new HtmlBullet(new
                {
                    HazeErrorImage = new HtmlImage(hazeErrorImagePath),
                    HazeImagePath = hazeErrorImagePath,
                    ShinyErrorImage = new HtmlImage(shinyErrorImagePath),
                    ShinyImagePath = shinyErrorImagePath,
                    ErrorMessage = ex.Message
                }), guid.LoggingHtml());
            throw;
        }
    }

    public (double StartImageYPixel, double EndImageYPixel) GetOpticsROOSResult(BitmapImage image, out HImage drawImage)
    {
        using var hImage = image.ToHImage();
        _algorithm.FindLineYPosition(hImage, 1, out var resultTuple, out var drawImageObj);

        drawImage = new HImage(drawImageObj);
        var size = hImage.GetSize();

        return resultTuple.Length switch
        {
            0 => (0, size.Height),
            1 => resultTuple[0].D <= size.Height / 2d ? (resultTuple[0].D, size.Height) : (0, resultTuple[0].D),
            _ => (resultTuple[0].D, resultTuple[1].D)
        };
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

    public double[] GetImageGrayYProjectionsPixels(BitmapImage image, Guid guid)
    {
        try
        {
            using var hImage = image.ToHImage();
            _algorithm.LightSpot(hImage, out var yValue);

            return yValue.ToDArr();
        }
        catch (Exception ex)
        {
            var errorImagePath = AlgorithmLogHelper.SaveErrorImage(image, guid, nameof(GetImageGrayYProjectionsPixels));
            AlgorithmLogHelper.LogAlgorithmError(logger, ex, nameof(GetImageGrayYProjectionsPixels), guid, errorImagePath);
            throw;
        }
    }

    public (Point CenterPosition, double Radius) FitCircle(IReadOnlyList<Point> points)
    {
        _algorithm.FindCircle(points.Select(t => t.X).ToList(), points.Select(t => t.Y).ToList(), out var yValue, out var xValue, out var radius);

        return (new Point(xValue.D, yValue.D), radius.D);
    }
}