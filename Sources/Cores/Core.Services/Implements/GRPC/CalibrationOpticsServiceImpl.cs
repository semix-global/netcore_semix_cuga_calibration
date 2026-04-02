using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Optics;
using Cuga.Interface.Diagnosis;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;

namespace Core.Services.Implements.GRPC;

[IOCAppService(ServiceType = typeof(ICalibrationOpticsService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationOpticsServiceImpl : BaseService<ICgDiagIlluminationOpticsService>, ICalibrationOpticsService
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

    public SxExecuteRet<IReadOnlyList<ProductivityInformation>> GetProductivityInformations()
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<double> GetDOEMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetDOEMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<double> GetRelayMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetRelayMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<double> GetINCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetINCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, double value)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<(double L1, double L3)> GetSCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetSCMotorAbsoluteValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, (double L1, double L3) value)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<double> GetCollectorPolarizationMotorAbsoluteValue(int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetCollectorPolarizationMotorAbsoluteValue(int channelId, double value)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> ToggleODFilter(bool isEnable)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<OpticsApodizationModeEnum> GetApodizationMode()
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetApodizationMode(OpticsApodizationModeEnum opticsApodizationModeEnum)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<OpticsPolarizationModeEnum> GetPolarizationMode()
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetPolarizationMode(OpticsPolarizationModeEnum opticsPolarizationModeEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.SetPolarization(new SxParamObj<CgPolarizationTypeEnum>(opticsPolarizationModeEnum.ToCgPolarizationTypeEnum())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<OpticsCollectorPolarizationModeEnum> GetCollectorPolarizationMode()
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetCollectorPolarizationMode(OpticsCollectorPolarizationModeEnum opticsCollectorPolarizationModeEnum)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<OpticsCollectorPolarizationModeEnum> GetCollectorPolarizationMode(int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetCollectorPolarizationMode(int channelId, OpticsCollectorPolarizationModeEnum opticsCollectorPolarizationModeEnum)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> ClinderEXC(OpticsYGhostModeEnum type, bool status)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }
}