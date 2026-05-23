using Core.Models.Helper;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Enums.Files;
using Semix.CoreLib;
using System.IO;
using Net.Utilities.Models.Geometries;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationReviewService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationReviewServiceMockImpl : ICalibrationReviewService
{
    private const int Width = 2448;
    private const int Height = 2048;

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<BitmapImage> GetBrightFieldImage()
    {
#pragma warning disable IDE0079
#pragma warning disable IDISP001

        var bitmapImage = BitmapImage.Random(Width, Height, 10);
        return SxExecuteRetHelper.CreateSuccess(bitmapImage);

#pragma warning restore IDISP001
#pragma warning restore IDE0079
    }

    public SxExecuteRet<byte[]> GetBrightFieldImageMemoryByteArray()
    {
        using var bitmapImage = BitmapImage.Random(Width, Height, 10);
        using var memorySteam = new MemoryStream();

        bitmapImage.Save(memorySteam, ImageTypeEnum.Bmp);

        return SxExecuteRetHelper.CreateSuccess(memorySteam.ToArray());
    }

    public SxExecuteRet<Size> GetBrightFieldImagePixelSize()
    {
        return SxExecuteRetHelper.CreateSuccess(new Size(Width, Height));
    }
}