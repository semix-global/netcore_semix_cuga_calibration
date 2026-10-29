using Core.Models.Enums.Algorithm;
using Core.Models.Extensions;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using HalconDotNet;
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
    CalibrationSetting calibrationSetting) : ICalibrationAlgorithmService
{
    private readonly CalibrationAlgorithmServiceImpl _calibrationAlgorithmServiceImpl = new(HostApplication.GetRequiredService<ILogger<CalibrationAlgorithmServiceImpl>>(), calibrationSetting);

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

    public double[] GetImageGrayYProjectionsPixels(BitmapImage image, Guid guid)
    {
        return _calibrationAlgorithmServiceImpl.GetImageGrayYProjectionsPixels(image, guid);
    }

    public (Point CenterPosition, double Radius) FitCircle(IReadOnlyList<Point> points)
    {
        return _calibrationAlgorithmServiceImpl.FitCircle(points);
    }
}
