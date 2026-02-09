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

    public SxExecuteRet<(double StartPos, double EndPos, double Accuracy)> GetDOEMotorRouteRange(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
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
}