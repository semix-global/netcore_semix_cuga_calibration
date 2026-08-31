using Core.Models.Helper;
using Core.Services.Interfaces;
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
        var sxExecuteRet = Invoke(() => Service?.SetXSpeedFeed(new SxParamObj<(CgSpeedLevelType speed, bool isPositive, (ushort X1, ushort X2) value)>((CgSpeedLevelType.Low, isPositive, (Convert.ToUInt16(value.X1), Convert.ToUInt16(value.X2))))));

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
        var sxExecuteRet = Invoke(() =>
            Service?.SetYSpeedFeed(new SxParamObj<(CgSpeedLevelType speed, bool isPositive, (ushort Y1, ushort Y2, ushort Y3) value)>((CgSpeedLevelType.Low, isPositive,
                (Convert.ToUInt16(value.Y1), Convert.ToUInt16(value.Y2), Convert.ToUInt16(value.Y3))))));

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
        throw new NotImplementedException();
    }

    public Task<SxExecuteRet<List<(double Height, double Roll, double Pitch, double xSpeed, double ySpeed)>>> GetSensorHeightRollPitchTraceBufferListAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<SxExecuteRet<(List<double> Z_ECS0, List<double> Z_ECS1, List<double> Z_ECS2, List<double> Height, List<double> Roll, List<double> Pitch, List<double> X_Speed, List<double> Y_Speed)>> GetSensorSpeedZ1Z2Z3TraceBufferListAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<SxExecuteRet<(List<double> X0, List<double> X1, List<double> Y0, List<double> Y1, List<double> Speed)>> GetSensorSpeedX0X1Y0Y1WithSpeedTraceBufferListAsync(bool isAxisX, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
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