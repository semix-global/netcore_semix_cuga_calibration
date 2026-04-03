using Core.Models.Helper;
using Core.Models.Models.Common.Fourier;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Core.Utilities;
using Cuga.Data.DataStruct.Optics;
using Cuga.Interface.Diagnosis;
using HalconDotNet;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Primitives.Enums.Medias.Imaging;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Enums.Files;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using System.IO;
using System.Runtime.CompilerServices;
using FFCH = Core.Models.Models.Common.Fourier.FFCH;

namespace Core.Services.Implements.GRPC;

[IOCAppService(ServiceType = typeof(ICalibrationFourierService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationFourierServiceImpl : BaseService<ICgDiagFourierOpticsService>, ICalibrationFourierService
{
    private const int Width = 2448;
    private const int Height = 2048;

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

    public SxExecuteRet<HImage> GetFourierImage(int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<byte[]> GetFFReviewImgForTrigger(int id, ProductivityInformation productivityInformation, double level, Point pos, int width = 800)  
    {  
        var imageInfo = new ImageInfo(
            width,
            width,
            PixelFormatEnum.Gray8,
            AlphaFormatEnum.Opaque
        );

        byte[] randomPixels = new byte[imageInfo.BytesSize];
        Random.Shared.NextBytes(randomPixels);
        using var bitmapImage = new BitmapImage(randomPixels, isCopy: true);
        using var memoryStream = new MemoryStream();
        bitmapImage.Save(memoryStream, ImageTypeEnum.Bmp);

        return SxExecuteRetHelper.CreateSuccess(memoryStream.ToArray());
    }

    public SxExecuteRet<C2MFFRangeModel> GetFourierConfig()
    {
        var c2MFFRangeModel = new C2MFFRangeModel();

        return SxExecuteRetHelper.CreateSuccess(c2MFFRangeModel);
    }

    public SxExecuteRet<bool> FF_Move_CH12(FFCH channelId, List<(int rodnumber, double rodpos)> rodpostions)
    { 
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> FF_Move_CH3X(int rpos, double lpos, double ppos)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> FF_Move_CH3Y(int rpos, double lpos)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetFFHome(FFCH ch)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetFFRACT(CgFFCHEnum ch)
    {
        return SxExecuteRetHelper.CreateSuccess(1.0);
    }

    public SxExecuteRet<double> GetFFLACT(CgFFCHEnum ch)
    {
        return SxExecuteRetHelper.CreateSuccess(1.0);
    }

    public SxExecuteRet<double> GetFFPACT(CgFFCHEnum ch)
    {
        return SxExecuteRetHelper.CreateSuccess(1.0);
    }

    public SxExecuteRet<bool> SetFFRPOS_CH3(FFCH ch, double pos)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetFFLPOS_CH3(FFCH ch, double pos)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetFFPPOS_CH3(FFCH ch, double pos)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }
}