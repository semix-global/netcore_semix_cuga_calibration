using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Services.Interfaces;
using Cuga.Interface.Calibration;
using HalconDotNet;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;

namespace Core.Services.Implements.GRPC;

[IOCAppService(ServiceType = typeof(ICalibrationReviewService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationReviewServiceImpl : BaseService<ICgCalibReviewService>, ICalibrationReviewService
{
    private Size? _pixel;
    private int? _channels;

    public SxExecuteRet<bool> Connect()
    {
        if (IsConnected) return SxExecuteRetHelper.CreateSuccess(true);

        return Invoke(() =>
        {
            var createService = CreateService();
            IsConnected = createService.IsSuccess;

            return createService;
        });
    }

    public SxExecuteRet<HImage> GetBrightFieldImage()
    {
        var bytes = Invoke(() => Service?.GetBrightFieldImageMemoryByteArray());
        var size = GetBrightFieldImagePixelSize();
        var channels = GetChannels();

        if (bytes.IsSuccess == false || size.IsSuccess == false || channels.IsSuccess == false) return SxExecuteRetHelper.CreateError(bytes.ErrorMsg, HalconFactory.EmptyHImage);

        var (width, height) = (SizeI)size.Anything;

#pragma warning disable IDE0079
#pragma warning disable IDISP004

        return SxExecuteRetHelper.CreateSuccess(HalconFactory.CreateImage(bytes.Anything, width, height, channels.Anything, channels.Anything * 8));

#pragma warning restore IDISP004
#pragma warning restore IDE0079
    }

    public SxExecuteRet<byte[]> GetBrightFieldImageMemoryByteArray()
    {
        return Invoke(() => Service?.GetBrightFieldImage());
    }

    public SxExecuteRet<Size> GetBrightFieldImagePixelSize()
    {
        if (_pixel is not null) return SxExecuteRetHelper.CreateSuccess(_pixel.Value);

        var sxExecuteRet = Invoke(() => Service?.GetBrightFieldImagePixelSize());
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

        var sxExecuteRet = Invoke(() => Service?.GetChannel());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, 0);

        _channels = sxExecuteRet.Anything;

        return SxExecuteRetHelper.CreateSuccess(_channels.Value);
    }

    #endregion 私有
}