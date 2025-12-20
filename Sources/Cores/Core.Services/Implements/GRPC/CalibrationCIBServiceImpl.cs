using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Interface.Diagnosis;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;

namespace Core.Services.Implements.GRPC;

[IOCAppService(ServiceType = typeof(ICalibrationCIBService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationCIBServiceImpl : BaseService<ICgDiagIlluminationOpticsService>, ICalibrationCIBService
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

    public SxExecuteRet<bool> SetMMD(CIBInformation cibInformation, IReadOnlyList<double> logGainMul128U12Bits, IReadOnlyList<double> gainS16Bits, double maxLogGain)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetLightMatching(CIBInformation cibInformation, double digitalGainPlusMultiplicativeFactors)
    {
        throw new NotImplementedException();
    }

    public Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDto>>> GetPMTValuesAsync(
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point position,
        IReadOnlyList<CIBInformation> cibInformations,
        int imageWidth,
        bool isAutoFocus,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}