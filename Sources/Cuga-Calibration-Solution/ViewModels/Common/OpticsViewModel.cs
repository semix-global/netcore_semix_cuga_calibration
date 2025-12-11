using Core.Models.Enums.Optics;
using Core.Models.Exceptions;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(OpticsViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class OpticsViewModel(
    ICalibrationOpticsService calibrationOpticsService) : ViewModelBase
{
    public bool Connect()
    {
        var ret = calibrationOpticsService.Connect();

        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public double GetRelayMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        var ret = calibrationOpticsService.GetRelayMotorAbsoluteValue(opticsIlluminationModeEnum);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetRelayMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value)
    {
        var ret = calibrationOpticsService.SetRelayMotorAbsoluteValue(opticsIlluminationModeEnum, value);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }
}