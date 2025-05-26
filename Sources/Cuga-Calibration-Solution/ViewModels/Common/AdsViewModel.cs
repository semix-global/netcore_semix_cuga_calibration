using Core.Models.Exceptions;
using Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(AdsViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class AdsViewModel(
    ICalibrationAdsService calibrationAdsService,
    ILogger<AdsViewModel> logger) : ViewModelBase
{
    #region 服务

    public bool Connect()
    {
        var ret = calibrationAdsService.Connect();

        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public (double X1, double X2) GetSensorXSpeedFeedForwardValue(bool isPositive)
    {
        var ret = calibrationAdsService.GetSensorXSpeedFeedForwardValue(isPositive);
        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetSensorXSpeedFeedForwardValue(bool isPositive, (double X1, double X2) value)
    {
        var ret = calibrationAdsService.SetSensorXSpeedFeedForwardValue(isPositive, value);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public (double Y1, double Y2, double Y3) GetSensorYSpeedFeedForwardValue(bool isPositive)
    {
        var ret = calibrationAdsService.GetSensorYSpeedFeedForwardValue(isPositive);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetSensorYSpeedFeedForwardValue(bool isPositive, (double Y1, double Y2, double Y3) value)
    {
        var ret = calibrationAdsService.SetSensorYSpeedFeedForwardValue(isPositive, value);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public (double Z1, double Z2, double Z3) GetSensorSpeedZ1Z2Z3Value()
    {
        var ret = calibrationAdsService.GetSensorSpeedZ1Z2Z3Value();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetSensorFeedForwardPressureValue(double pressureValue1, double pressureValue2, double pressureValue3)
    {
        var ret = calibrationAdsService.SetSensorFeedForwardPressureValue(pressureValue1, pressureValue2, pressureValue3);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public List<(double PressureValue1, double PressureValue2, double PressureValue3)> GetSensorAllPressureTraceBufferList(TimeSpan timeSpan)
    {
        logger.LogInformation("Start TraceBuffer");
        var ret = calibrationAdsService.GetSensorAllPressureTraceBufferList(timeSpan);
        logger.LogInformation("End TraceBuffer");

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public List<(double Height, double Roll, double Pitch)> GetSensorHeightRollPitchTraceBufferList(TimeSpan timeSpan, int repeatCount = 0)
    {
        logger.LogInformation("Start TraceBuffer");
        var ret = calibrationAdsService.GetSensorHeightRollPitchTraceBufferList(timeSpan);
        logger.LogInformation("End TraceBuffer");

        if (repeatCount == 0 || repeatCount == 5) return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
        else return ret.IsSuccess ? ret.Anything : [];
    }

    public List<List<double>> GetSensorSpeedZ1Z2Z3TraceBufferList(TimeSpan timeSpan, int repeatCount = 0)
    {
        logger.LogInformation("Start TraceBuffer");
        var ret = calibrationAdsService.GetSensorSpeedZ1Z2Z3TraceBufferList(timeSpan);
        logger.LogInformation("End TraceBuffer");
        if (repeatCount == 0 || repeatCount == 5) return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
        else return ret.IsSuccess ? ret.Anything : [];
    }

    public List<List<double>> GetSensorSpeedX0X1Y0Y1WithSpeedTraceBufferList(bool isAxisX, TimeSpan timeSpan)
    {
        logger.LogInformation("Start TraceBuffer");
        var ret = calibrationAdsService.GetSensorSpeedX0X1Y0Y1WithSpeedTraceBufferList(isAxisX, timeSpan);
        logger.LogInformation("End TraceBuffer");

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetAdsXyEnabled(bool isEnabled)
    {
        var ret = calibrationAdsService.SetAdsXyEnabled(isEnabled);
        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    #endregion 服务
}