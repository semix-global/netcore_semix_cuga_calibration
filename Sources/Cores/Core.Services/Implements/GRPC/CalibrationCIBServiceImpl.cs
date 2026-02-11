using Core.Models.Enums.CIB;
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

    public SxExecuteRet<IReadOnlyList<CIBInformation>> GetCIBInformations()
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> ToggleEnableAGC(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> ToggleProfileMode(IReadOnlyList<CIBInformation> cibInformations, CIBProfileModeEnum cibProfileModeEnum)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> ToggleEnableL0K(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetGain(IReadOnlyList<CIBInformation> cibInformations, double gain)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetMMD(CIBInformation cibInformation, IReadOnlyList<double> logGainMul128U12Bits, IReadOnlyList<double> gainS16Bits)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetLightMatching(IReadOnlyList<CIBInformation> cibInformations, double digitalGainPlusMultiplicativeFactors)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetIlluminationProfile(IReadOnlyList<CIBInformation> cibInformations, IReadOnlyList<double> illuminationProfiles)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<CIBDelayDTO>> GetDelays(IReadOnlyList<CIBInformation> cibInformations)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetDelays(IReadOnlyList<CIBDelayDTO> delays)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<IReadOnlyList<CIBMMDGainRelationshipDTO>>> GetCIBMMDGains(IReadOnlyList<CIBInformation> cibInformations, double startGain, double stepGain, double stopGain)
    {
        throw new NotImplementedException();
    }

    public Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(ProductivityInformation productivityInformation, StageCoordinateSystemEnum stageCoordinateSystemEnum, Point position, IReadOnlyList<CIBInformation> cibInformations, int imageWidth,
        bool isForward, bool isAutoFocus, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<SxExecuteRet<IReadOnlyList<DarkFieldRawScanImageDTO>>> GetPMTImagesAsync(ProductivityInformation productivityInformation, StageCoordinateSystemEnum stageCoordinateSystemEnum, Point startPosition, Point endPosition,
        IReadOnlyList<CIBInformation> cibInformations, bool isForward, bool isAutoFocus, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(ProductivityInformation productivityInformation, StageCoordinateSystemEnum stageCoordinateSystemEnum, Point startPosition, Point endPosition,
        IReadOnlyList<CIBInformation> cibInformations, double startECS, double stopECS, bool isForward, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}