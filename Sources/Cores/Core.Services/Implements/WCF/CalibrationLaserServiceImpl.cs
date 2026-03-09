using CommunityToolkit.Diagnostics;
using Core.Models.Enums.Optics;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Data.DataStruct.PMT;
using Cuga.Engine.Interface;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationLaserService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationLaserServiceImpl(
    ICalibrationConfigService calibrationConfigService) : BaseService<ICgCalibrationService>, ICalibrationLaserService
{
    public SxExecuteRet<bool> Connect()
    {
        if (IsConnected) return SxExecuteRetHelper.CreateSuccess(true);

        return Invoke(() =>
        {
            var createService = CreateService(new SxWcfEndPoint("127.0.0.1", 80, CgInernalAddr.CalAddr));
            IsConnected = createService.IsSuccess;

            return createService;
        }, false);
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
        var sxExecuteRet = Invoke(() => Service?.LaserBeamAdjust(isEnable));

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

    public SxExecuteRet<IReadOnlyList<LaserLightInformation>> GetLaserLightInformations()
    {
        var sxExecuteRet = Invoke(() => Service?.GetLightConfig());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<LaserLightInformation>>(sxExecuteRet.ErrorMsg, []);
        if (sxExecuteRet.Anything.Length == 0) return SxExecuteRetHelper.CreateError<IReadOnlyList<LaserLightInformation>>("Laser Light Information is empty", []);

        var laserLightInformations = sxExecuteRet.Anything.Select(t => LaserLightInformation.Default.Clone().AdaptIn(t)).OrderBy(t => t).ToArray();

        Guard.IsTrue(laserLightInformations.DistinctBy(t => t).Count() == laserLightInformations.Length, "Laser Light Information is not unique");

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<LaserLightInformation>>(laserLightInformations);
    }

    public SxExecuteRet<bool> ToggleOpticsMagType(ProductivityInformation productivityInformation)
    {
        var c2MProductivityInfo = productivityInformation.AdaptTo();
        var sxExecuteRet = Invoke(() => Service?.SetMag(c2MProductivityInfo.Mag, c2MProductivityInfo.Speed, productivityInformation.OpticsIlluminationModeEnum.ToSxNIOIEnum()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum opticsAODWorkingModeEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.SetAOD_NO(opticsAODWorkingModeEnum.ToOpticsAodWorkingMode()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetAODDelayValue(ProductivityInformation productivityInformation, double prescanAODDelay, double chirpAODDelay)
    {
        var c2MProductivityInfo = productivityInformation.AdaptTo();
        var sxExecuteRet = Invoke(() => Service?.SetMagAndWaveZero(c2MProductivityInfo.Mag, productivityInformation.OpticsIlluminationModeEnum.ToSxNIOIEnum(), c2MProductivityInfo.Speed, Convert.ToInt32(chirpAODDelay), Convert.ToInt32(prescanAODDelay)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetDefaultPrescanAODWaveProfileByCoefficient(ProductivityInformation productivityInformation, double coefficient)
    {
        var sxExecuteRetByGetPrescanAODWaveProfiles = calibrationConfigService.GetPrescanAODWaveProfiles(productivityInformation);
        if (sxExecuteRetByGetPrescanAODWaveProfiles.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRetByGetPrescanAODWaveProfiles.Msg, false);

        var prescanAODWaveProfiles = sxExecuteRetByGetPrescanAODWaveProfiles.Anything;

        foreach (var aodWaveProfile in prescanAODWaveProfiles) aodWaveProfile.ApplyCoefficient(coefficient);

        var sxExecuteRetBySetPrescanAODWaveProfiles = SetPrescanAODWaveProfiles(productivityInformation.OpticsIlluminationModeEnum, prescanAODWaveProfiles);

        return sxExecuteRetBySetPrescanAODWaveProfiles.IsSuccess == false
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

        var sxExecuteRet = Invoke(() => Service?.SendChirpAndPrescan([
            ..prescanAODWaveProfiles.Select(t => new CgAwgWaveParam
            {
                Electrode = t.OpticsAODElectrodeEnum.ToCgAwgElectrodeEnum(),
                WaveType = CgWaveType.Prescan,
                Mode = CgAwgSendWaveMode.ElectrodeDataMode,
                NIOI = opticsIlluminationModeEnum.ToCgNIOITypeEnum(),
                zeroNum = t.ZeroSampleCount,
                WaveData = [.. t.Bytes]
            })
        ]));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

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

        var sxExecuteRet = Invoke(() => Service?.SendChirpAndPrescan([
            ..chirpAODWaveProfiles.Select(t => new CgAwgWaveParam
            {
                Electrode = t.OpticsAODElectrodeEnum.ToCgAwgElectrodeEnum(),
                WaveType = CgWaveType.Chirp,
                Mode = CgAwgSendWaveMode.ElectrodeDataMode,
                NIOI = opticsIlluminationModeEnum.ToCgNIOITypeEnum(),
                zeroNum = t.ZeroSampleCount,
                WaveData = [.. t.Bytes]
            })
        ]));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        return SxExecuteRetHelper.CreateSuccess(true);
    }
}