using CommunityToolkit.Diagnostics;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Engine.Interface;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Graphics.Extensions;
using Net.Utilities.Graphics.Primitives.Medias.Imaging;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
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

    public SxExecuteRet<bool> Home(int channelId)
    {
        var channels = channelId switch
        {
            1 => [Semix.WcfTransfer.DTO.FFCH.Ch1],
            2 => [Semix.WcfTransfer.DTO.FFCH.Ch2],
            3 => [Semix.WcfTransfer.DTO.FFCH.Ch3_X, Semix.WcfTransfer.DTO.FFCH.Ch3_Y],
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Semix.WcfTransfer.DTO.FFCH[]>(nameof(channelId))
        };

        foreach (var channel in channels)
        {
            var sxExecuteRet = Invoke(() => Service!.SetFFHome(channel));

            if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, false);
        }

        if (channelId == 3) // ch3有回零缓存, 如果回零过, 所以就直接ok, 所以需要手动移动. 但是ch1 2没有回零缓存
        {
            var sxExecuteRet = SetFFRPOS_CH3(FFCH.Ch3_X, 0d);
            if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, false);

            sxExecuteRet = SetFFLPOS_CH3(FFCH.Ch3_X, 0d);
            if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, false);

            sxExecuteRet = SetFFRPOS_CH3(FFCH.Ch3_Y, 0d);
            if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, false);

            sxExecuteRet = SetFFLPOS_CH3(FFCH.Ch3_Y, 0d);
            if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, false);

            sxExecuteRet = SetFFPPOS_CH3(FFCH.Ch3_X, 0d);
            if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, false);
        }

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetRods(int channelId, double[] rodPositions)
    {
        var wcfChannelId = channelId switch
        {
            1 => Semix.WcfTransfer.DTO.FFCH.Ch1,
            2 => Semix.WcfTransfer.DTO.FFCH.Ch2,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<Semix.WcfTransfer.DTO.FFCH>(nameof(channelId))
        };

        var sxExecuteRet = Invoke(() => Service!.FF_Move_CH12_Pos(wcfChannelId,
        [
            .. rodPositions
                .Index()
                .Select(t => (t.Index + 1 /* Cuga配置规定 */, t.Item))
        ]));

        return sxExecuteRet.IsSuccess
            ? SxExecuteRetHelper.CreateSuccess(true)
            : SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, false);
    }

    public SxExecuteRet<BitmapImage> GetImage(
        ProductivityInformation productivityInformation,
        LaserLightInformation laserLightInformation,
        Point dfPosition,
        double scanLength,
        int channelId,
        bool isCustomWaveform)
    {
        var wcfParam = new Semix.WcfTransfer.DTO.SxOpticsParam
        {
            Magnification = productivityInformation.AdaptTo().Mag,
            Speed = productivityInformation.AdaptTo().Speed,
            NIOI = productivityInformation.OpticsIlluminationModeEnum.ToSxNIOIEnum(),
            LightLevelUnit = laserLightInformation.Level,
            OpenZoos = true
        };

        var sxExecuteRet = Invoke(() => Service!.GetFFReviewImgForTriggerCalibration(
            channelId - 1, /* Cuga配置规定 */
            wcfParam,
            dfPosition.ToSxPointD(),
            Convert.ToInt32(scanLength),
            isCustomWaveform)
        );

#pragma warning disable IDE0079
#pragma warning disable IDISP001

        if (sxExecuteRet.IsSuccess == false)
        {
            var defaultBitmapImage = BitmapImage.Empty;

            return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, defaultBitmapImage);
        }

        var bitmapImage = new BitmapImage(sxExecuteRet.Anything);

        return SxExecuteRetHelper.CreateSuccess(bitmapImage);

#pragma warning restore IDISP001
#pragma warning restore IDE0079
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

    public SxExecuteRet<bool> SetFFRPOS_CH3(FFCH ch, double pos)
    {
        var sxExecuteRet = Invoke(() => Service!.FF_Move_RPOS_CH3((Semix.WcfTransfer.DTO.FFCH)ch, pos));
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