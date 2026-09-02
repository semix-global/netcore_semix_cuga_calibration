using Core.Models.Enums.ADS;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.ADS;
using Cuga.Data.DataStruct.Basic;
using Cuga.Data.DataStruct.Board;
using Cuga.Engine.Interface;
using Net.Utilities.Attributes;
using Net.Utilities.Calibration;
using Net.Utilities.Enums;
using Semix.CoreLib;
using Semix.WcfTransfer.DTO;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationAdsService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationAdsServiceImpl : BaseService<ICgCalibrationService>, ICalibrationAdsService
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

    public SxExecuteRet<(double X1, double X2)> GetSensorXSpeedFeedForwardValue(bool isPositive)
    {
        var sxExecuteRet = Invoke(() => Service!.GetADSData());
        if (sxExecuteRet.Success == false) return SxExecuteRetHelper.CreateError<(double X1, double X2)>(sxExecuteRet.Msg, (0, 0));
        var cgAdsDataInfo = sxExecuteRet.Anything;

        return isPositive
            ? SxExecuteRetHelper.CreateSuccess((Convert.ToDouble(cgAdsDataInfo.XFeedZ1PositiveLowSpeed), Convert.ToDouble(cgAdsDataInfo.XFeedZ2PositiveLowSpeed)))
            : SxExecuteRetHelper.CreateSuccess((Convert.ToDouble(cgAdsDataInfo.XFeedZ1NegativeLowSpeed), Convert.ToDouble(cgAdsDataInfo.XFeedZ2NegativeLowSpeed)));
    }

    public SxExecuteRet<bool> SetSensorXSpeedFeedForwardValue(bool isPositive, (double X1, double X2) value)
    {
        var sxExecuteRet = Invoke(() => Service!.SetXSpeedFeed(SxSpeedEnum.Low, isPositive, (Convert.ToUInt16(value.X1), Convert.ToUInt16(value.X2))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double Y1, double Y2, double Y3)> GetSensorYSpeedFeedForwardValue(bool isPositive)
    {
        var sxExecuteRet = Invoke(() => Service!.GetADSData());
        if (sxExecuteRet.Success == false) return SxExecuteRetHelper.CreateError<(double Y1, double Y2, double Y3)>(sxExecuteRet.Msg, (0, 0, 0));
        var cgAdsDataInfo = sxExecuteRet.Anything;

        return isPositive
            ? SxExecuteRetHelper.CreateSuccess((Convert.ToDouble(cgAdsDataInfo.YFeedZ1PositiveSpeed), Convert.ToDouble(cgAdsDataInfo.YFeedZ2PositiveSpeed), Convert.ToDouble(cgAdsDataInfo.YFeedZ3PositiveSpeed)))
            : SxExecuteRetHelper.CreateSuccess((Convert.ToDouble(cgAdsDataInfo.YFeedZ1NegativeSpeed), Convert.ToDouble(cgAdsDataInfo.YFeedZ2NegativeSpeed), Convert.ToDouble(cgAdsDataInfo.YFeedZ3NegativeSpeed)));
    }

    public SxExecuteRet<bool> SetSensorYSpeedFeedForwardValue(bool isPositive, (double Y1, double Y2, double Y3) value)
    {
        var sxExecuteRet = Invoke(() => Service!.SetYSpeedFeed(SxSpeedEnum.Low, isPositive, (Convert.ToUInt16(value.Y1), Convert.ToUInt16(value.Y2), Convert.ToUInt16(value.Y3))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double Z1, double Z2, double Z3)> GetSensorSpeedZ1Z2Z3Value()
    {
        var sxExecuteRet = Invoke(() => Service!.GetADSData());
        if (sxExecuteRet.Success == false) return SxExecuteRetHelper.CreateError<(double Z1, double Z2, double Z3)>(sxExecuteRet.Msg, (0, 0, 0));
        var cgAdsDataInfo = sxExecuteRet.Anything;

        return SxExecuteRetHelper.CreateSuccess((Convert.ToDouble(cgAdsDataInfo.Z1Ecs), Convert.ToDouble(cgAdsDataInfo.Z2Ecs), Convert.ToDouble(cgAdsDataInfo.Z3Ecs)));
    }

    public SxExecuteRet<bool> SetSensorFeedForwardPressureValue(double pressureValue1, double pressureValue2, double pressureValue3)
    {
        var sxExecuteRet = Invoke(() => Service!.SetSensorPressureValue(Convert.ToUInt16(pressureValue1), Convert.ToUInt16(pressureValue2), Convert.ToUInt16(pressureValue3)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public async Task<SxExecuteRet<List<(double PressureValue1, double PressureValue2, double PressureValue3)>>> GetSensorAllPressureTraceBufferListAsync(CancellationToken cancellationToken)
    {
        var calibrationRegList = new List<CgADSTraceBufferReg>
        {
            AdsTracebufferRegEnum.PropOutput0.ToCgADSTraceBufferRegEnum(),
            AdsTracebufferRegEnum.PropOutput1.ToCgADSTraceBufferRegEnum(),
            AdsTracebufferRegEnum.PropOutput2.ToCgADSTraceBufferRegEnum(),
        };
        var sxExecuteRetStartADSTraceBuffExec = Invoke(() => Service!.StartADSTraceBuff(calibrationRegList));

        if (sxExecuteRetStartADSTraceBuffExec.IsSuccess == false)
            return SxExecuteRetHelper.CreateError<
                List<(double PressureValue1, double PressureValue2, double PressureValue3)>>(sxExecuteRetStartADSTraceBuffExec.Msg, []);

        await cancellationToken.WaitUntilCanceledAsync();

        var sxExecuteRetStopADSTraceBuffExec = Invoke(() => Service!.StopADSTraceBuff(calibrationRegList));

        if (sxExecuteRetStopADSTraceBuffExec.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<(double PressureValue1, double PressureValue2, double PressureValue3)>>(sxExecuteRetStopADSTraceBuffExec.Msg, []);
        if (sxExecuteRetStopADSTraceBuffExec.Anything[0].Count == 0
            || sxExecuteRetStopADSTraceBuffExec.Anything[1].Count == 0
            || sxExecuteRetStopADSTraceBuffExec.Anything[2].Count == 0
            || sxExecuteRetStopADSTraceBuffExec.Anything[0].Count != sxExecuteRetStopADSTraceBuffExec.Anything[1].Count
            || sxExecuteRetStopADSTraceBuffExec.Anything[1].Count != sxExecuteRetStopADSTraceBuffExec.Anything[2].Count) return SxExecuteRetHelper.CreateError<List<(double PressureValue1, double PressureValue2, double PressureValue3)>>("Ads error trans buffer is empty", []);

        // 将三个压力值的数据合并成一个列表
        var pressureList = sxExecuteRetStopADSTraceBuffExec.Anything[0]
            .Select((t, i) => (PressureValue1: Convert.ToDouble(t), PressureValue2: Convert.ToDouble(sxExecuteRetStopADSTraceBuffExec.Anything[1][i]), PressureValue3: Convert.ToDouble(sxExecuteRetStopADSTraceBuffExec.Anything[2][i])))
            .ToList();

        return SxExecuteRetHelper.CreateSuccess(pressureList);
    }

    public async Task<SxExecuteRet<List<(double Height, double Roll, double Pitch, double xSpeed, double ySpeed)>>> GetSensorHeightRollPitchTraceBufferListAsync(CancellationToken cancellationToken)
    {
        var calibrationRegList = new List<CgADSTraceBufferReg>
        {
            AdsTracebufferRegEnum.Height.ToCgADSTraceBufferRegEnum(),
            AdsTracebufferRegEnum.Roll.ToCgADSTraceBufferRegEnum(),
            AdsTracebufferRegEnum.Pitch.ToCgADSTraceBufferRegEnum(),
            AdsTracebufferRegEnum.ACS_X_Speed.ToCgADSTraceBufferRegEnum(),
            AdsTracebufferRegEnum.ACS_Y_Speed.ToCgADSTraceBufferRegEnum()
        };
        var sxExecuteRetStartADSTraceBuffExec = Invoke(() => Service!.StartADSTraceBuff(calibrationRegList));

        if (sxExecuteRetStartADSTraceBuffExec.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<(double Height, double Roll, double Pitch, double AcsXSpeed, double AcsYSpeed)>>(sxExecuteRetStartADSTraceBuffExec.Msg, []);

        await cancellationToken.WaitUntilCanceledAsync();

        var sxExecuteRetStopADSTraceBuffExec = Invoke(() => Service!.StopADSTraceBuff(calibrationRegList));

        if (sxExecuteRetStopADSTraceBuffExec.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<(double Height, double Roll, double Pitch, double AcsXSpeed, double AcsYSpeed)>>(sxExecuteRetStopADSTraceBuffExec.Msg, []);

        if (sxExecuteRetStopADSTraceBuffExec.Anything.Count != 5
            || sxExecuteRetStopADSTraceBuffExec.Anything.Any(t => t.Count == 0)
            || sxExecuteRetStopADSTraceBuffExec.Anything[0].Count != sxExecuteRetStopADSTraceBuffExec.Anything[1].Count
            || sxExecuteRetStopADSTraceBuffExec.Anything[1].Count != sxExecuteRetStopADSTraceBuffExec.Anything[2].Count) return SxExecuteRetHelper.CreateError<List<(double Height, double Roll, double Pitch, double AcsXSpeed, double AcsYSpeed)>>("Ads error trans buffer is empty", []);

        // 将三个地址的traceBuffer的数据合并成一个列表
        var traceBufferList = sxExecuteRetStopADSTraceBuffExec.Anything
            .Select(shortList => shortList.Select(Convert.ToDouble).ToList())
            .ToList();
        var resultList = traceBufferList[0]
            .Select((_, index) => (Height: traceBufferList[0][index], Roll: traceBufferList[1][index], Pitch: traceBufferList[2][index], AcsXSpeed: traceBufferList[3][index], AcsYSpeed: traceBufferList[4][index]))
            .ToList();

        return SxExecuteRetHelper.CreateSuccess(resultList);
    }

    public async Task<SxExecuteRet<(List<double> Z_ECS0, List<double> Z_ECS1, List<double> Z_ECS2, List<double> Height, List<double> Roll, List<double> Pitch, List<double> X_Speed, List<double> Y_Speed)>> GetSensorSpeedZ1Z2Z3TraceBufferListAsync(CancellationToken cancellationToken)
    {
        var calibrationRegList = new List<CgADSTraceBufferReg>
        {
            AdsTracebufferRegEnum.Z_ECS0.ToCgADSTraceBufferRegEnum(),
            AdsTracebufferRegEnum.Z_ECS1.ToCgADSTraceBufferRegEnum(),
            AdsTracebufferRegEnum.Z_ECS2.ToCgADSTraceBufferRegEnum(),
            AdsTracebufferRegEnum.Height.ToCgADSTraceBufferRegEnum(),
            AdsTracebufferRegEnum.Roll.ToCgADSTraceBufferRegEnum(),
            AdsTracebufferRegEnum.Pitch.ToCgADSTraceBufferRegEnum(),
            AdsTracebufferRegEnum.ACS_X_Speed.ToCgADSTraceBufferRegEnum(),
            AdsTracebufferRegEnum.ACS_Y_Speed.ToCgADSTraceBufferRegEnum()
        };
        var sxExecuteRetStartADSTraceBuffExec = Invoke(() => Service!.StartADSTraceBuff(calibrationRegList));

        if (sxExecuteRetStartADSTraceBuffExec.IsSuccess == false)
            return SxExecuteRetHelper.CreateError<
                (List<double> Z_ECS0, List<double> Z_ECS1, List<double> Z_ECS2,
                List<double> Height, List<double> Roll, List<double> Pitch,
                List<double> X_Speed, List<double> Y_Speed)>(sxExecuteRetStartADSTraceBuffExec.Msg, ([], [], [], [], [], [], [], []));

        await cancellationToken.WaitUntilCanceledAsync();

        var sxExecuteRetStopADSTraceBuffExec = Invoke(() => Service!.StopADSTraceBuff(calibrationRegList));

        if (sxExecuteRetStopADSTraceBuffExec.IsSuccess == false)
            return SxExecuteRetHelper.CreateError<
                (List<double> Z_ECS0, List<double> Z_ECS1, List<double> Z_ECS2,
                List<double> Height, List<double> Roll, List<double> Pitch,
                List<double> X_Speed, List<double> Y_Speed)>(sxExecuteRetStopADSTraceBuffExec.Msg, ([], [], [], [], [], [], [], []));

        if (sxExecuteRetStopADSTraceBuffExec.Anything.Count != 8
            || sxExecuteRetStopADSTraceBuffExec.Anything.Any(t => t.Count == 0)
            || sxExecuteRetStopADSTraceBuffExec.Anything[0].Count != sxExecuteRetStopADSTraceBuffExec.Anything[1].Count
            || sxExecuteRetStopADSTraceBuffExec.Anything[1].Count != sxExecuteRetStopADSTraceBuffExec.Anything[2].Count
            || sxExecuteRetStopADSTraceBuffExec.Anything[2].Count != sxExecuteRetStopADSTraceBuffExec.Anything[3].Count
            || sxExecuteRetStopADSTraceBuffExec.Anything[3].Count != sxExecuteRetStopADSTraceBuffExec.Anything[4].Count
            || sxExecuteRetStopADSTraceBuffExec.Anything[4].Count != sxExecuteRetStopADSTraceBuffExec.Anything[5].Count)
            return SxExecuteRetHelper.CreateError<
                (List<double> Z_ECS0, List<double> Z_ECS1, List<double> Z_ECS2,
                List<double> Height, List<double> Roll, List<double> Pitch,
                List<double> X_Speed, List<double> Y_Speed)>("Ads error trans buffer is empty", ([], [], [], [], [], [], [], []));

        // 将三个地址的tracebuffer的数据合并成一个列表
        var tracebufferList = sxExecuteRetStopADSTraceBuffExec.Anything
            .Select(shortList => shortList.Select(Convert.ToDouble).ToList())
            .ToList();

        return SxExecuteRetHelper.CreateSuccess((tracebufferList[0], tracebufferList[1], tracebufferList[2], tracebufferList[3],
            tracebufferList[4], tracebufferList[5], tracebufferList[6], tracebufferList[7]));
    }

    public async Task<SxExecuteRet<(List<double> X0, List<double> X1, List<double> Y0, List<double> Y1, List<double> Speed)>> GetSensorSpeedX0X1Y0Y1WithSpeedTraceBufferListAsync(bool isAxisX, CancellationToken cancellationToken)
    {
        var calibrationRegList = new List<CgADSTraceBufferReg>
        {
            AdsTracebufferRegEnum.XY_X0.ToCgADSTraceBufferRegEnum(),
            AdsTracebufferRegEnum.XY_X1.ToCgADSTraceBufferRegEnum(),
            AdsTracebufferRegEnum.XY_Y0.ToCgADSTraceBufferRegEnum(),
            AdsTracebufferRegEnum.XY_Y1.ToCgADSTraceBufferRegEnum(),
            isAxisX
                ? AdsTracebufferRegEnum.ACS_X_Speed.ToCgADSTraceBufferRegEnum()
                : AdsTracebufferRegEnum.ACS_Y_Speed.ToCgADSTraceBufferRegEnum()
        };
        var sxExecuteRetStartADSTraceBuffExec = Invoke(() => Service!.StartADSTraceBuff(calibrationRegList));

        if (sxExecuteRetStartADSTraceBuffExec.IsSuccess == false)
            return SxExecuteRetHelper.CreateError<
                (List<double> X0, List<double> X1,
                List<double> Y0, List<double> Y1, List<double> Speed)>(sxExecuteRetStartADSTraceBuffExec.Msg, ([], [], [], [], []));

        await cancellationToken.WaitUntilCanceledAsync();

        var sxExecuteRetStopADSTraceBuffExec = Invoke(() => Service!.StopADSTraceBuff(calibrationRegList));

        if (sxExecuteRetStopADSTraceBuffExec.IsSuccess == false)
            return SxExecuteRetHelper.CreateError<
                (List<double> X0, List<double> X1,
                List<double> Y0, List<double> Y1, List<double> Speed)>(sxExecuteRetStopADSTraceBuffExec.Msg, ([], [], [], [], []));

        if (sxExecuteRetStopADSTraceBuffExec.Anything.Count != 5
            || sxExecuteRetStopADSTraceBuffExec.Anything.Any(t => t.Count == 0)
            || sxExecuteRetStopADSTraceBuffExec.Anything[0].Count != sxExecuteRetStopADSTraceBuffExec.Anything[1].Count
            || sxExecuteRetStopADSTraceBuffExec.Anything[1].Count != sxExecuteRetStopADSTraceBuffExec.Anything[2].Count
            || sxExecuteRetStopADSTraceBuffExec.Anything[2].Count != sxExecuteRetStopADSTraceBuffExec.Anything[3].Count
            || sxExecuteRetStopADSTraceBuffExec.Anything[3].Count != sxExecuteRetStopADSTraceBuffExec.Anything[4].Count)
            return SxExecuteRetHelper.CreateError<
                (List<double> X0, List<double> X1,
                List<double> Y0, List<double> Y1, List<double> Speed)>("Ads error trans buffer is empty", ([], [], [], [], []));

        // 将四个地址的tracebuffer数据合并成一个列表
        var tracebufferList = sxExecuteRetStopADSTraceBuffExec.Anything
            .Select(shortList => shortList.Select(Convert.ToDouble).ToList())
            .ToList();

        return SxExecuteRetHelper.CreateSuccess((tracebufferList[0], tracebufferList[1], tracebufferList[2], tracebufferList[3], tracebufferList[4]));
    }

    public SxExecuteRet<bool> SetAdsXyEnabled(bool isEnabled)
    {
        var dataInfoList = new List<CgBoardDataInfo>
        {
            new() { Reg = CgBoardReg.REG2, Data = Convert.ToInt32(isEnabled) }
        };
        var sxExecuteRet = Invoke(() => Service!.SeADSReg(dataInfoList));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }
}