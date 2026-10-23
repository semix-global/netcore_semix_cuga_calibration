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
        var sxExecuteRet = Invoke(() => Service!.FF_GetConfig());

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<ICalibrationFourierService.FourierMotorConfiguration>(sxExecuteRet.ErrorMsg);

        return SxExecuteRetHelper.CreateSuccess(new ICalibrationFourierService.FourierMotorConfiguration(
            SideRodTotalCount: sxExecuteRet.Anything.RodNum,
            SideMinMotorAbsoluteValue: sxExecuteRet.Anything.CH12MinPOS,
            SideMaxMotorAbsoluteValue: sxExecuteRet.Anything.CH12MaxPOS,
            CenterXMinMotorAbsoluteValue: sxExecuteRet.Anything.CH3_X_LPOSMin,
            CenterXMaxMotorAbsoluteValue: sxExecuteRet.Anything.CH3_X_LPOSMax,
            CenterPushMinMotorAbsoluteValue: sxExecuteRet.Anything.CH3_X_PPOSMin,
            CenterPushMaxMotorAbsoluteValue: sxExecuteRet.Anything.CH3_X_PPOSMax,
            CenterYMinMotorAbsoluteValue: sxExecuteRet.Anything.CH3_Y_LPOSMin,
            CenterYMaxMotorAbsoluteValue: sxExecuteRet.Anything.CH3_Y_LPOSMax,
            // 当前 SDK 未提供衍射推杆范围, 使用无效范围表示尚未接入.
            CenterDiffractionPushMinMotorAbsoluteValue: 0d,
            CenterDiffractionPushMaxMotorAbsoluteValue: 0d));
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

    public SxExecuteRet<bool> SetChannel3XDiffractionPush(double motorAbsoluteValue)
    {
        throw new NotImplementedException("当前 SDK 尚未提供通道3的X衍射推杆控制接口.");
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