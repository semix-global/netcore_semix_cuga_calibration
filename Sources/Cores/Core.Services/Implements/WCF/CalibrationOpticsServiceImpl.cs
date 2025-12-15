using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Data.DataStruct.Optics;
using Cuga.Engine.Interface;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationOpticsService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationOpticsServiceImpl : BaseService<ICgCalibrationService>, ICalibrationOpticsService
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

    public SxExecuteRet<double> GetRelayMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.OpticCommonReadPos(opticsIlluminationModeEnum switch
        {
            OpticsIlluminationModeEnum.OI => CgCommonType.OI_Relay,
            OpticsIlluminationModeEnum.NI => CgCommonType.NI_Relay,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
        }));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<double>(sxExecuteRet.ErrorMsg, 0);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<bool> SetRelayMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value)
    {
        var sxExecuteRet = Invoke(() => Service?.OpticCommonMove(opticsIlluminationModeEnum switch
        {
            OpticsIlluminationModeEnum.OI => CgCommonType.OI_Relay,
            OpticsIlluminationModeEnum.NI => CgCommonType.NI_Relay,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<CgCommonType>(nameof(opticsIlluminationModeEnum))
        }, value));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, false);

        return SxExecuteRetHelper.CreateSuccess(true);
    }
}