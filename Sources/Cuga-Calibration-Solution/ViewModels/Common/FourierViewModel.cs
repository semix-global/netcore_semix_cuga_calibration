using Core.Models.Exceptions;
using Core.Services.Interfaces;
using HalconDotNet;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(FourierViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class FourierViewModel(
    ICalibrationFourierService calibrationFourierService) : ViewModelBase
{
    public bool Connect()
    {
        var ret = calibrationFourierService.Connect();

        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public HImage GetFourierImage(int channelId)
    {
        var ret = calibrationFourierService.GetFourierImage(channelId);

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }
}