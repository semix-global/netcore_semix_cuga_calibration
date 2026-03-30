using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Data.DataStruct.Optics;
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
        var sxExecuteRet = Invoke(() => Service?.ReadNDFType(CgNDFCHEnum.ALL));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<CollectorPolarizationModeEnum>(sxExecuteRet.ErrorMsg, default);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.ToCollectorPolarizationModeEnum());
    }

    public SxExecuteRet<bool> SetPolarizationMode(CollectorPolarizationModeEnum collectorPolarizationModeEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.SetNDF(CgNDFCHEnum.ALL, collectorPolarizationModeEnum.ToCgNDFTypeEnum()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }
}