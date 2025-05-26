using Core.Models.Helper;
using Core.Services.Interfaces;
using Cuga.Interface.Calibration;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;

namespace Core.Services.Implements.GRPC;

[IOCAppService(ServiceType = typeof(ICalibrationMonitorService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationMonitorServiceImpl : BaseService<ICgCalibMonitorService>, ICalibrationMonitorService
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

    public SxExecuteRet<double> GetCIBCurrentTemperature()
    {
        var sxExecuteRet = Invoke(() => Service?.GetCIBCurrentTemperature());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<double>(sxExecuteRet.Msg, 0)
            : SxExecuteRetHelper.CreateSuccess(Convert.ToDouble(sxExecuteRet.Anything));
    }

    public SxExecuteRet<double> GetReviewCameraCurrentTemperature()
    {
        var sxExecuteRet = Invoke(() => Service?.GetReviewCameraCurrentTemperature());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<double>(sxExecuteRet.Msg, 0)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<double> GetXAxisCurrentTemperature()
    {
        var sxExecuteRet = Invoke(() => Service?.GetXAxisCurrentTemperature());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<double>(sxExecuteRet.Msg, 0)
            : SxExecuteRetHelper.CreateSuccess(Convert.ToDouble(sxExecuteRet.Anything));
    }

    public SxExecuteRet<double> GetYAxisCurrentTemperature()
    {
        var sxExecuteRet = Invoke(() => Service?.GetYAxisCurrentTemperature());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<double>(sxExecuteRet.Msg, 0)
            : SxExecuteRetHelper.CreateSuccess(Convert.ToDouble(sxExecuteRet.Anything));
    }
}