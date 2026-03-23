using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.PMT;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationLaserService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationLaserServiceMockImpl(
    ICalibrationConfigService calibrationConfigService) : ICalibrationLaserService
{
    private Point _curPosition = new(0, 0);
    private double _coefficient = 1;

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(Point PD1Point, Point PD2Point)> GetLaserBeamPoint()
    {
        Thread.Sleep(100);

        _curPosition = new Point(Random.Shared.Next(240, 300), Random.Shared.Next(240, 300));

        return SxExecuteRetHelper.CreateSuccess((_curPosition, _curPosition));
    }

    public SxExecuteRet<(Point PD1Point, Point PD2Point)> GetLaserBeamOriginPoint()
    {
        Thread.Sleep(100);

        _curPosition = new Point(Random.Shared.Next(240, 300), Random.Shared.Next(240, 300));

        return SxExecuteRetHelper.CreateSuccess((_curPosition, _curPosition));
    }

    public SxExecuteRet<bool> AdjustBeamStabilizer(bool isEnable)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetOpticalMeasurePower()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(Convert.ToDouble(Random.Shared.Next(30, 60) * _coefficient));
    }

    public SxExecuteRet<IReadOnlyList<LaserLightInformation>> GetLaserLightInformations()
    {
        Thread.Sleep(100);

        var laserLightInformations = new[]
        {
            LaserLightInformation.Default.Clone().AdaptIn(new CgLightConfig { LightProp = 170, LightCoeff = 0.85 }),
            LaserLightInformation.Default.Clone().AdaptIn(new CgLightConfig { LightProp = 157, LightCoeff = 0.785 }),
            LaserLightInformation.Default.Clone().AdaptIn(new CgLightConfig { LightProp = 127, LightCoeff = 0.635 }),
            LaserLightInformation.Default.Clone().AdaptIn(new CgLightConfig { LightProp = 99, LightCoeff = 0.495 }),
            LaserLightInformation.Default.Clone().AdaptIn(new CgLightConfig { LightProp = 78, LightCoeff = 0.39 }),
            LaserLightInformation.Default.Clone().AdaptIn(new CgLightConfig { LightProp = 67, LightCoeff = 0.335 }),
            LaserLightInformation.Default.Clone().AdaptIn(new CgLightConfig { LightProp = 52, LightCoeff = 0.26 }),
            LaserLightInformation.Default.Clone().AdaptIn(new CgLightConfig { LightProp = 38, LightCoeff = 0.19 }),
            LaserLightInformation.Default.Clone().AdaptIn(new CgLightConfig { LightProp = 24, LightCoeff = 0.12 }),
            LaserLightInformation.Default.Clone().AdaptIn(new CgLightConfig { LightProp = 14, LightCoeff = 0.07 }),
            LaserLightInformation.Default.Clone().AdaptIn(new CgLightConfig { LightProp = 10, LightCoeff = 0.05 }),
            LaserLightInformation.Default.Clone().AdaptIn(new CgLightConfig { LightProp = 7, LightCoeff = 0.035 }),
            LaserLightInformation.Default.Clone().AdaptIn(new CgLightConfig { LightProp = 1, LightCoeff = 0.005 })
        };

        Guard.IsTrue(laserLightInformations.DistinctBy(t => t).Count() == laserLightInformations.Length, "Laser Light Information is not unique");

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<LaserLightInformation>>([.. laserLightInformations.OrderBy(t => t)]);
    }

    public SxExecuteRet<double> GetLaserLightSaturationCoefficient()
    {
        return SxExecuteRetHelper.CreateSuccess(1d);
    }

    public SxExecuteRet<bool> SetLaserLightSaturationCoefficient(double coefficient)
    {
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleOpticsMagType(ProductivityInformation productivityInformation)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum opticsAODWorkingModeEnum)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetAODDelayValue(ProductivityInformation productivityInformation, double prescanAODDelay, double chirpAODDelay)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetDefaultPrescanAODWaveProfileByCoefficient(ProductivityInformation productivityInformation, double coefficient)
    {
        var sxExecuteRetByGetPrescanAODWaveProfiles = calibrationConfigService.GetPrescanAODWaveProfiles(productivityInformation);
        if (sxExecuteRetByGetPrescanAODWaveProfiles.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRetByGetPrescanAODWaveProfiles.Msg, false);

        var prescanAODWaveProfiles = sxExecuteRetByGetPrescanAODWaveProfiles.Anything;

        foreach (var aodWaveProfile in prescanAODWaveProfiles) aodWaveProfile.ApplyCoefficient(coefficient);

        var sxExecuteRetBySetPrescanAODWaveProfiles = SetPrescanAODWaveProfiles(productivityInformation.OpticsIlluminationModeEnum, prescanAODWaveProfiles);

        var isSuccess = sxExecuteRetBySetPrescanAODWaveProfiles.IsSuccess;
        if (isSuccess) _coefficient = coefficient;

        return isSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRetBySetPrescanAODWaveProfiles.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetPrescanAODWaveProfiles(OpticsIlluminationModeEnum opticsIlluminationModeEnum, IReadOnlyList<PrescanAODWaveformProfile> prescanAODWaveProfiles)
    {
        Guard.IsNotEmpty(prescanAODWaveProfiles);

        foreach (var aodWaveProfile in prescanAODWaveProfiles)
        {
            Guard.IsNotEmpty(aodWaveProfile.Bytes);
        }

        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetDefaultChirpAODWaveProfile(ProductivityInformation productivityInformation)
    {
        var sxExecuteRetByGetChirpAODWaveProfiles = calibrationConfigService.GetChirpAODWaveProfiles(productivityInformation);
        if (sxExecuteRetByGetChirpAODWaveProfiles.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRetByGetChirpAODWaveProfiles.Msg, false);

        var sxExecuteRetBySetPrescanAODWaveProfiles = SetChirpAODWaveProfiles(productivityInformation.OpticsIlluminationModeEnum, sxExecuteRetByGetChirpAODWaveProfiles.Anything);

        return sxExecuteRetBySetPrescanAODWaveProfiles.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRetBySetPrescanAODWaveProfiles.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetChirpAODWaveProfiles(OpticsIlluminationModeEnum opticsIlluminationModeEnum, IReadOnlyList<ChirpAODWaveformProfile> chirpAODWaveProfiles)
    {
        Guard.IsNotEmpty(chirpAODWaveProfiles);

        foreach (var aodWaveProfile in chirpAODWaveProfiles)
        {
            Guard.IsNotEmpty(aodWaveProfile.Bytes);
        }

        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }
}