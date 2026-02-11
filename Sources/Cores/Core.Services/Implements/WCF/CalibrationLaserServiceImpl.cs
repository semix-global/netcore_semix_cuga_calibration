using CommunityToolkit.Diagnostics;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Core.Utilities;
using Cuga.Data.DataStruct.Basic;
using Cuga.Data.DataStruct.PMT;
using Cuga.Engine.Interface;
using HalconDotNet;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using System.IO;

#if NET48
using Semix.WcfTransfer.DTO;
#endif

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationLaserService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed partial class CalibrationLaserServiceImpl(
    ICalibrationAlgorithmService calibrationAlgorithmService,
    ICalibrationStageService calibrationStageService,
    ICalibrationConfigService calibrationConfigService,
    CalibrationSetting calibrationSetting)
    : BaseService<ICgCalibrationService>, ICalibrationLaserService
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

    public SxExecuteRet<double> GetOpticalMeasurePower(ProductivityInformation productivityInformation, double flatnessTime)
    {
        var sxExecuteRet = Invoke(() => Service?.ReadDynamometer());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<double>(sxExecuteRet.Msg)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything /*/ (flatnessTime */ /*ns*/ /* / ((1 / productivityInformation.SampleRate */ /*KHz*/ /*) * 1000_000))*/);
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
        var sxExecuteRet = Invoke(() => Service?.GetProductivityInfos());

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ProductivityInformation>>(sxExecuteRet.ErrorMsg, []);

        var productivityInformationList = new List<ProductivityInformation>();

        foreach (var c2MProductivityInfo in sxExecuteRet.Anything
                     .Where(t => t.IsUsed && t.NIOI == opticsIlluminationModeEnum.ToSxNIOIEnum()))
        {
            var speedInfoSxExecuteRet = Invoke(() => Service?.GetSpeedInfo(c2MProductivityInfo.Mag, opticsIlluminationModeEnum.ToSxNIOIEnum()));
            if (speedInfoSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ProductivityInformation>>(speedInfoSxExecuteRet.ErrorMsg, []);

            var pmtDataLineHeightSxExecuteRet = Invoke(() => Service?.GetPmtDataLineHeight(c2MProductivityInfo.Mag, opticsIlluminationModeEnum.ToSxNIOIEnum()));
            if (pmtDataLineHeightSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ProductivityInformation>>(speedInfoSxExecuteRet.ErrorMsg, []);

            var hzAndRealSpeedSxExecuteRet = Invoke(() => Service?.GetHzAndRealSpeed(c2MProductivityInfo.NIOI, c2MProductivityInfo.Mag, c2MProductivityInfo.Speed));
            if (hzAndRealSpeedSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<ProductivityInformation>>(speedInfoSxExecuteRet.ErrorMsg, []);

            productivityInformationList.Add(ProductivityInformation.Default.Clone().AdaptIn(
                c2MProductivityInfo,
                speedInfoSxExecuteRet.Anything,
                pmtDataLineHeightSxExecuteRet.Anything,
                hzAndRealSpeedSxExecuteRet.Anything.hz,
                hzAndRealSpeedSxExecuteRet.Anything.realSpeed));
        }

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<ProductivityInformation>>([.. productivityInformationList.OrderBy(t => t)]);
    }

    [Obsolete]
    public SxExecuteRet<bool> ToggleOpticsMagType(OpticsIlluminationModeEnum opticsIlluminationModeEnum, OpticsMagTypeEnum opticsMagTypeEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.SetMag(opticsMagTypeEnum.ToSxMagEnum(), SxSpeedEnum.Low, opticsIlluminationModeEnum.ToSxNIOIEnum()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
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

    [Obsolete]
    public SxExecuteRet<bool> SetAODDelayValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, OpticsMagTypeEnum opticsMagTypeEnum, double prescanAODDelay, double chirpAODDelay)
    {
        var sxExecuteRet = Invoke(() => Service?.SetMagAndWaveZero(opticsMagTypeEnum.ToSxMagEnum(), opticsIlluminationModeEnum.ToSxNIOIEnum(), SxSpeedEnum.Low, Convert.ToInt32(chirpAODDelay), Convert.ToInt32(prescanAODDelay)));

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

    [Obsolete]
    public SxExecuteRet<bool> SetDefaultPrescanAODWaveProfileByCoefficient(OpticsIlluminationModeEnum opticsIlluminationModeEnum, OpticsMagTypeEnum opticsMagTypeEnum, double coefficient)
    {
        var sxExecuteRet = GetProductivityInformations(opticsIlluminationModeEnum);
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        var sxExecuteRetByGetPrescanAODWaveProfiles = calibrationConfigService.GetPrescanAODWaveProfiles(sxExecuteRet.Anything.First(t => t.OpticsIlluminationModeEnum == opticsIlluminationModeEnum && t.AdaptTo().Mag == opticsMagTypeEnum.ToSxMagEnum()));
        if (sxExecuteRetByGetPrescanAODWaveProfiles.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRetByGetPrescanAODWaveProfiles.Msg, false);

        var prescanAODWaveProfiles = sxExecuteRetByGetPrescanAODWaveProfiles.Anything;

        foreach (var aodWaveProfile in prescanAODWaveProfiles) aodWaveProfile.ApplyCoefficient(coefficient);

        var sxExecuteRetBySetPrescanAODWaveProfiles = SetPrescanAODWaveProfiles(opticsIlluminationModeEnum, prescanAODWaveProfiles);

        return sxExecuteRetBySetPrescanAODWaveProfiles.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRetBySetPrescanAODWaveProfiles.Msg, false)
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

    [Obsolete]
    public SxExecuteRet<bool> SetDefaultChirpAODWaveProfile(OpticsIlluminationModeEnum opticsIlluminationModeEnum, OpticsMagTypeEnum opticsMagTypeEnum)
    {
        var sxExecuteRet = GetProductivityInformations(opticsIlluminationModeEnum);
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        var sxExecuteRetByGetChirpAODWaveProfiles = calibrationConfigService.GetChirpAODWaveProfiles(sxExecuteRet.Anything.First(t => t.OpticsIlluminationModeEnum == opticsIlluminationModeEnum && t.AdaptTo().Mag == opticsMagTypeEnum.ToSxMagEnum()));
        if (sxExecuteRetByGetChirpAODWaveProfiles.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRetByGetChirpAODWaveProfiles.Msg, false);

        var sxExecuteRetBySetPrescanAODWaveProfiles = SetChirpAODWaveProfiles(opticsIlluminationModeEnum, sxExecuteRetByGetChirpAODWaveProfiles.Anything);

        return sxExecuteRetBySetPrescanAODWaveProfiles.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRetBySetPrescanAODWaveProfiles.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
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

    public SxExecuteRet<bool> ToggleEnableAutoGainControl(bool enable, int pmtId, int channelId) =>
        SetCIBControlValue(enable ? 0x00_01_00_00 : 0x00_00_00_00, pmtId, channelId, sendDataList => Invoke(() => Service?.SetPmtDiffDataCommon(PMTRegEnum.DcAgc, sendDataList)));

    public SxExecuteRet<bool> ToggleProfileMode(CIBProfileModeEnum cibProfileModeEnum, int pmtId, int channelId) =>
        SetCIBControlValue(cibProfileModeEnum.ToCIBProfileMode(), pmtId, channelId, sendDataList => Invoke(() => Service?.SetPmtDiffDataCommon(PMTRegEnum.CibProfile, sendDataList)));

    public SxExecuteRet<bool> ToggleEnableMarkMode(bool enable, int pmtId, int channelId) => SetCIBControlValue(enable ? 1 : 0, pmtId, channelId, sendDataList => Invoke(() => Service?.SetPmtDiffDataCommon(PMTRegEnum.MarkMode, sendDataList)));

    public SxExecuteRet<bool> ToggleEnableL0K(bool enable, int pmtId, int channelId) => SetCIBControlValue(enable ? 1 : 0, pmtId, channelId, sendDataList => Invoke(() => Service?.SetPmtDiffDataCommon(PMTRegEnum.L0k, sendDataList)));

    public SxExecuteRet<bool> SetGain(double gain, int pmtId, int channelId) => SetCIBControlValue(gain, pmtId, channelId, sendDataList => Invoke(() => /* direct current */Service?.SendDc(sendDataList)));

    private SxExecuteRet<bool> SetCIBControlValue<T>(T value, int pmtId, int channelId, Func<List<(T Data, int PMTId, int ChannelId)>, SxExecuteRet> func)
    {
        var pmtConfigListSxExecuteRet = GetCIBConfigList();
        if (pmtConfigListSxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(pmtConfigListSxExecuteRet.Msg, false);

        var pmtConfigList = pmtConfigListSxExecuteRet.Anything.Where(t => t.IsUsed).ToList();
        var sendDataList = new List<(T Data, int PMTId, int ChannelId)>();

        switch (pmtId, channelId)
        {
            case (Constants.NegInt32Value, Constants.NegInt32Value):
                foreach (var (currentPmtId, _, channelIdList) in pmtConfigList) sendDataList.AddRange(channelIdList.Select(t => (value, currentPmtId, t)));

                break;

            case ( > 0, > 0):
                Guard.IsNotNull(pmtConfigList.Single(t => t.PmtId == pmtId).ChannelIdList.Single(t => t == channelId));
                sendDataList.Add((value, pmtId, channelId));

                break;

            case ( > 0, Constants.NegInt32Value):
                sendDataList.AddRange(pmtConfigList.Single(t => t.PmtId == pmtId).ChannelIdList.Select(t => (value, pmtId, t)));
                break;

            default:
                return ThrowHelper.ThrowArgumentOutOfRangeException<SxExecuteRet<bool>>(nameof(pmtId), nameof(channelId));
        }

        var sxExecuteRetAll = func.Invoke(sendDataList);

        return sxExecuteRetAll.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRetAll.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<IReadOnlyList<(int PmtId, bool IsUsed, IReadOnlyList<int> ChannelIdList)>> GetCIBConfigList()
    {
        var sxExecuteRet = Invoke(() => Service?.GetPmtState());

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<(int PmtId, bool IsUsed, IReadOnlyList<int> ChannelIdList)>>(sxExecuteRet.Msg, []);

        var result = (
            from item in sxExecuteRet.Anything
            group item by item.id
            into g
            select (
                PmtId: g.Key,
                IsUsed: g.All(x => x.used),
                ChannelIdList: (IReadOnlyList<int>)[.. g.Select(x => x.chl)]
            )
        ).ToList();

        var firstChannelList = result.First().ChannelIdList;

        Guard.IsTrue(result.Count > 0 && firstChannelList.Count > 0, "Pmt Config List is empty");
        Guard.IsTrue(firstChannelList.SequenceEqual(Enumerable.Range(1, firstChannelList.Count)), "Channel Id is not from 1 to ..");
        Guard.IsTrue(result.All(item => item.ChannelIdList.SequenceEqual(firstChannelList)), "Channel Id is not equal");
        Guard.IsTrue(result.Any(item => item.IsUsed), "Pmt Config List is not used");

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<(int PmtId, bool IsUsed, IReadOnlyList<int> ChannelIdList)>>(result);
    }

    public SxExecuteRet<IReadOnlyList<IReadOnlyList<double>>> GetCIBOfPMTDataList(int count, int pmtId, int channelId)
    {
        var sxExecuteRet = Invoke(() => Service?.GetPMTData_Appoint(count, pmtId, channelId));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<IReadOnlyList<double>>>(sxExecuteRet.Msg, []);
        if (sxExecuteRet.Anything.Count == 0) return SxExecuteRetHelper.CreateError<IReadOnlyList<IReadOnlyList<double>>>("Pmt Value List is empty", []);

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<IReadOnlyList<double>>>(sxExecuteRet.Anything);
    }

    public SxExecuteRet<IReadOnlyList<IReadOnlyList<double>>> GetCIBOfSenseDataList(int count, int pmtId, int channelId)
    {
        var sxExecuteRet = Invoke(() => Service?.GetSenseData(count, pmtId, channelId));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<IReadOnlyList<double>>>(sxExecuteRet.Msg, []);
        if (sxExecuteRet.Anything.Data.Count != count) return SxExecuteRetHelper.CreateError<IReadOnlyList<IReadOnlyList<double>>>("Pmt Sense Value List is empty", []);

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<IReadOnlyList<double>>>(sxExecuteRet.Anything.Data);
    }

    public SxExecuteRet<bool> SetCIBChirp(IReadOnlyList<double> gainList, int pmtId, int channelId)
    {
        // pmt增益电压范围 [-14, 14]
        var senseValues = (Vector<double>.Build.DenseOfEnumerable(gainList) / 14)
            .Select(t => ConvertUtils.ToInt16NotOverflowException(Math.Round(Math.Pow(2, 15) * t, MidpointRounding.AwayFromZero)))
            .ToArray();

        var senseData = new List<byte>();
        foreach (var compArray in senseValues.Select(t => -t).Select(BitConverter.GetBytes))
        {
            senseData.Add(0);
            senseData.Add(0);
            senseData.Add(compArray[1]);
            senseData.Add(compArray[0]);
        }

        // 取反, 差分信号
        var pmtValues = (Vector<double>.Build.DenseOfEnumerable(gainList) * -1 / 14)
            .Select(t => ConvertUtils.ToInt16NotOverflowException(Math.Round(Math.Pow(2, 15) * t, MidpointRounding.AwayFromZero)))
            .ToArray();

        var pmtData = new List<byte>();
        foreach (var compArray in pmtValues.Select(t => -t).Select(BitConverter.GetBytes))
        {
            pmtData.Add(0);
            pmtData.Add(0);
            pmtData.Add(compArray[1]);
            pmtData.Add(compArray[0]);
        }

        var sxExecuteRet = Invoke(() => Service?.SendChirp(pmtData, senseData, pmtId, channelId));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double Ecs, double Motor, bool isAFServo)> RuntimeAfCalibration(
        CalChipSiteModelEnum calChipSiteModelEnum,
        ProductivityInformation productivityInformation,
        int pmtId,
        double? coefficient = null,
        Point? point = null)
    {
        ushort? level = null;
        if (coefficient is not null)
        {
            var laserLightInformationRet = CoefficientToLaserLightInformation(coefficient.Value);
            if (laserLightInformationRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<(double Ecs, double AfMotor, bool isAFServo)>(laserLightInformationRet.ErrorMsg);
            level = Convert.ToUInt16(laserLightInformationRet.Anything.Level);
        }

        var sxExecuteRet = Invoke(() => Service!.RuntimeAutofocusCalibration(
        productivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum(),
        productivityInformation.AdaptTo().Mag.ToCgMagTypeEnum(),
        productivityInformation.AdaptTo().Speed.ToCgSpeedLevelType(),
        calChipSiteModelEnum.ToCgCalChipType(),
        Convert.ToUInt16(pmtId),
        level,
        point?.ToCgPoint()
        ));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<(double Ecs, double AfMotor, bool isAFServo)>(sxExecuteRet.Msg)
            : SxExecuteRetHelper.CreateSuccess<(double Ecs, double AfMotor, bool isAFServo)>((sxExecuteRet.Anything.Ecs, sxExecuteRet.Anything.Offset, true));
    }

    [Obsolete]
    public SxExecuteRet<List<DarkFieldImageDTO>> GetDarkFieldLineScanImageList(
        Point position,
        int xWidthPixel,
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward)
    {
        var darkFieldImagesRet = Invoke(() => Service?.GetDFImgCalibration(
            new SxCollectImgParam
            {
                Type = SxCollectImgType.Normal,
                Mag = opticsMagTypeEnum.ToSxMagEnum(),
                Speed = xStageSpeedEnum.ToSxSpeedEnum(),
                NIOI = opticsIlluminationModeEnum.ToSxNIOIEnum(),
                CoordinateSystem = stageCoordinateSystemEnum.ToSxCollectImgCoordinateSystemEnum(),
                CollectMode = SxCollectMode.PW,
                PMTId = pmtId,
                Width = xWidthPixel,
                StartPoint = [position.ToSxPointD()],
                IsSingle = true,
                AF = isAutoFocus ? 0 : 1,
                IsForward = isForward,
                IsCalibration = true, /*为true时不下发波形*/
                ImgArrayResoult = false /*true时返回CgRawImgModel/C2MImgMode(byte[])，false时返回M2CImgSysCollectImgDTO(Url)*/
            }));

        if (darkFieldImagesRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<DarkFieldImageDTO>>(darkFieldImagesRet.ErrorMsg, []);
        if (darkFieldImagesRet.Anything.Count != 3) return SxExecuteRetHelper.CreateError<List<DarkFieldImageDTO>>("Dark Images Count is not 3", []);

        var result = new List<DarkFieldImageDTO>(darkFieldImagesRet.Anything.Count);

        foreach (var m2CImgSysCollectImgDto in darkFieldImagesRet.Anything)
        {
            var bytes = File.ReadAllBytes(m2CImgSysCollectImgDto.Url);
            var image = RawImageFactory.CreateImage(bytes);
            result.Add(new DarkFieldImageDTO { Image = image }.AdaptIn(m2CImgSysCollectImgDto));
        }

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<List<DarkFieldImageDTO>> GetDarkFieldLineScanImageList(
        Point position,
        int xWidthPixel,
        ProductivityInformation productivityInformation,
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward)
    {
        var darkFieldImagesRet = Invoke(() => Service?.GetDFImgCalibration(
            new SxCollectImgParam
            {
                Type = SxCollectImgType.Normal,
                Mag = productivityInformation.AdaptTo().Mag,
                Speed = productivityInformation.AdaptTo().Speed,
                NIOI = opticsIlluminationModeEnum.ToSxNIOIEnum(),
                CoordinateSystem = stageCoordinateSystemEnum.ToSxCollectImgCoordinateSystemEnum(),
                CollectMode = SxCollectMode.PW,
                PMTId = pmtId,
                Width = xWidthPixel,
                StartPoint = [position.ToSxPointD()],
                IsSingle = true,
                AF = isAutoFocus ? 0 : 1,
                IsForward = isForward,
                IsCalibration = true, /*为true时不下发波形*/
                ImgArrayResoult = false /*true时返回CgRawImgModel/C2MImgMode(byte[])，false时返回M2CImgSysCollectImgDTO(Url)*/
            }));

        if (darkFieldImagesRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<DarkFieldImageDTO>>(darkFieldImagesRet.ErrorMsg, []);
        if (darkFieldImagesRet.Anything.Count != 3) return SxExecuteRetHelper.CreateError<List<DarkFieldImageDTO>>("Dark Images Count is not 3", []);

        var result = new List<DarkFieldImageDTO>(darkFieldImagesRet.Anything.Count);

        foreach (var m2CImgSysCollectImgDto in darkFieldImagesRet.Anything)
        {
            var bytes = File.ReadAllBytes(m2CImgSysCollectImgDto.Url);
            var image = RawImageFactory.CreateImage(bytes);
            result.Add(new DarkFieldImageDTO { Image = image }.AdaptIn(m2CImgSysCollectImgDto));
        }

        return SxExecuteRetHelper.CreateSuccess(result);
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
        SxExecuteRet<List<M2CImgSysCollectImgDTO>> darkFieldImagesRet;
        try
        {
            var setWaitTimeRet = Invoke(() => Service?.SetWaitTime(60));
            if (setWaitTimeRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<DarkFieldRawScanImageDTO>>(setWaitTimeRet.ErrorMsg, []);

            darkFieldImagesRet = stageCoordinateSystemEnum switch
            {
                StageCoordinateSystemEnum.Machine => Invoke(() => Service?.GetDFImgCalibration(
                    new SxCollectImgParam
                    {
                        Type = SxCollectImgType.Normal,
                        Mag = opticsMagTypeEnum.ToSxMagEnum(),
                        Speed = xStageSpeedEnum.ToSxSpeedEnum(),
                        NIOI = opticsIlluminationModeEnum.ToSxNIOIEnum(),
                        CoordinateSystem = stageCoordinateSystemEnum.ToSxCollectImgCoordinateSystemEnum(),
                        CollectMode = SxCollectMode.PTP,
                        PMTId = pmtId,
                        StartPoint = [startPosition.ToSxPointD()],
                        EndPoint = [endPosition.ToSxPointD()],
                        IsSingle = true,
                        AF = isAutoFocus ? 0 : 1,
                        IsForward = isForward,
                        IsCalibration = true, /*为true时不下发波形*/
                        ImgArrayResoult = false /*true时返回CgRawImgModel/C2MImgMode(byte[])，false时返回M2CImgSysCollectImgDTO(Url)*/
                    })),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<SxExecuteRet<List<M2CImgSysCollectImgDTO>>>(nameof(stageCoordinateSystemEnum))
            };
        }
        finally
        {
            var setWaitTimeRet = Invoke(() => Service?.SetWaitTime(30));
            if (setWaitTimeRet.IsSuccess == false) throw new CugaException(setWaitTimeRet.ErrorMsg);
        }

        if (darkFieldImagesRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<DarkFieldRawScanImageDTO>>(darkFieldImagesRet.ErrorMsg, []);
        if (darkFieldImagesRet.Anything.Count != 3) return SxExecuteRetHelper.CreateError<List<DarkFieldRawScanImageDTO>>("Dark Images Count is not 3", []);

        var result = new List<DarkFieldRawScanImageDTO>(darkFieldImagesRet.Anything.Count);

        foreach (var m2CImgSysCollectImgDto in darkFieldImagesRet.Anything)
        {
            result.Add(new DarkFieldRawScanImageDTO().AdaptIn(m2CImgSysCollectImgDto));
        }

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<List<DarkFieldRawScanImageDTO>> GetDarkFieldLineScanImageList(
        Point startPosition,
        Point endPosition,
        ProductivityInformation productivityInformation,
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward,
        (double zStart, double zEnd, double zSpeed)? zMotionParam = null)
    {
        SxExecuteRet<List<M2CImgSysCollectImgDTO>> darkFieldImagesRet;
        try
        {
            var setWaitTimeRet = Invoke(() => Service?.SetWaitTime(60));
            if (setWaitTimeRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<DarkFieldRawScanImageDTO>>(setWaitTimeRet.ErrorMsg, []);

            darkFieldImagesRet = stageCoordinateSystemEnum switch
            {
                StageCoordinateSystemEnum.Machine => Invoke(() => Service?.GetDFImgCalibration(
                    new SxCollectImgParam
                    {
                        Type = pmtId == -1 ? SxCollectImgType.Using : SxCollectImgType.Normal,
                        Mag = productivityInformation.AdaptTo().Mag,
                        Speed = productivityInformation.AdaptTo().Speed,
                        NIOI = opticsIlluminationModeEnum.ToSxNIOIEnum(),
                        CoordinateSystem = stageCoordinateSystemEnum.ToSxCollectImgCoordinateSystemEnum(),
                        CollectMode = SxCollectMode.PTP,
                        PMTId = pmtId,
                        StartPoint = [startPosition.ToSxPointD()],
                        EndPoint = [endPosition.ToSxPointD()],
                        IsSingle = true,
                        AF = isAutoFocus ? 0 : 1,
                        IsForward = isForward,
                        IsCalibration = true, /*为true时不下发波形*/
                        ImgArrayResoult = false /*true时返回CgRawImgModel/C2MImgMode(byte[])，false时返回M2CImgSysCollectImgDTO(Url)*/,
                        ZParam = (isAutoFocus == false && zMotionParam != null)
                            ? new SxZParam()
                            {
                                Start = Convert.ToInt32(zMotionParam.Value.zStart),
                                End = Convert.ToInt32(zMotionParam.Value.zEnd),
                                Vel = Convert.ToInt32(zMotionParam.Value.zSpeed)
                            }
                            : new SxZParam()
                    })),
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<SxExecuteRet<List<M2CImgSysCollectImgDTO>>>(nameof(stageCoordinateSystemEnum))
            };
        }
        finally
        {
            var setWaitTimeRet = Invoke(() => Service?.SetWaitTime(30));
            if (setWaitTimeRet.IsSuccess == false) throw new CugaException(setWaitTimeRet.ErrorMsg);
        }

        if (darkFieldImagesRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<DarkFieldRawScanImageDTO>>(darkFieldImagesRet.ErrorMsg, []);
        if (darkFieldImagesRet.Anything.Count % 3 != 0) return SxExecuteRetHelper.CreateError<List<DarkFieldRawScanImageDTO>>("Dark Images Count is not 3", []);

        var result = new List<DarkFieldRawScanImageDTO>(darkFieldImagesRet.Anything.Count);

        foreach (var m2CImgSysCollectImgDto in darkFieldImagesRet.Anything)
        {
            result.Add(new DarkFieldRawScanImageDTO().AdaptIn(m2CImgSysCollectImgDto));
        }

        return SxExecuteRetHelper.CreateSuccess(result);
    }


    [Obsolete]
    public SxExecuteRet<List<List<DarkFieldImageDTO>>> GetChuckDarkFieldRowLineScanImageList(
        List<Point> machinePositionList,
        int xWidthPixel,
        double xPixelSize,
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        OpticsIlluminationModeEnum opticsIlluminationModeEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus)
    {
        var isIncreasing = machinePositionList.Select(t => t.X).IsIncreasing(true);
        var isDecreasing = machinePositionList.Select(t => t.X).IsDecreasing(true);
        if (machinePositionList.Count < 2
            || machinePositionList.Any(t => t.Y - machinePositionList[0].Y == 0) == false // 检查y是否相同
            || (isIncreasing == false && isDecreasing == false)) // 检查x是否递增
            throw new ArgumentOutOfRangeException(nameof(machinePositionList), machinePositionList, null);

        var directionRet = calibrationStageService.GetMachineDirection();
        if (directionRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<List<DarkFieldImageDTO>>>(directionRet.ErrorMsg, []);
        var directionX = directionRet.Anything.XDirection;

        var extendWidth = xWidthPixel * xPixelSize / 2.0;

        var startPointList = new List<SxPointD>();
        var endPointList = new List<SxPointD>();

        foreach (var machinePoint in machinePositionList)
        {
            var startPoint = new Point(machinePoint.X - extendWidth, machinePoint.Y).ToSxPointD();
            var endPoint = new Point(machinePoint.X + extendWidth, machinePoint.Y).ToSxPointD();

            startPointList.Add(isIncreasing ? startPoint : endPoint);
            endPointList.Add(isIncreasing ? endPoint : startPoint);
        }

        // 从起点到终点采图，输出三通道长图片
        var darkFieldImagesRet = stageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Machine => Invoke(() => Service?.GetDFImgCalibration(
                new SxCollectImgParam
                {
                    Type = SxCollectImgType.Normal,
                    Mag = opticsMagTypeEnum.ToSxMagEnum(),
                    Speed = xStageSpeedEnum.ToSxSpeedEnum(),
                    NIOI = opticsIlluminationModeEnum.ToSxNIOIEnum(),
                    CoordinateSystem = stageCoordinateSystemEnum.ToSxCollectImgCoordinateSystemEnum(),
                    CollectMode = SxCollectMode.PTP,
                    PMTId = pmtId,
                    StartPoint = startPointList,
                    EndPoint = endPointList,
                    IsSingle = true,
                    AF = isAutoFocus ? 0 : 1,
                    IsForward = isIncreasing,
                    IsCalibration = true, /*为true时不下发波形*/
                    ImgArrayResoult = false /*true时返回CgRawImgModel/C2MImgMode(byte[])，false时返回M2CImgSysCollectImgDTO(Url)*/
                })),
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<SxExecuteRet<List<M2CImgSysCollectImgDTO>>>(nameof(stageCoordinateSystemEnum))
        };

        if (darkFieldImagesRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<List<DarkFieldImageDTO>>>(darkFieldImagesRet.ErrorMsg, []);
        if (darkFieldImagesRet.Anything.Count != machinePositionList.Count * 3) return SxExecuteRetHelper.CreateError<List<List<DarkFieldImageDTO>>>("Dark Images Count is empty", []);

        var splitImagesAllChannels = new List<List<DarkFieldImageDTO>>();
        for (var i = 0; i < machinePositionList.Count; i++)
        {
            var results = darkFieldImagesRet.Anything.Where(t => t.Position == i).ToList();

            var splitImages = new List<DarkFieldImageDTO>();
            foreach (var item in results.OrderBy(t => t.Channel))
            {
                var bytes = File.ReadAllBytes(item.Url);

                var image = stageCoordinateSystemEnum switch
                {
                    StageCoordinateSystemEnum.Machine => directionX < 0 && machinePositionList.First().X < machinePositionList.Last().X
                        ? RawImageFactory.CreateImage(RawImageFactory.ToHorizontalFlipMatrix(bytes).RawBytes)
                        : RawImageFactory.CreateImage(bytes),
                    StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => RawImageFactory.CreateImage(bytes),
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<HImage>(nameof(stageCoordinateSystemEnum))
                };

                var splitImageDto = new DarkFieldImageDTO { PMTId = pmtId, ChannelId = item.Channel, Image = image, Height = item.ImgHeight, Width = item.ImgWidth };
                splitImages.Add(splitImageDto);
            }

            splitImagesAllChannels.Add(splitImages);
        }

        return SxExecuteRetHelper.CreateSuccess(splitImagesAllChannels);
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
        var isIncreasing = machinePositionList.Select(t => t.X).IsIncreasing(true);
        var isDecreasing = machinePositionList.Select(t => t.X).IsDecreasing(true);
        if (machinePositionList.Count < 2
            || machinePositionList.Any(t => t.Y - machinePositionList[0].Y == 0) == false // 检查y是否相同
            || (isIncreasing == false && isDecreasing == false)) // 检查x是否递增
            throw new ArgumentOutOfRangeException(nameof(machinePositionList), machinePositionList, null);

        var directionRet = calibrationStageService.GetMachineDirection();
        if (directionRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<List<DarkFieldImageDTO>>>(directionRet.ErrorMsg, []);
        var directionX = directionRet.Anything.XDirection;

        var scanLineXPixelSize = xPixelSize;
        var extendWidth = xWidthPixel * scanLineXPixelSize / 2.0;

        var startPointList = new List<SxPointD>();
        var endPointList = new List<SxPointD>();

        foreach (var machinePoint in machinePositionList)
        {
            var startPoint = new Point(machinePoint.X - extendWidth, machinePoint.Y).ToSxPointD();
            var endPoint = new Point(machinePoint.X + extendWidth, machinePoint.Y).ToSxPointD();

            startPointList.Add(isIncreasing ? startPoint : endPoint);
            endPointList.Add(isIncreasing ? endPoint : startPoint);
        }

        // 从起点到终点采图，输出三通道长图片
        var darkFieldImagesRet = stageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Machine => Invoke(() => Service?.GetDFImgCalibration(
                new SxCollectImgParam
                {
                    Type = SxCollectImgType.Normal,
                    Mag = productivityInformation.AdaptTo().Mag,
                    Speed = productivityInformation.AdaptTo().Speed,
                    NIOI = opticsIlluminationModeEnum.ToSxNIOIEnum(),
                    CoordinateSystem = stageCoordinateSystemEnum.ToSxCollectImgCoordinateSystemEnum(),
                    CollectMode = SxCollectMode.PTP,
                    PMTId = pmtId,
                    StartPoint = startPointList,
                    EndPoint = endPointList,
                    IsSingle = true,
                    AF = isAutoFocus ? 0 : 1,
                    IsForward = isIncreasing,
                    IsCalibration = true, /*为true时不下发波形*/
                    ImgArrayResoult = false /*true时返回CgRawImgModel/C2MImgMode(byte[])，false时返回M2CImgSysCollectImgDTO(Url)*/
                })),
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<SxExecuteRet<List<M2CImgSysCollectImgDTO>>>(nameof(stageCoordinateSystemEnum))
        };

        if (darkFieldImagesRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<List<DarkFieldImageDTO>>>(darkFieldImagesRet.ErrorMsg, []);
        if (darkFieldImagesRet.Anything.Count != machinePositionList.Count * 3) return SxExecuteRetHelper.CreateError<List<List<DarkFieldImageDTO>>>("Dark Images Count is empty", []);

        var splitImagesAllChannels = new List<List<DarkFieldImageDTO>>();
        for (var i = 0; i < machinePositionList.Count; i++)
        {
            var results = darkFieldImagesRet.Anything.Where(t => t.Position == i).ToList();

            var splitImages = new List<DarkFieldImageDTO>();
            foreach (var item in results.OrderBy(t => t.Channel))
            {
                var bytes = File.ReadAllBytes(item.Url);

                var image = stageCoordinateSystemEnum switch
                {
                    StageCoordinateSystemEnum.Machine => directionX < 0 && machinePositionList.First().X < machinePositionList.Last().X
                        ? RawImageFactory.CreateImage(RawImageFactory.ToHorizontalFlipMatrix(bytes).RawBytes)
                        : RawImageFactory.CreateImage(bytes),
                    StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => RawImageFactory.CreateImage(bytes),
                    _ => ThrowHelper.ThrowArgumentOutOfRangeException<HImage>(nameof(stageCoordinateSystemEnum))
                };

                var splitImageDto = new DarkFieldImageDTO { PMTId = pmtId, ChannelId = item.Channel, Image = image, Height = item.ImgHeight, Width = item.ImgWidth };
                splitImages.Add(splitImageDto);
            }

            splitImagesAllChannels.Add(splitImages);
        }

        return SxExecuteRetHelper.CreateSuccess(splitImagesAllChannels);
    }
}