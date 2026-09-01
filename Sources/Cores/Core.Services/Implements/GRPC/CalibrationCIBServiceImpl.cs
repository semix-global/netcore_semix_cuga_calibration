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

    public SxExecuteRet<IReadOnlyList<bool>> GetAGC(IReadOnlyList<CIBInformation> cibInformations)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetAGC(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<CIBProfileModeEnum>> GetCIBProfileModeEnum(IReadOnlyList<CIBInformation> cibInformations)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetCIBProfileModeEnum(IReadOnlyList<CIBInformation> cibInformations, CIBProfileModeEnum cibProfileModeEnum)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<bool>> GetL0K(IReadOnlyList<CIBInformation> cibInformations)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetL0K(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<bool>> GetMarker(IReadOnlyList<CIBInformation> cibInformations)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetMarker(IReadOnlyList<CIBInformation> cibInformations, bool enable)
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

    public SxExecuteRet<bool> SetXPixelSize(ProductivityInformation productivityInformation, double xPixelSize)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<IReadOnlyList<CIBMMDGainRelationshipDTO>>> GetCIBMMDGains(IReadOnlyList<CIBInformation> cibInformations, double startGain, double stepGain, double stopGain)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetGlobalRTFCParams(ProductivityInformation productivityInformation)
    {
        throw new NotImplementedException();
    }

    public Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(ProductivityInformation productivityInformation, StageCoordinateSystemEnum stageCoordinateSystemEnum, Point centerPosition, int imageWidth, IReadOnlyList<CIBInformation> cibInformations, bool isForward, bool isAutoFocus, bool isKeepRawImageCIBProfileModeEnum, bool isCustomAFParam, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(ProductivityInformation productivityInformation, StageCoordinateSystemEnum stageCoordinateSystemEnum, IReadOnlyList<Point> centerPositions, int imageWidth, CIBInformation cibInformation, bool isAutoFocus, bool isKeepRawImageCIBProfileModeEnum, bool isCustomAFParam, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<SxExecuteRet<IReadOnlyList<DarkFieldRawScanImageDTO>>> GetPMTImagesAsync(ProductivityInformation productivityInformation, StageCoordinateSystemEnum stageCoordinateSystemEnum, Point startPosition, Point stopPosition, IReadOnlyList<CIBInformation> cibInformations, bool isForward, bool isAutoFocus, bool isKeepRawImageCIBProfileModeEnum, bool isCustomAFParam, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(ProductivityInformation productivityInformation, StageCoordinateSystemEnum stageCoordinateSystemEnum, Point startPosition, Point stopPosition, double startECS, double stopECS, IReadOnlyList<CIBInformation> cibInformations, bool isForward, bool isKeepRawImageCIBProfileModeEnum, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<(double ECS, double Motor, bool isAFServo)> RuntimeAFCalibration(CalChipSiteModelEnum calChipSiteModelEnum, ProductivityInformation productivityInformation, CIBInformation cibInformation, Point? centerMachinePosition = null, LaserLightInformation? laserLightInformation = null)
    {
        throw new NotImplementedException();
    }
}