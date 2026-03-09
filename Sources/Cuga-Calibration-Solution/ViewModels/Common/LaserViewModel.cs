using Core.Models.Enums.Optics;
using Core.Models.Exceptions;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.WPF.MVVM.ViewModels.Bases;

namespace CugaCalibration.ViewModels.Common;

[IOCAppService(ServiceType = typeof(LaserViewModel), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton)]
public sealed class LaserViewModel(
    ICalibrationLaserService calibrationLaserService) : ViewModelBase
{
    #region 服务

    public bool Connect()
    {
        var ret = calibrationLaserService.Connect();

        return ret.IsSuccess ? true : throw new CugaException(ret.ErrorMsg);
    }

    public (Point PD1Point, Point PD2Point) GetLaserBeamPoint()
    {
        var ret = calibrationLaserService.GetLaserBeamPoint();

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);

        return (ret.Anything.PD1Point, ret.Anything.PD2Point);
    }

    public (Point PD1Point, Point PD2Point) GetLaserBeamOriginPoint()
    {
        var ret = calibrationLaserService.GetLaserBeamOriginPoint();

        return ret.IsSuccess ? (ret.Anything.PD1Point, ret.Anything.PD2Point) : throw new CugaException(ret.ErrorMsg);
    }

    public void AdjustBeamStabilizer(bool isEnable)
    {
        var ret = calibrationLaserService.AdjustBeamStabilizer(isEnable);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public double GetOpticalMeasurePower()
    {
        var ret = calibrationLaserService.GetOpticalMeasurePower();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public IReadOnlyList<LaserLightInformation> GetLaserLightInformations()
    {
        var ret = calibrationLaserService.GetLaserLightInformations();

        return ret.IsSuccess ? ret.Anything : throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleOpticsMagType(ProductivityInformation productivityInformation)
    {
        var ret = calibrationLaserService.ToggleOpticsMagType(productivityInformation);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum opticsAODWorkingModeEnum)
    {
        var ret = calibrationLaserService.ToggleOpticsAODWorkingMode(opticsAODWorkingModeEnum);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetAODDelayValue(ProductivityInformation productivityInformation, double prescanAODDelay, double chirpAODDelay)
    {
        var ret = calibrationLaserService.SetAODDelayValue(productivityInformation, prescanAODDelay, chirpAODDelay);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetPrescanAODWaveProfileByCoefficient(ProductivityInformation productivityInformation, double coefficient)
    {
        var ret = calibrationLaserService.SetDefaultPrescanAODWaveProfileByCoefficient(productivityInformation, coefficient);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetPrescanAODWaveProfiles(OpticsIlluminationModeEnum opticsIlluminationModeEnum, IReadOnlyList<PrescanAODWaveformProfile> prescanAODWaveProfiles)
    {
        var ret = calibrationLaserService.SetPrescanAODWaveProfiles(opticsIlluminationModeEnum, prescanAODWaveProfiles);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetChirpAODWaveProfile(ProductivityInformation productivityInformation)
    {
        var ret = calibrationLaserService.SetDefaultChirpAODWaveProfile(productivityInformation);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    public void SetChirpAODWaveProfiles(OpticsIlluminationModeEnum opticsIlluminationModeEnum, IReadOnlyList<ChirpAODWaveformProfile> chirpAODWaveProfiles)
    {
        var ret = calibrationLaserService.SetChirpAODWaveProfiles(opticsIlluminationModeEnum, chirpAODWaveProfiles);

        if (ret.IsSuccess == false) throw new CugaException(ret.ErrorMsg);
    }

    #endregion 服务
}