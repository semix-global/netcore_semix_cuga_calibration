using Core.Models.Enums.Collector;
using Core.Models.Helper;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Engine.Interface;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationCollectorService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationCollectorServiceImpl : BaseService<ICgCalibrationService>, ICalibrationCollectorService
{
    public SxExecuteRet<bool> Connect()
    {
        if (IsConnected) return SxExecuteRetHelper.CreateSuccess(true);

        return Invoke(() =>
        {
            var ep = new SxWcfEndPoint("127.0.0.1", 80, CgInernalAddr.CalAddr);
            var createService = CreateService(ep);
            IsConnected = createService.IsSuccess;
            return createService;
        }, false);
    }

    public SxExecuteRet<CollectorPolarizationModeEnum> GetPolarizationMode()
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetPolarizationMode(CollectorPolarizationModeEnum collectorPolarizationModeEnum)
    {
        throw new NotImplementedException();
    }
}