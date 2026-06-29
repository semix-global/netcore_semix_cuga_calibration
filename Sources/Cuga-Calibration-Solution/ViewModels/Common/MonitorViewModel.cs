using Core.Models.Exceptions;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(MonitorViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class MonitorViewModel(ICalibrationMonitorService calibrationMonitorService) : ViewModelBase
{
    #region 服务

    public bool Connect()
    {
        var ret = calibrationMonitorService.Connect();

        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public (double reviewCam, double cib, double xAxis, double yAixs) GetHardwareTemperature()
    {
        var retCam = calibrationMonitorService.GetReviewCameraCurrentTemperature();
        if (retCam.IsSuccess == false) throw new CugaException(retCam.ErrorMsg);

        var retCib = calibrationMonitorService.GetCIBCurrentTemperature();
        if (retCam.IsSuccess == false) throw new CugaException(retCam.ErrorMsg);

        var retXAxis = calibrationMonitorService.GetXAxisCurrentTemperature();
        if (retXAxis.IsSuccess == false) throw new CugaException(retXAxis.ErrorMsg);

        var retYAxis = calibrationMonitorService.GetYAxisCurrentTemperature();
        if (retYAxis.IsSuccess == false) throw new CugaException(retYAxis.ErrorMsg);

        return (retCam.Anything, retCib.Anything, retXAxis.Anything, retYAxis.Anything);
    }

    #endregion 服务
}