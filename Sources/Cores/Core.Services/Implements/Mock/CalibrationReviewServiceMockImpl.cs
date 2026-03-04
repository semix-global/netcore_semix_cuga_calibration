using Core.Models.Helper;
using Core.Services.Interfaces;
using Core.Utilities;
using HalconDotNet;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Models.Enums.Files;
using Semix.CoreLib;
using System.IO;
using Size = Net.Utilities.Models.Geometries.Size;

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

    public SxExecuteRet<HImage> GetBrightFieldImage()
    {
        using var bitmapImage = BitmapImageGenerate.GenerateRandomImage(Width, Height, 10, Random.Shared);

#pragma warning disable IDE0079
#pragma warning disable IDISP004
        
        return SxExecuteRetHelper.CreateSuccess(bitmapImage.ToHImage());
        
#pragma warning restore IDISP004
#pragma warning restore IDE0079
    }

    public SxExecuteRet<byte[]> GetBrightFieldImageMemoryByteArray()
    {
        using var bitmapImage = BitmapImageGenerate.GenerateRandomImage(Width, Height, 10, Random.Shared);
        using var memorySteam = new MemoryStream();

        bitmapImage.Save(memorySteam, ImageTypeEnum.Bmp);

        return SxExecuteRetHelper.CreateSuccess(memorySteam.ToArray());
    }

    public SxExecuteRet<Size> GetBrightFieldImagePixelSize()
    {
        return SxExecuteRetHelper.CreateSuccess(new Size(Width, Height));
    }
}