using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.Fourier;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Data.DataStruct.Optics;
using Cuga.Engine.Interface;
using HalconDotNet;
using Humanizer;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Algorithms.Halcon;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using System.ServiceModel;
using System.Threading.Channels;
using static Humanizer.On;
using C2MFFRangeModel = Core.Models.Models.Common.Fourier.C2MFFRangeModel;
using FFCH = Core.Models.Models.Common.Fourier.FFCH;

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
        var sxExecuteRet = Invoke(() => Service!.GetFFReviewImg(channelId));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<HImage>(sxExecuteRet.ErrorMsg, HalconFactory.EmptyHImage);

        using var bitmapImage = new BitmapImage(sxExecuteRet.Anything);

        return SxExecuteRetHelper.CreateSuccess(bitmapImage.ToHImage());
    }

    public SxExecuteRet<byte[]> GetFFReviewImgForTrigger(int id, ProductivityInformation productivityInformation,  double level, Point pos, int width = 800)
    {
        // 类型转换：Core.Models.Models.Common.SxNew.SxOpticsParam -> Semix.WcfTransfer.DTO.SxOpticsParam
        var wcfParam = new Semix.WcfTransfer.DTO.SxOpticsParam
        {      
            Magnification = productivityInformation.AdaptTo().Mag,    
            Speed = productivityInformation.AdaptTo().Speed,
            NIOI = productivityInformation.OpticsIlluminationModeEnum.ToSxNIOIEnum(),
            LightLevelUnit = level
        };

        var sxExecuteRet = Invoke(() => Service!.GetFFReviewImgForTrigger(id, wcfParam, UtilitiesPointExtension.ToSxPointD(pos), width));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<byte[]>(sxExecuteRet.ErrorMsg, Array.Empty<byte>());

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<C2MFFRangeModel> GetFourierConfig()
    {
        var sxExecuteRet = Invoke(() => Service!.FF_GetConfig());
        var c2MFFRangeModel = new C2MFFRangeModel();
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<C2MFFRangeModel>(sxExecuteRet.ErrorMsg, c2MFFRangeModel);
        var dto = sxExecuteRet.Anything;
        var model = new C2MFFRangeModel
        {
            CH12MaxPOS = dto.CH12MaxPOS,
            CH12MinPOS = dto.CH12MinPOS,
            CH3_X_LPOSMax = dto.CH3_X_LPOSMax,
            CH3_X_LPOSMin = dto.CH3_X_LPOSMin,
            CH3_X_PPOSMax = dto.CH3_X_PPOSMax,
            CH3_X_PPOSMin = dto.CH3_X_PPOSMin,
            CH3_Y_LPOSMax = dto.CH3_Y_LPOSMax,
            CH3_Y_LPOSMin = dto.CH3_Y_LPOSMin,
            RodNum = dto.RodNum
        };
        return SxExecuteRetHelper.CreateSuccess(model);
    }

    public SxExecuteRet<bool> FF_Move_CH12(FFCH channelId,List<(int rodnumber,double rodpos)> rodpostions)
    {
        // 枚举跨命名空间转换
        var wcfChannelId = (Semix.WcfTransfer.DTO.FFCH)channelId;
        var sxExecuteRet = Invoke(() => Service!.FF_Move_CH12(wcfChannelId,rodpostions));
        
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<bool>(sxExecuteRet.ErrorMsg, false);
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> FF_Move_CH3X(int rpos, double lpos, double ppos)
    {
        var sxExecuteRet = Invoke(() => Service!.FF_Move_CH3X(rpos,lpos,ppos));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<bool>(sxExecuteRet.ErrorMsg, false);
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> FF_Move_CH3Y(int rpos, double lpos)
    {
        var sxExecuteRet = Invoke(() => Service!.FF_Move_CH3Y(rpos, lpos));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<bool>(sxExecuteRet.ErrorMsg, false);
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetFFHome(FFCH ch)
    {
        var sxExecuteRet = Invoke(() => Service!.SetFFHome((Semix.WcfTransfer.DTO.FFCH)ch));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<bool>(sxExecuteRet.ErrorMsg, false);
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetFFRACT(CgFFCHEnum ch)
    {
        var sxExecuteRet = Invoke(() => Service!.FF_GetRACT((Semix.WcfTransfer.DTO.FFCH)ch));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<double>(sxExecuteRet.ErrorMsg, 0);
        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<double> GetFFLACT(CgFFCHEnum ch)
    { 
        var sxExecuteRet = Invoke(() => Service!.FF_GetLACT((Semix.WcfTransfer.DTO.FFCH)ch));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<double>(sxExecuteRet.ErrorMsg, 0);
        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<double> GetFFPACT(CgFFCHEnum ch)
    { 
        var sxExecuteRet = Invoke(() => Service!.FF_GetPACT((Semix.WcfTransfer.DTO.FFCH)ch));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<double>(sxExecuteRet.ErrorMsg, 0);
        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);    
    }

    public SxExecuteRet<bool> SetFFRPOS_CH3(FFCH ch, double pos)
    {
        var sxExecuteRet = Invoke(() => Service!.FF_Move_RPOS_CH3((Semix.WcfTransfer.DTO.FFCH)ch,pos));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<bool>(sxExecuteRet.ErrorMsg, false);
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetFFLPOS_CH3(FFCH ch, double pos)
    {
        var sxExecuteRet = Invoke(() => Service!.FF_Move_LPOS_CH3((Semix.WcfTransfer.DTO.FFCH)ch, pos));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<bool>(sxExecuteRet.ErrorMsg, false);
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetFFPPOS_CH3(FFCH ch, double pos)
    {
        var sxExecuteRet = Invoke(() => Service!.FF_Move_PPOS_CH3((Semix.WcfTransfer.DTO.FFCH)ch, pos));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<bool>(sxExecuteRet.ErrorMsg, false);
        return SxExecuteRetHelper.CreateSuccess(true);
    }
}