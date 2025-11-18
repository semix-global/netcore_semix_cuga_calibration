using Core.Models.Helper;
using Core.Services.Interfaces;
using Cuga.Interface.Diagnosis;
using HalconDotNet;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Semix.CoreLib;

namespace Core.Services.Implements.GRPC;

[IOCAppService(ServiceType = typeof(ICalibrationFourierService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationFourierServiceImpl : BaseService<ICgDiagFourierOpticsService>, ICalibrationFourierService
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

    public SxExecuteRet<HImage> GetFourierImage(int channelId)
    {
        throw new NotImplementedException();
    }
}