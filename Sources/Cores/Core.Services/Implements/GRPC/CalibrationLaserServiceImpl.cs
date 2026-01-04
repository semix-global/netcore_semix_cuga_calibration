using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Cuga.Agent.Facade.Service.MachineFacade;
using Cuga.Data.DataStruct.PMT;
using Cuga.Interface.Calibration;
using Cuga.Interface.Diagnosis;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;

namespace Core.Services.Implements.GRPC;

[IOCAppService(ServiceType = typeof(ICalibrationLaserService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed partial class CalibrationLaserServiceImpl(
    ICalibrationAlgorithmService calibrationAlgorithmService,
    ICalibrationStageService calibrationStageService,
    ICalibrationConfigService calibrationConfigService,
    CalibrationSetting calibrationSetting)
    : BaseService<ICgCalibLaserService, ICgDiagIlluminationOpticsService, ICgFacadeSwathService>, ICalibrationLaserService
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

    public SxExecuteRet<(Point PD1Point, Point PD2Point)> GetLaserBeamPoint()
    {
        var sxExecuteRet = Invoke(() => Service?.ReadLaserBeamPos());
        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, (Point.Origin, Point.Origin))
            : SxExecuteRetHelper.CreateSuccess((new Point(sxExecuteRet.Anything.PD_X_1_FPOS, sxExecuteRet.Anything.PD_Y_1_FPOS) * 1000, new Point(sxExecuteRet.Anything.PD_X_2_FPOS, sxExecuteRet.Anything.PD_Y_2_FPOS) * 1000));
    }

    public SxExecuteRet<(Point PD1Point, Point PD2Point)> GetLaserBeamOriginPoint()
    {
        var sxExecuteRet = Invoke(() => Service?.ReadLaserBeamOriginPos());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, (Point.Origin, Point.Origin))
            : SxExecuteRetHelper.CreateSuccess((new Point(sxExecuteRet.Anything.PD_X_1_FPOS, sxExecuteRet.Anything.PD_Y_1_FPOS) * 1000, new Point(sxExecuteRet.Anything.PD_X_2_FPOS, sxExecuteRet.Anything.PD_Y_2_FPOS) * 1000));
    }

    public SxExecuteRet<bool> AdjustBeamStabilizer(bool isEnable)
    {
        var sxExecuteRet = Invoke(() => Service?.LaserBeamAdjust(new SxParamObj<bool>(isEnable)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetOpticalMeasurePower()
    {
        var sxExecuteRet = Invoke(() => Service?.ReadDynamometer());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<double>(sxExecuteRet.Msg)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<double> GetOpticalMeasurePower(ProductivityInformation productivityInformation, double flatnessTime)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<LaserLightInformation>> GetLaserLightInformations()
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<LaserLightInformation> LevelToLaserLightInformation(double level)
    {
        var sxExecuteRet = GetLaserLightInformations();
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, LaserLightInformation.Default);

        var result = sxExecuteRet.Anything.SingleOrDefault(m => m.Level - level == 0);

        return result is null
            ? SxExecuteRetHelper.CreateError("Laser Light Information is not single", LaserLightInformation.Default)
            : SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<LaserLightInformation> CoefficientToLaserLightInformation(double coefficient)
    {
        var sxExecuteRet = GetLaserLightInformations();
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, LaserLightInformation.Default);

        var result = sxExecuteRet.Anything.SingleOrDefault(m => m.Coefficient - coefficient == 0);

        return result is null
            ? SxExecuteRetHelper.CreateError("Laser Light Information is not single", LaserLightInformation.Default)
            : SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<IReadOnlyList<ProductivityInformation>> GetProductivityInformations(OpticsIlluminationModeEnum opticsIlluminationModeEnum)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> ToggleOpticsMagType(OpticsIlluminationModeEnum opticsIlluminationModeEnum, OpticsMagTypeEnum opticsMagTypeEnum)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> ToggleOpticsMagType(ProductivityInformation productivityInformation)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum opticsAodWorkingModeEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.SetAODNO(new SxParamObj<int>(opticsAodWorkingModeEnum.ToOpticsAodWorkingMode())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetAODDelayValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, OpticsMagTypeEnum opticsMagTypeEnum, double prescanAODDelay, double chirpAODDelay)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetAODDelayValue(ProductivityInformation productivityInformation, double prescanAODDelay, double chirpAODDelay)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetDefaultPrescanAODWaveProfileByCoefficient(OpticsIlluminationModeEnum opticsIlluminationModeEnum, OpticsMagTypeEnum opticsMagTypeEnum, double coefficient)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetDefaultPrescanAODWaveProfileByCoefficient(ProductivityInformation productivityInformation, double coefficient)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetPrescanAODWaveProfiles(OpticsIlluminationModeEnum opticsIlluminationModeEnum, IReadOnlyList<PrescanAODWaveformProfile> prescanAODWaveProfiles)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetDefaultChirpAODWaveProfile(OpticsIlluminationModeEnum opticsIlluminationModeEnum, OpticsMagTypeEnum opticsMagTypeEnum)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetDefaultChirpAODWaveProfile(ProductivityInformation productivityInformation)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetChirpAODWaveProfiles(OpticsIlluminationModeEnum opticsIlluminationModeEnum, IReadOnlyList<ChirpAODWaveformProfile> chirpAODWaveProfiles)
    {
        throw new NotImplementedException();
    }


    public SxExecuteRet<bool> ToggleCIBControlTypeAndProfileType(CIBConfiguration cIbConfiguration, int pmtId, int channelId)
    {
        var toggleAutoGainRet = ToggleEnableAutoGainControl(cIbConfiguration.IsAutoGainControl, pmtId, channelId);
        if (toggleAutoGainRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(toggleAutoGainRet.ErrorMsg, false);

        if (cIbConfiguration.IsAutoGainControl == false)
        {
            var setGainRet = SetGain(cIbConfiguration.Gain, pmtId, channelId);
            if (setGainRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(setGainRet.ErrorMsg, false);
        }

        var toggleL0KRet = ToggleEnableL0K(cIbConfiguration.IsL0K, pmtId, channelId);
        if (toggleL0KRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(toggleL0KRet.ErrorMsg, false);

        var toggleProfileTypeRet = ToggleProfileMode(cIbConfiguration.CIBProfileMode, pmtId, channelId);
        if (toggleProfileTypeRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(toggleProfileTypeRet.ErrorMsg, false);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableAutoGainControl(bool enable, int pmtId, int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> ToggleProfileMode(CIBProfileModeEnum cibProfileModeEnum, int pmtId, int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> ToggleEnableMarkMode(bool enable, int pmtId, int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> ToggleEnableL0K(bool enable, int pmtId, int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetGain(IReadOnlyList<CIBInformation> cibInformations, double gain)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<bool> SetGain(double gain, int pmtId, int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<CIBInformation>> GetCIBInformations()
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<(int PmtId, bool IsUsed, IReadOnlyList<int> ChannelIdList)>> GetCIBConfigList()
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<IReadOnlyList<double>>> GetCIBOfPMTDataList(int count, int pmtId, int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<IReadOnlyList<double>>> GetCIBOfSenseDataList(int count, int pmtId, int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<CIBDelayDTO>> GetCIBDelayList()
    {
        var pmtRet = Invoke(() => Service?.GetPMTDelay());
        if (pmtRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<CIBDelayDTO>>(pmtRet.ErrorMsg, []);

        var result = new List<CIBDelayDTO>(pmtRet.Anything.Count);
        result.AddRange(pmtRet.Anything.Select(pmtDelayModel => new CIBDelayDTO().AdaptIn(pmtDelayModel)));

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<CIBDelayDTO>>(result);
    }

    public SxExecuteRet<bool> SetCIBDelayList(IReadOnlyList<CIBDelayDTO> darkFieldPmtDelayDtoList)
    {
        var pmtDelayModel = darkFieldPmtDelayDtoList.Select(item => item.AdaptTo()).ToList();

        var pmtRet = Invoke(() => Service?.SetPMTDelay(new SxParamObj<List<CgPMTDelayModel>>(pmtDelayModel)));

        return pmtRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(pmtRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetCIBChirp(IReadOnlyList<double> gainList, int pmtId, int channelId)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<(double Ecs, double AfMotor)> RuntimeAfCalibration(
        CalChipSiteModelEnum calChipSiteModelEnum,
        int pmtId,
        double? coefficient = null,
        Point? point = null)
    {
        throw new NotImplementedException();
    }

    [Obsolete]
    public SxExecuteRet<List<DarkFieldImageDTO>> GetDarkFieldLineScanImageList(Point position,
        int xWidthPixel,
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<List<DarkFieldImageDTO>> GetDarkFieldLineScanImageList(Point position,
        int xWidthPixel,
        ProductivityInformation productivityInformation,
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward)
    {
        throw new NotImplementedException();
    }

    [Obsolete]
    public SxExecuteRet<List<DarkFieldRawScanImageDTO>> GetDarkFieldLineScanImageList(
        Point startPosition,
        Point endPosition,
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<List<DarkFieldRawScanImageDTO>> GetDarkFieldLineScanImageList(
        Point startPosition,
        Point endPosition,
        ProductivityInformation productivityInformation,
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<List<List<DarkFieldImageDTO>>> GetChuckDarkFieldRowLineScanImageList(List<Point> machinePositionList, int xWidthPixel, double xPixelSize, OpticsMagTypeEnum opticsMagTypeEnum, StageSpeedEnum xStageSpeedEnum, OpticsIlluminationModeEnum opticsIlluminationModeEnum, int pmtId, StageCoordinateSystemEnum stageCoordinateSystemEnum, bool isAutoFocus)
    {
        throw new NotImplementedException();
    }


    public SxExecuteRet<List<List<DarkFieldImageDTO>>> GetChuckDarkFieldRowLineScanImageList(
        List<Point> machinePositionList,
        int xWidthPixel,
        double xPixelSize,
        ProductivityInformation productivityInformation,
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<double> ReadDOECurrentAngle()
    {
        return SxExecuteRetHelper.CreateSuccess(0d);
    }

    public SxExecuteRet<bool> SetDOEAngle(double angle)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }
}