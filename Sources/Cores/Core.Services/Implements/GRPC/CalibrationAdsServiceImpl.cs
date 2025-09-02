using Core.Models.Enums.ADS;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.ADS;
using Cuga.Data.DataStruct.Board;
using Cuga.Data.DataStruct.DTO.Swath;
using Cuga.Interface.Calibration;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;

namespace Core.Services.Implements.GRPC;

[IOCAppService(ServiceType = typeof(ICalibrationAdsService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationAdsServiceImpl : BaseService<ICgCalibAdsService>, ICalibrationAdsService
{
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

    public SxExecuteRet<(double X1, double X2)> GetSensorXSpeedFeedForwardValue(bool isPositive)
    {
        var sxExecuteRet = Invoke(() => Service?.GetADSData());
        if (sxExecuteRet.Success == false) return SxExecuteRetHelper.CreateError<(double X1, double X2)>(sxExecuteRet.Msg, (0, 0));
        var cgAdsDataInfo = sxExecuteRet.Anything;

        return isPositive
            ? SxExecuteRetHelper.CreateSuccess((Convert.ToDouble(cgAdsDataInfo.XFeedZ1PositiveLowSpeed), Convert.ToDouble(cgAdsDataInfo.XFeedZ2PositiveLowSpeed)))
            : SxExecuteRetHelper.CreateSuccess((Convert.ToDouble(cgAdsDataInfo.XFeedZ1NegativeLowSpeed), Convert.ToDouble(cgAdsDataInfo.XFeedZ2NegativeLowSpeed)));
    }

    public SxExecuteRet<bool> SetSensorXSpeedFeedForwardValue(bool isPositive, (double X1, double X2) value)
    {
        var sxExecuteRet = Invoke(() => Service?.SetXSpeedFeed(new SxParamObj<(CgSpeedLevelType speed, bool isPositive, (ushort X1, ushort X2) value)>((StageSpeedEnum.Low.ToAdsSpeedEnum(), isPositive, (Convert.ToUInt16(value.X1), Convert.ToUInt16(value.X2))))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double Y1, double Y2, double Y3)> GetSensorYSpeedFeedForwardValue(bool isPositive)
    {
        var sxExecuteRet = Invoke(() => Service?.GetADSData());
        if (sxExecuteRet.Success == false) return SxExecuteRetHelper.CreateError<(double Y1, double Y2, double Y3)>(sxExecuteRet.Msg, (0, 0, 0));
        var cgAdsDataInfo = sxExecuteRet.Anything;

        return isPositive
            ? SxExecuteRetHelper.CreateSuccess((Convert.ToDouble(cgAdsDataInfo.YFeedZ1PositiveSpeed), Convert.ToDouble(cgAdsDataInfo.YFeedZ2PositiveSpeed), Convert.ToDouble(cgAdsDataInfo.YFeedZ3PositiveSpeed)))
            : SxExecuteRetHelper.CreateSuccess((Convert.ToDouble(cgAdsDataInfo.YFeedZ1NegativeSpeed), Convert.ToDouble(cgAdsDataInfo.YFeedZ2NegativeSpeed), Convert.ToDouble(cgAdsDataInfo.YFeedZ3NegativeSpeed)));
    }

    public SxExecuteRet<bool> SetSensorYSpeedFeedForwardValue(bool isPositive, (double Y1, double Y2, double Y3) value)
    {
        var sxExecuteRet = Invoke(() => Service?.SetYSpeedFeed(new SxParamObj<(CgSpeedLevelType speed, bool isPositive, (ushort Y1, ushort Y2, ushort Y3) value)>((StageSpeedEnum.Low.ToAdsSpeedEnum(), isPositive, (Convert.ToUInt16(value.Y1), Convert.ToUInt16(value.Y2), Convert.ToUInt16(value.Y3))))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double Z1, double Z2, double Z3)> GetSensorSpeedZ1Z2Z3Value()
    {
        var sxExecuteRet = Invoke(() => Service?.GetADSData());
        if (sxExecuteRet.Success == false) return SxExecuteRetHelper.CreateError<(double Z1, double Z2, double Z3)>(sxExecuteRet.Msg, (0, 0, 0));
        var cgAdsDataInfo = sxExecuteRet.Anything;

        return SxExecuteRetHelper.CreateSuccess((Convert.ToDouble(cgAdsDataInfo.Z1Ecs), Convert.ToDouble(cgAdsDataInfo.Z2Ecs), Convert.ToDouble(cgAdsDataInfo.Z3Ecs)));
    }

    public SxExecuteRet<bool> SetSensorFeedForwardPressureValue(double pressureValue1, double pressureValue2, double pressureValue3)
    {
        var sxExecuteRet = Invoke(() => Service?.SetSensorPressureValue(new SxParamObj<(ushort pressureValue1, ushort pressureValue2, ushort pressureValue3)>((Convert.ToUInt16(pressureValue1), Convert.ToUInt16(pressureValue2), Convert.ToUInt16(pressureValue3)))));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<List<(double PressureValue1, double PressureValue2, double PressureValue3)>> GetSensorAllPressureTraceBufferList(TimeSpan timeSpan)
    {
        var sxExecuteRet = Invoke(() => Service?.GetADSTraceBuff(new SxParamObj<int>((Convert.ToInt32(timeSpan.TotalMilliseconds)))));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<(double PressureValue1, double PressureValue2, double PressureValue3)>>(sxExecuteRet.Msg, []);
        if (sxExecuteRet.Anything.Pressure1.Count == 0
            || sxExecuteRet.Anything.Pressure2.Count == 0
            || sxExecuteRet.Anything.Pressure3.Count == 0
            || sxExecuteRet.Anything.Pressure1.Count != sxExecuteRet.Anything.Pressure2.Count
            || sxExecuteRet.Anything.Pressure2.Count != sxExecuteRet.Anything.Pressure3.Count) return SxExecuteRetHelper.CreateError<List<(double PressureValue1, double PressureValue2, double PressureValue3)>>("Ads error trans buffer is empty", []);

        // 将三个压力值的数据合并成一个列表
        var pressureList = sxExecuteRet.Anything.Pressure1
            .Select((t, i) => (PressureValue1: Convert.ToDouble(t), PressureValue2: Convert.ToDouble(sxExecuteRet.Anything.Pressure2[i]), PressureValue3: Convert.ToDouble(sxExecuteRet.Anything.Pressure3[i])))
            .ToList();

        return SxExecuteRetHelper.CreateSuccess(pressureList);
    }

    public SxExecuteRet<List<(double Height, double Roll, double Pitch, double xSpeed, double ySpeed)>> GetSensorHeightRollPitchTraceBufferList(TimeSpan timeSpan)
    {
        var calibrationRegList = new List<CalibrationRegEnum>
        {
            AdsTracebufferRegEnum.Height.ToCalibrationRegEnum(),
            AdsTracebufferRegEnum.Roll.ToCalibrationRegEnum(),
            AdsTracebufferRegEnum.Pitch.ToCalibrationRegEnum(),
            AdsTracebufferRegEnum.ACS_X_Speed.ToCalibrationRegEnum(),
            AdsTracebufferRegEnum.ACS_Y_Speed.ToCalibrationRegEnum()
        };
        var sxExecuteRet = Invoke(() => Service!.GetADSTraceBuffByReg(new SxParamObj<(List<CalibrationRegEnum> regs, int time)>((calibrationRegList, Convert.ToInt32(timeSpan.TotalMilliseconds)))));
        if (sxExecuteRet.Anything.Count != 5
            || sxExecuteRet.Anything.Any(t => t.Count == 0)
            || sxExecuteRet.Anything[0].Count != sxExecuteRet.Anything[1].Count
            || sxExecuteRet.Anything[1].Count != sxExecuteRet.Anything[2].Count) return SxExecuteRetHelper.CreateError<List<(double Height, double Roll, double Pitch, double SpeedX, double SpeedY)>>("Ads error trans buffer is empty", []);
        // 将三个地址的tracebuffer的数据合并成一个列表
        var tracebufferList = sxExecuteRet.Anything
            .Select(shortList => shortList.Select(t => Convert.ToDouble(t)).ToList())
            .ToList();
        var resultList = tracebufferList[0]
            .Select((height, index) => (Height: tracebufferList[0][index], Roll: tracebufferList[1][index], Pitch: tracebufferList[2][index], SpeedX: tracebufferList[3][index], SpeedY: tracebufferList[4][index]))
            .ToList();

        return SxExecuteRetHelper.CreateSuccess(resultList);
    }

    public SxExecuteRet<List<List<double>>> GetSensorSpeedZ1Z2Z3TraceBufferList(TimeSpan timeSpan)
    {
        var calibrationRegList = new List<CalibrationRegEnum>
        {
            AdsTracebufferRegEnum.Z_ECS0.ToCalibrationRegEnum(),
            AdsTracebufferRegEnum.Z_ECS1.ToCalibrationRegEnum(),
            AdsTracebufferRegEnum.Z_ECS2.ToCalibrationRegEnum(),
            AdsTracebufferRegEnum.Height.ToCalibrationRegEnum(),
            AdsTracebufferRegEnum.Roll.ToCalibrationRegEnum(),
            AdsTracebufferRegEnum.Pitch.ToCalibrationRegEnum(),
            AdsTracebufferRegEnum.ACS_X_Speed.ToCalibrationRegEnum(),
            AdsTracebufferRegEnum.ACS_Y_Speed.ToCalibrationRegEnum()
        };
        var sxExecuteRet = Invoke(() => Service!.GetADSTraceBuffByReg(new SxParamObj<(List<CalibrationRegEnum> regs, int time)>((calibrationRegList, Convert.ToInt32(timeSpan.TotalMilliseconds)))));

        if (sxExecuteRet.Anything.Count != 8
            || sxExecuteRet.Anything.Any(t => t.Count == 0)
            || sxExecuteRet.Anything[0].Count != sxExecuteRet.Anything[1].Count
            || sxExecuteRet.Anything[1].Count != sxExecuteRet.Anything[2].Count
            || sxExecuteRet.Anything[2].Count != sxExecuteRet.Anything[3].Count
            || sxExecuteRet.Anything[3].Count != sxExecuteRet.Anything[4].Count
            || sxExecuteRet.Anything[4].Count != sxExecuteRet.Anything[5].Count) return SxExecuteRetHelper.CreateError<List<List<double>>>("Ads error trans buffer is empty", []);

        // 将三个地址的tracebuffer的数据合并成一个列表
        var tracebufferList = sxExecuteRet.Anything
            .Select(shortList => shortList.Select(Convert.ToDouble).ToList())
            .ToList();

        return SxExecuteRetHelper.CreateSuccess(tracebufferList);
    }

    public SxExecuteRet<List<List<double>>> GetSensorSpeedX0X1Y0Y1WithSpeedTraceBufferList(bool isAxisX, TimeSpan timeSpan)
    {
        var calibrationRegList = new List<CalibrationRegEnum>
        {
            AdsTracebufferRegEnum.XY_X0.ToCalibrationRegEnum(),
            AdsTracebufferRegEnum.XY_X1.ToCalibrationRegEnum(),
            AdsTracebufferRegEnum.XY_Y0.ToCalibrationRegEnum(),
            AdsTracebufferRegEnum.XY_Y1.ToCalibrationRegEnum(),
            isAxisX
                ? AdsTracebufferRegEnum.ACS_X_Speed.ToCalibrationRegEnum()
                : AdsTracebufferRegEnum.ACS_Y_Speed.ToCalibrationRegEnum()
        };
        var sxExecuteRet = Invoke(() => Service?.GetADSTraceBuffByReg(new SxParamObj<(List<CalibrationRegEnum> regs, int time)>((calibrationRegList, Convert.ToInt32(timeSpan.TotalMilliseconds)))));

        if (sxExecuteRet.Anything.Count != 5
            || sxExecuteRet.Anything.Any(t => t.Count == 0)
            || sxExecuteRet.Anything[0].Count != sxExecuteRet.Anything[1].Count
            || sxExecuteRet.Anything[1].Count != sxExecuteRet.Anything[2].Count
            || sxExecuteRet.Anything[2].Count != sxExecuteRet.Anything[3].Count
            || sxExecuteRet.Anything[3].Count != sxExecuteRet.Anything[4].Count) return SxExecuteRetHelper.CreateError<List<List<double>>>("Ads error trans buffer is empty", []);

        // 将四个地址的tracebuffer数据合并成一个列表
        var tracebufferList = sxExecuteRet.Anything
            .Select(shortList => shortList.Select(Convert.ToDouble).ToList())
            .ToList();

        return SxExecuteRetHelper.CreateSuccess(tracebufferList);
    }

    public SxExecuteRet<bool> SetAdsXyEnabled(bool isEnabled)
    {
        var dataInfoList = new List<CgBoardDataInfo>
        {
            new() { Reg = CgBoardReg.REG2, Data = Convert.ToInt32(isEnabled) }
        };
        var sxExecuteRet = Invoke(() => Service?.SeADSReg(new SxParamObj<List<CgBoardDataInfo>>(dataInfoList)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }
}