using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationCollectorService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationCollectorServiceMockImpl : ICalibrationCollectorService
{
    private OpticsCollectorPolarizationModeEnum _currentOpticsCollectorPolarizationModeEnum;

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<OpticsCollectorPolarizationModeEnum> GetPolarizationMode()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(_currentOpticsCollectorPolarizationModeEnum);
    }

    public SxExecuteRet<bool> SetPolarizationMode(OpticsCollectorPolarizationModeEnum opticsCollectorPolarizationModeEnum)
    {
        Thread.Sleep(100);

        _currentOpticsCollectorPolarizationModeEnum = opticsCollectorPolarizationModeEnum;

        return SxExecuteRetHelper.CreateSuccess(true);
    }
}