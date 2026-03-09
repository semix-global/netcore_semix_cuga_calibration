using Core.Models.Helper;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationMonitorService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationMonitorServiceMockImpl : ICalibrationMonitorService
{
    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetCIBCurrentTemperature()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(Random.Shared.NextDouble());
    }

    public SxExecuteRet<double> GetReviewCameraCurrentTemperature()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(Random.Shared.NextDouble());
    }

    public SxExecuteRet<double> GetXAxisCurrentTemperature()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(Random.Shared.NextDouble());
    }

    public SxExecuteRet<double> GetYAxisCurrentTemperature()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(Random.Shared.NextDouble());
    }
}