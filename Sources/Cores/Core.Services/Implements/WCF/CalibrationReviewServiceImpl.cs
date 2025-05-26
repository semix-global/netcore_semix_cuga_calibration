using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Engine.Interface;
using HalconDotNet;
using Net.Utilities.Algorithm.Halcon.Helper;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
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

    public SxExecuteRet<HObject> GetBrightFieldImage()
    {
        var bytes = Invoke(() => Service!.GetReviewRawImage());
        var size = GetBrightFieldImagePixelSize();
        var channels = GetChannels();

        if (bytes.IsSuccess == false || size.IsSuccess == false || channels.IsSuccess == false) return SxExecuteRetHelper.CreateError(bytes.ErrorMsg, HalconHelper.EmptyHObject);

        var (width, height) = size.Anything;
        return SxExecuteRetHelper.CreateSuccess(HalconHelper.ImageRawBytesToHObject(bytes.Anything, width, height, channels.Anything));
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