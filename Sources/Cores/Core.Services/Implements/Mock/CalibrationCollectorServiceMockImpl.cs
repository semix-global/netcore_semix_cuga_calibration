using Core.Models.Enums.Collector;
using Core.Models.Helper;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationCollectorService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationCollectorServiceMockImpl : ICalibrationCollectorService
{
    private CollectorPolarizationModeEnum _currentCollectorPolarizationModeEnum;

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<CollectorPolarizationModeEnum> GetPolarizationMode()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(_currentCollectorPolarizationModeEnum);
    }

    public SxExecuteRet<bool> SetPolarizationMode(CollectorPolarizationModeEnum collectorPolarizationModeEnum)
    {
        Thread.Sleep(100);

        _currentCollectorPolarizationModeEnum = collectorPolarizationModeEnum;

        return SxExecuteRetHelper.CreateSuccess(true);
    }
}