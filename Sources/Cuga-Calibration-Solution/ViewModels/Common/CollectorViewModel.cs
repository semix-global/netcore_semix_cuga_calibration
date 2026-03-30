using Core.Models.Enums.Optics;
using Core.Models.Exceptions;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(CollectorViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class CollectorViewModel(
    ICalibrationCollectorService calibrationCollectorService) : ViewModelBase
{
    public bool Connect()
    {
        var ret = calibrationCollectorService.Connect();

        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public OpticsCollectorPolarizationModeEnum GetPolarizationMode()
    {
        var ret = calibrationCollectorService.GetPolarizationMode();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void SetPolarizationMode(OpticsCollectorPolarizationModeEnum opticsCollectorPolarizationModeEnum)
    {
        var ret = calibrationCollectorService.SetPolarizationMode(opticsCollectorPolarizationModeEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }
}