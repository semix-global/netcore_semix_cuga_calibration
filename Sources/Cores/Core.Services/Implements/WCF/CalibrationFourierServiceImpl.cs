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
        var service = Service;

        if (service is null) return SxExecuteRetHelper.CreateError<ICalibrationFourierService.FourierMotorConfiguration>("傅里叶服务尚未连接.");

        var sxExecuteRet = Invoke(() => service.GetFFTConfig());

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<ICalibrationFourierService.FourierMotorConfiguration>(sxExecuteRet.ErrorMsg);

        if (sxExecuteRet.Anything is not
            {
                FFCH12_Range: { } sideRange,
                FFCH3_XRange: { } xRange,
                FFCH3_YRange: { } yRange
            } configuration)
        {
            return SxExecuteRetHelper.CreateError<ICalibrationFourierService.FourierMotorConfiguration>("傅里叶电机范围配置为空或不完整.");
        }

        return SxExecuteRetHelper.CreateSuccess(new ICalibrationFourierService.FourierMotorConfiguration(
            SideRodTotalCount: configuration.FF_CH1_2RodNum,
            SideMinMotorAbsoluteValue: sideRange.MinPos,
            SideMaxMotorAbsoluteValue: sideRange.MaxPos,
            CenterXMinMotorAbsoluteValue: xRange.LPOSMin,
            CenterXMaxMotorAbsoluteValue: xRange.LPOSMax,
            CenterPushMinMotorAbsoluteValue: xRange.PPOSMin,
            CenterPushMaxMotorAbsoluteValue: xRange.PPOSMax,
            CenterYMinMotorAbsoluteValue: yRange.LPOSMin,
            CenterYMaxMotorAbsoluteValue: yRange.LPOSMax,
            CenterReflectionPushMinMotorAbsoluteValue: xRange.RFPOSMin,
            CenterReflectionPushMaxMotorAbsoluteValue: xRange.RFPOSMax,
            CenterDiffractionPushMinMotorAbsoluteValue: xRange.DFPOSMin,
            CenterDiffractionPushMaxMotorAbsoluteValue: xRange.DFPOSMax));
    }

    public SxExecuteRet<bool> SetChannel3XTurntable(double angleMotorAbsoluteValue, double motorAbsoluteValue) => MoveChannel3Turntable(FFCH.Ch3_X, angleMotorAbsoluteValue, motorAbsoluteValue);

    public SxExecuteRet<bool> SetChannel3XPush(double motorAbsoluteValue)
    {
        var setSxExecuteRet = Invoke(() => Service!.FF_Move_PPOS_CH3(FFCH.Ch3_X, motorAbsoluteValue));
        if (setSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(setSxExecuteRet.ErrorMsg, false);

        var getSxExecuteRet = Invoke(() => Service!.FF_GetPACT(FFCH.Ch3_X));
        if (getSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(setSxExecuteRet.ErrorMsg, false);

        if ((int)getSxExecuteRet.Anything - (int)motorAbsoluteValue != 0) return SxExecuteRetHelper.CreateError($"getSxExecuteRet.Anything({getSxExecuteRet.Anything}) - motorAbsoluteValue({motorAbsoluteValue}) != 0", false);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetChannel3XReflectionPush(double motorAbsoluteValue)
    {
        if (double.IsFinite(motorAbsoluteValue) == false) return SxExecuteRetHelper.CreateError("X反射推杆目标位置必须为有限值.", false);

        var service = Service;

        if (service is null) return SxExecuteRetHelper.CreateError("傅里叶服务尚未连接.", false);

        var setSxExecuteRet = Invoke(() => service.FF_Move_RFPOS_CH3(FFCH.Ch3_X, motorAbsoluteValue));

        if (setSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(setSxExecuteRet.ErrorMsg, false);

        var getSxExecuteRet = Invoke(() => service.GetRFPOS());

        if (getSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(getSxExecuteRet.ErrorMsg, false);

        if (double.IsFinite(getSxExecuteRet.Anything) == false || Math.Abs(getSxExecuteRet.Anything - motorAbsoluteValue) > 0.001)
        {
            return SxExecuteRetHelper.CreateError($"X反射推杆位置回读不一致: 实际 {getSxExecuteRet.Anything:0.######}, 目标 {motorAbsoluteValue:0.######}.", false);
        }

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetChannel3XDiffractionPush(double motorAbsoluteValue)
    {
        if (double.IsFinite(motorAbsoluteValue) == false) return SxExecuteRetHelper.CreateError("X衍射推杆目标位置必须为有限值.", false);

        var service = Service;

        if (service is null) return SxExecuteRetHelper.CreateError("傅里叶服务尚未连接.", false);

        var setSxExecuteRet = Invoke(() => service.FF_Move_DFPOS_CH3(FFCH.Ch3_X, motorAbsoluteValue));

        if (setSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(setSxExecuteRet.ErrorMsg, false);

        var getSxExecuteRet = Invoke(() => service.GetDFPOS());

        if (getSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(getSxExecuteRet.ErrorMsg, false);

        if (double.IsFinite(getSxExecuteRet.Anything) == false || Math.Abs(getSxExecuteRet.Anything - motorAbsoluteValue) > 0.001)
        {
            return SxExecuteRetHelper.CreateError($"X衍射推杆位置回读不一致: 实际 {getSxExecuteRet.Anything:0.######}, 目标 {motorAbsoluteValue:0.######}.", false);
        }

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetChannel3YTurntable(double angleMotorAbsoluteValue, double motorAbsoluteValue) => MoveChannel3Turntable(FFCH.Ch3_Y, angleMotorAbsoluteValue, motorAbsoluteValue);

    private SxExecuteRet<bool> MoveChannel3Turntable(FFCH channel, double angleMotorAbsoluteValue, double motorAbsoluteValue)
    {
        var setSxExecuteRet = Invoke(() => Service!.FF_Move_RPOS_CH3(channel, angleMotorAbsoluteValue));
        if (setSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(setSxExecuteRet.ErrorMsg, false);

        var getSxExecuteRet = Invoke(() => Service!.FF_GetRACT(channel));
        if (getSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(setSxExecuteRet.ErrorMsg, false);

        if ((int)getSxExecuteRet.Anything - (int)angleMotorAbsoluteValue != 0) return SxExecuteRetHelper.CreateError($"getSxExecuteRet.Anything({getSxExecuteRet.Anything}) - angleMotorAbsoluteValue({angleMotorAbsoluteValue}) != 0", false);

        setSxExecuteRet = Invoke(() => Service!.FF_Move_LPOS_CH3(channel, motorAbsoluteValue));

        getSxExecuteRet = Invoke(() => Service!.FF_GetLACT(channel));
        if (getSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(setSxExecuteRet.ErrorMsg, false);

        if ((int)getSxExecuteRet.Anything - (int)motorAbsoluteValue != 0) return SxExecuteRetHelper.CreateError($"getSxExecuteRet.Anything({getSxExecuteRet.Anything}) - motorAbsoluteValue({motorAbsoluteValue}) != 0", false);

        return setSxExecuteRet.IsSuccess
            ? SxExecuteRetHelper.CreateSuccess(true)
            : SxExecuteRetHelper.CreateError(setSxExecuteRet.ErrorMsg, false);
    }
}