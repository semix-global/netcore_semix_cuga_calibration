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
using Semix.WcfTransfer.DTO;

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

    public SxExecuteRet<bool> Home(int channelId, bool isGoToZero)
    {
        var channels = channelId switch
        {
            1 => [FFCH.Ch1],
            2 => [FFCH.Ch2],
            3 => [FFCH.Ch3_X, FFCH.Ch3_Y],
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<FFCH[]>(nameof(channelId))
        };

        foreach (var channel in channels)
        {
            var sxExecuteRet = Invoke(() => Service!.SetFFHome(channel));

            if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, false);
        }

        if (isGoToZero && channelId == 3) // Cuga配置规定: ch3有回零缓存, 需要显式将转盘和推杆移动到零位; ch1 2回零就会去零点, 没有缓存.
        {
            var sxExecuteRet = SetChannel3XTurntable(0d, 0d);
            if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, false);

            sxExecuteRet = SetChannel3YTurntable(0d, 0d);
            if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, false);

            sxExecuteRet = SetChannel3XPush(0d);
            if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, false);

            sxExecuteRet = SetChannel3XReflectionPush(0d);
            if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, false);

            sxExecuteRet = SetChannel3XDiffractionPush(0d);
            if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, false);
        }

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetRods(int channelId, double[] rodPositions)
    {
        var wcfChannelId = channelId switch
        {
            1 => FFCH.Ch1,
            2 => FFCH.Ch2,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<FFCH>(nameof(channelId))
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
        var wcfParam = new SxOpticsParam
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

    public SxExecuteRet<ICalibrationFourierService.FourierMotorConfiguration> GetMotorConfiguration()
    {
        var sxExecuteRet = Invoke(() => Service!.GetFFTConfig());

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<ICalibrationFourierService.FourierMotorConfiguration>(sxExecuteRet.ErrorMsg);

        return SxExecuteRetHelper.CreateSuccess(new ICalibrationFourierService.FourierMotorConfiguration(
            SideRodTotalCount: sxExecuteRet.Anything.FF_CH1_2RodNum,
            SideMinMotorAbsoluteValue: sxExecuteRet.Anything.FFCH12_Range.MinPos,
            SideMaxMotorAbsoluteValue: sxExecuteRet.Anything.FFCH12_Range.MaxPos,
            CenterXMinMotorAbsoluteValue: sxExecuteRet.Anything.FFCH3_XRange.LPOSMin,
            CenterXMaxMotorAbsoluteValue: sxExecuteRet.Anything.FFCH3_XRange.LPOSMax,
            CenterPushMinMotorAbsoluteValue: sxExecuteRet.Anything.FFCH3_XRange.PPOSMin,
            CenterPushMaxMotorAbsoluteValue: sxExecuteRet.Anything.FFCH3_XRange.PPOSMax,
            CenterYMinMotorAbsoluteValue: sxExecuteRet.Anything.FFCH3_YRange.LPOSMin,
            CenterYMaxMotorAbsoluteValue: sxExecuteRet.Anything.FFCH3_YRange.LPOSMax,
            CenterReflectionPushMinMotorAbsoluteValue: sxExecuteRet.Anything.FFCH3_XRange.RFPOSMin,
            CenterReflectionPushMaxMotorAbsoluteValue: sxExecuteRet.Anything.FFCH3_XRange.RFPOSMax,
            CenterDiffractionPushMinMotorAbsoluteValue: sxExecuteRet.Anything.FFCH3_XRange.DFPOSMin,
            CenterDiffractionPushMaxMotorAbsoluteValue: sxExecuteRet.Anything.FFCH3_XRange.DFPOSMax));
    }

    public SxExecuteRet<bool> SetChannel3XTurntable(double angleMotorAbsoluteValue, double motorAbsoluteValue) => MoveChannel3Turntable(FFCH.Ch3_X, angleMotorAbsoluteValue, motorAbsoluteValue);

    public SxExecuteRet<bool> SetChannel3XPush(double motorAbsoluteValue)
    {
        var setSxExecuteRet = Invoke(() => Service!.FF_Move_PPOS_CH3(FFCH.Ch3_X, motorAbsoluteValue));
        if (setSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(setSxExecuteRet.ErrorMsg, false);

        var getSxExecuteRet = Invoke(() => Service!.FF_GetPACT(FFCH.Ch3_X));
        if (getSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(setSxExecuteRet.ErrorMsg, false);

        return (int)(getSxExecuteRet.Anything * 100) != (int)(motorAbsoluteValue * 100)
            ? SxExecuteRetHelper.CreateError($"(getSxExecuteRet.Anything({getSxExecuteRet.Anything}) * 100) != (int)(motorAbsoluteValue({motorAbsoluteValue}) * 100)", false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetChannel3XReflectionPush(double motorAbsoluteValue)
    {
        var setSxExecuteRet = Invoke(() => Service!.FF_Move_RFPOS_CH3(FFCH.Ch3_X, motorAbsoluteValue));
        if (setSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(setSxExecuteRet.ErrorMsg, false);

        var getSxExecuteRet = Invoke(() => Service!.GetRFPOS());
        if (getSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(getSxExecuteRet.ErrorMsg, false);

        return (int)(getSxExecuteRet.Anything * 100) != (int)(motorAbsoluteValue * 100)
            ? SxExecuteRetHelper.CreateError($"(getSxExecuteRet.Anything({getSxExecuteRet.Anything}) * 100) != (int)(motorAbsoluteValue({motorAbsoluteValue}) * 100)", false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetChannel3XDiffractionPush(double motorAbsoluteValue)
    {
        var setSxExecuteRet = Invoke(() => Service!.FF_Move_DFPOS_CH3(FFCH.Ch3_X, motorAbsoluteValue));
        if (setSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(setSxExecuteRet.ErrorMsg, false);

        var getSxExecuteRet = Invoke(() => Service!.GetDFPOS());
        if (getSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(getSxExecuteRet.ErrorMsg, false);

        return (int)(getSxExecuteRet.Anything * 100) != (int)(motorAbsoluteValue * 100)
            ? SxExecuteRetHelper.CreateError($"(getSxExecuteRet.Anything({getSxExecuteRet.Anything}) * 100) != (int)(motorAbsoluteValue({motorAbsoluteValue}) * 100)", false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetChannel3YTurntable(double angleMotorAbsoluteValue, double motorAbsoluteValue) => MoveChannel3Turntable(FFCH.Ch3_Y, angleMotorAbsoluteValue, motorAbsoluteValue);

    private SxExecuteRet<bool> MoveChannel3Turntable(FFCH channel, double angleMotorAbsoluteValue, double motorAbsoluteValue)
    {
        var setSxExecuteRet = Invoke(() => Service!.FF_Move_RPOS_CH3(channel, angleMotorAbsoluteValue));
        if (setSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(setSxExecuteRet.ErrorMsg, false);

        var getSxExecuteRet = Invoke(() => Service!.FF_GetRACT(channel));
        if (getSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(setSxExecuteRet.ErrorMsg, false);

        if ((int)(getSxExecuteRet.Anything * 100) != (int)(angleMotorAbsoluteValue * 100)) return SxExecuteRetHelper.CreateError($"(getSxExecuteRet.Anything({getSxExecuteRet.Anything}) * 100) != (int)(angleMotorAbsoluteValue({angleMotorAbsoluteValue}) * 100)", false);

        setSxExecuteRet = Invoke(() => Service!.FF_Move_LPOS_CH3(channel, motorAbsoluteValue));

        getSxExecuteRet = Invoke(() => Service!.FF_GetLACT(channel));
        if (getSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(setSxExecuteRet.ErrorMsg, false);

        if ((int)(getSxExecuteRet.Anything * 100) != (int)(motorAbsoluteValue * 100)) return SxExecuteRetHelper.CreateError($"(getSxExecuteRet.Anything({getSxExecuteRet.Anything}) * 100) != (int)(motorAbsoluteValue({motorAbsoluteValue}) * 100)", false);

        return setSxExecuteRet.IsSuccess
            ? SxExecuteRetHelper.CreateSuccess(true)
            : SxExecuteRetHelper.CreateError(setSxExecuteRet.ErrorMsg, false);
    }
}