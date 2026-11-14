using Core.Models.Helper;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Enums.Files;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using System.IO;
using Net.Utilities.Graphics.Algorithms.Halcon;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationReviewService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationReviewServiceMockImpl : ICalibrationReviewService
{
    private const int Width = 2448;
    private const int Height = 2048;

    public string? SimulatorImageFilePath { get; set; }

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<BitmapImage> GetBrightFieldImage()
    {
#pragma warning disable IDE0079
#pragma warning disable IDISP001

        var bitmapImage = CreateImage();

        return SxExecuteRetHelper.CreateSuccess(bitmapImage);

#pragma warning restore IDISP001
#pragma warning restore IDE0079
    }

    public SxExecuteRet<byte[]> GetBrightFieldImageMemoryByteArray()
    {
        using var bitmapImage = CreateImage();
        using var memorySteam = new MemoryStream();

        bitmapImage.Save(memorySteam, ImageTypeEnum.Bmp);

        return SxExecuteRetHelper.CreateSuccess(memorySteam.ToArray());
    }

    public SxExecuteRet<Size> GetBrightFieldImagePixelSize()
    {
        var filePath = SimulatorImageFilePath ?? string.Empty;
        if (File.Exists(filePath) == false) return SxExecuteRetHelper.CreateSuccess(new Size(Width, Height));

        using var image = BitmapHelper.OpenImage(filePath);

        return SxExecuteRetHelper.CreateSuccess(new Size(image.Width, image.Height));
    }

    public SxExecuteRet<Size> GetDefaultPixelSize() => SxExecuteRetHelper.CreateSuccess(new Size(3.45, 3.45));

    private BitmapImage CreateImage()
    {
        var filePath = SimulatorImageFilePath ?? string.Empty;

        return File.Exists(filePath)
            ? BitmapHelper.OpenImage(filePath)
            : BitmapImage.Random(Width, Height, 10);
    }
}