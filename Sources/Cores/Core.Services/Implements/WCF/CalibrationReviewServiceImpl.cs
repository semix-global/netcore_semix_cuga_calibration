using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Engine.Interface;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationReviewService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationReviewServiceImpl : BaseService<ICgCalibrationService>, ICalibrationReviewService
{
    private Size? _pixel;
    private int? _channels;

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

    public unsafe SxExecuteRet<BitmapImage> GetBrightFieldImage()
    {
        var bytes = Invoke(() => Service!.GetReviewRawImage());
        var size = GetBrightFieldImagePixelSize();
        var channels = GetChannels();

        using var defaultBitmapImage = BitmapImage.Random(2448, 2048, 10);
        if (bytes.IsSuccess == false || size.IsSuccess == false || channels.IsSuccess == false) return SxExecuteRetHelper.CreateError(bytes.ErrorMsg, defaultBitmapImage);

        var (width, height) = (SizeI)size.Anything;

        var imageInfo = ImageInfoFactory.Create(width, height, channels.Anything, channels.Anything * 8);

#pragma warning disable IDE0079
#pragma warning disable IDISP001

        fixed (byte* ptr = bytes.Anything)
        {
            var bitmapImage = new BitmapImage(imageInfo, (IntPtr)ptr);
            return SxExecuteRetHelper.CreateSuccess(bitmapImage);
        }
#pragma warning restore IDISP001
#pragma warning restore IDE0079
    }

    public SxExecuteRet<byte[]> GetBrightFieldImageMemoryByteArray()
    {
        return Invoke(() => Service!.GetReviewImage());
    }

    public SxExecuteRet<Size> GetBrightFieldImagePixelSize()
    {
        if (_pixel is not null) return SxExecuteRetHelper.CreateSuccess(_pixel.Value);

        var sxExecuteRet = Invoke(() => Service!.GetPixel());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, Size.Empty);

        _pixel = sxExecuteRet.Anything.ToSize();

        return SxExecuteRetHelper.CreateSuccess(_pixel.Value);
    }

    #region 私有

    /// <summary>
    /// 相机通道数
    /// </summary>
    /// <returns>是否成功</returns>
    private SxExecuteRet<int> GetChannels()
    {
        if (_channels is not null) return SxExecuteRetHelper.CreateSuccess(_channels.Value);

        var sxExecuteRet = Invoke(() => Service!.GetChannels());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, 0);

        _channels = sxExecuteRet.Anything;

        return SxExecuteRetHelper.CreateSuccess(_channels.Value);
    }

    #endregion 私有
}