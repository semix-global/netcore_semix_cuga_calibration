using Core.Models.Helper;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Engine.Interface;
using HalconDotNet;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Semix.CoreLib;
using SkiaSharp;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationFourierService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationFourierServiceImpl : BaseService<ICgCalibrationService>, ICalibrationFourierService
{
    public SxExecuteRet<bool> Connect()
    {
        if (IsConnected) return SxExecuteRetHelper.CreateSuccess(true);

        return Invoke(() =>
        {
            var ep = new SxWcfEndPoint("127.0.0.1", 80, CgInernalAddr.CalAddr);
            var createService = CreateService(ep);
            IsConnected = createService.IsSuccess;
            return createService;
        }, false);
    }

    public SxExecuteRet<HImage> GetFourierImage(int channelId)
    {
        var sxExecuteRet = Invoke(() => Service!.GetFFReviewImg(channelId - 1));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<HImage>(sxExecuteRet.ErrorMsg, HalconFactory.EmptyHImage);

        using var skBitmap = SKBitmap.Decode(sxExecuteRet.Anything);
        using var bitmapImage = new BitmapImage(ImageInfoFactory.Create(skBitmap.Info), skBitmap.GetPixels());

        return SxExecuteRetHelper.CreateSuccess(bitmapImage.ToHImage());
    }
}