using CommunityToolkit.Diagnostics;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Exceptions;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Data.DataStruct.PMT;
using Cuga.Engine.Interface;
using MathNet.Numerics;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using Semix.WcfTransfer.DTO;
using System.IO;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationCIBService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationCIBServiceImpl : BaseService<ICgCalibrationService>, ICalibrationCIBService
{
    public SxExecuteRet<bool> Connect()
    {
        if (IsConnected) return SxExecuteRetHelper.CreateSuccess(true);

        return Invoke(() =>
        {
            var ep = new SxWcfEndPoint("127.0.0.1", 80, CgInernalAddr.CalAddr);
            var createService = CreateService(ep);
            IsConnected = createService.IsSuccess;
            return createService;
        }, false);
    }

    public SxExecuteRet<IReadOnlyList<CIBInformation>> GetCIBInformations()
    {
        var sxExecuteRet = Invoke(() => Service?.GetPmtState());

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<CIBInformation>>(sxExecuteRet.Msg, []);

        var cibInformations = sxExecuteRet.Anything
            .Where(t => t.used)
            .Select(t => CIBInformation.Default.Clone().AdaptIn((t.id, t.chl, t.used)))
            .OrderBy(t => t)
            .ToArray();

        Guard.IsTrue(cibInformations.DistinctBy(t => t).Count() == cibInformations.Length, "CIB Information is not unique");

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<CIBInformation>>(cibInformations);
    }

    public SxExecuteRet<bool> ToggleEnableAGC(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        var setValue = enable ? 0x00_01_00_00 : 0x00_00_00_00;

        var sxExecuteRet = Invoke(() => Service?.SetPmtDiffDataCommon(PMTRegEnum.DcAgc, [.. cibInformations.Select(t => (setValue, t.PMTId, t.ChannelId))]));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        var sxExecuteRetReadCIBReg = Invoke(() => Service?.ReadCIBReg(PMTRegEnum.DcAgc));
        if (sxExecuteRetReadCIBReg.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRetReadCIBReg.Msg, false);

        foreach (var cibInformation in cibInformations)
        {
            var cibReg = sxExecuteRetReadCIBReg.Anything.Single(t => t.Id == cibInformation.PMTId && t.Channel == cibInformation.ChannelId);

            if (cibReg.Value != setValue) return SxExecuteRetHelper.CreateError($"Set AGC {(enable ? "Enable" : "Disable")} Failed for PMTId:{cibInformation.PMTId} ChannelId:{cibInformation.ChannelId}", false);
        }

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleProfileMode(IReadOnlyList<CIBInformation> cibInformations, CIBProfileModeEnum cibProfileModeEnum)
    {
        var setValue = cibProfileModeEnum.ToCIBProfileMode();

        var sxExecuteRet = Invoke(() => Service?.SetPmtDiffDataCommon(PMTRegEnum.CibProfile, [.. cibInformations.Select(t => (setValue, t.PMTId, t.ChannelId))]));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        var sxExecuteRetReadCIBReg = Invoke(() => Service?.ReadCIBReg(PMTRegEnum.CibProfile));
        if (sxExecuteRetReadCIBReg.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRetReadCIBReg.Msg, false);

        foreach (var cibInformation in cibInformations)
        {
            var cibReg = sxExecuteRetReadCIBReg.Anything.Single(t => t.Id == cibInformation.PMTId && t.Channel == cibInformation.ChannelId);

            if (cibReg.Value != setValue) return SxExecuteRetHelper.CreateError($"Set Profile Mode {cibProfileModeEnum} Failed for PMTId:{cibInformation.PMTId} ChannelId:{cibInformation.ChannelId}", false);
        }

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableL0K(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        var setValue = enable ? 1 : 0;

        var sxExecuteRet = Invoke(() => Service?.SetPmtDiffDataCommon(PMTRegEnum.L0k, [.. cibInformations.Select(t => (setValue, t.PMTId, t.ChannelId))]));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        var sxExecuteRetReadCIBReg = Invoke(() => Service?.ReadCIBReg(PMTRegEnum.L0k));
        if (sxExecuteRetReadCIBReg.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRetReadCIBReg.Msg, false);

        foreach (var cibInformation in cibInformations)
        {
            var cibReg = sxExecuteRetReadCIBReg.Anything.Single(t => t.Id == cibInformation.PMTId && t.Channel == cibInformation.ChannelId);

            if (cibReg.Value != setValue) return SxExecuteRetHelper.CreateError($"Set L0K {(enable ? "Enable" : "Disable")} Failed for PMTId:{cibInformation.PMTId} ChannelId:{cibInformation.ChannelId}", false);
        }

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetGain(IReadOnlyList<CIBInformation> cibInformations, double gain)
    {
        var sxExecuteRet = Invoke(() => Service?.SendDc([.. cibInformations.Select(t => (gain, t.PMTId, t.ChannelId))]));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetMMD(CIBInformation cibInformation, IReadOnlyList<double> logGainMul128U12Bits, IReadOnlyList<double> gainS16Bits)
    {
        var sxExecuteRet = Invoke(() => Service?.SendCIBWave([..logGainMul128U12Bits], CgCIBWaveType.Sense, cibInformation.PMTId, cibInformation.ChannelId));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        sxExecuteRet = Invoke(() => Service?.SendCIBWave([..gainS16Bits], CgCIBWaveType.IG, cibInformation.PMTId, cibInformation.ChannelId));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        sxExecuteRet = Invoke(() => Service?.SetPmtDiffDataCommon(PMTRegEnum.MaxGain, [(Convert.ToInt32(logGainMul128U12Bits.Max()), cibInformation.PMTId, cibInformation.ChannelId)]));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetLightMatching(IReadOnlyList<CIBInformation> cibInformations, double digitalGainPlusMultiplicativeFactors)
    {
        var sxExecuteRet = Invoke(() => Service?.SetPmtDiffDataCommon(PMTRegEnum.Digital_Gain_Multiplicative_Factors, [.. cibInformations.Select(t => (Convert.ToInt32(digitalGainPlusMultiplicativeFactors), t.PMTId, t.ChannelId))]));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetIlluminationProfile(IReadOnlyList<CIBInformation> cibInformations, IReadOnlyList<double> illuminationProfiles)
    {
        throw new NotImplementedException();
    }

    public SxExecuteRet<IReadOnlyList<CIBDelayDTO>> GetDelays(IReadOnlyList<CIBInformation> cibInformations)
    {
        var pmtRet = Invoke(() => Service?.GetPMTDelay());
        if (pmtRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<CIBDelayDTO>>(pmtRet.ErrorMsg, []);

        var results = pmtRet.Anything
            .Select(t => new CIBDelayDTO().AdaptIn(t))
            .Where(t => cibInformations.Contains(t.CIBInformation))
            .ToArray();

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<CIBDelayDTO>>(results);
    }

    public SxExecuteRet<bool> SetDelays(IReadOnlyList<CIBDelayDTO> delays)
    {
        var pmtRet = Invoke(() => Service?.SetPMTDelay(delays.Select(item => item.AdaptTo()).ToList()));

        return pmtRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(pmtRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<IReadOnlyList<IReadOnlyList<CIBMMDGainRelationshipDTO>>> GetCIBMMDGains(IReadOnlyList<CIBInformation> cibInformations, double startGain, double stepGain, double stopGain)
    {
        var sxExecuteRet = Invoke(() => Service?.GetDcSenseRelationalTables([.. cibInformations.Select(t => (stopGain, startGain, stepGain, t.PMTId, t.ChannelId))]));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<IReadOnlyList<CIBMMDGainRelationshipDTO>>>(sxExecuteRet.ErrorMsg, []);

        var gains = Generate.LinearRange(startGain, stepGain, stopGain);

        var results = new CIBMMDGainRelationshipDTO[cibInformations.Count][];

        foreach (var (cibInformationIndex, cibInformation) in cibInformations.Index())
        {
            var cgDcSenseRelationalModel = sxExecuteRet.Anything.Single(t => t.PmtId == cibInformation.PMTId && t.Channel == cibInformation.ChannelId);

            var cibmmdGains = new CIBMMDGainRelationshipDTO[gains.Length];

            foreach (var (gainIndex, gain) in gains.Index())
            {
                cibmmdGains[gainIndex] = new CIBMMDGainRelationshipDTO()
                    .WithCIBInformation(cibInformation)
                    .WithGain(gain)
                    .WithSenseU14Bit(Convert.ToInt32(cgDcSenseRelationalModel.AvgSense[gainIndex].sense))
                    .WithGainS16Bit(cgDcSenseRelationalModel.AvgSense[gainIndex].dc);
            }

            results[cibInformationIndex] = cibmmdGains;
        }

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<IReadOnlyList<CIBMMDGainRelationshipDTO>>>(results);
    }

    public async Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point position,
        IReadOnlyList<CIBInformation> cibInformations,
        int imageWidth,
        bool isForward,
        bool isAutoFocus,
        CancellationToken cancellationToken)
    {
        var pmtIds = cibInformations.GroupBy(t => t.PMTId).Select(t => t.Key).ToArray();

        var dfImgCalibrationRet = Invoke(new SxCollectImgParam
        {
            Type = pmtIds.Length > 1 ? SxCollectImgType.Using : SxCollectImgType.Normal,
            Mag = productivityInformation.AdaptTo().Mag,
            Speed = productivityInformation.AdaptTo().Speed,
            NIOI = productivityInformation.OpticsIlluminationModeEnum.ToSxNIOIEnum(),
            CoordinateSystem = stageCoordinateSystemEnum.ToSxCollectImgCoordinateSystemEnum(),
            CollectMode = SxCollectMode.PW,
            PMTId = pmtIds.Length > 1 ? -1 : pmtIds[0],
            Width = imageWidth,
            StartPoint = [position.ToSxPointD()],
            IsSingle = true,
            AF = isAutoFocus ? 0 : 1,
            IsForward = isForward,
            IsCalibration = true, /*为true时不下发波形*/
            ImgArrayResoult = false /*true时返回CgRawImgModel/C2MImgMode(byte[])，false时返回M2CImgSysCollectImgDTO(Url)*/
        });

        var result = new DarkFieldImageDTO[cibInformations.Count];

        await Task.WhenAll(cibInformations.Index().Select(t => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (index, cibInformation) = t;

            var m2CImgSysCollectImgDto = dfImgCalibrationRet.Anything.Single(tt => tt.PMTId == cibInformation.PMTId && tt.Channel == cibInformation.ChannelId);

            var rawBytes = File.ReadAllBytes(m2CImgSysCollectImgDto.Url);
            var image = RawImageFactory.CreateImage(rawBytes);

            result[index] = new DarkFieldImageDTO { Image = image }.AdaptIn(m2CImgSysCollectImgDto);
        }, cancellationToken)));

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldImageDTO>>(result);
    }

    public async Task<SxExecuteRet<IReadOnlyList<DarkFieldRawScanImageDTO>>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point startPosition,
        Point endPosition,
        IReadOnlyList<CIBInformation> cibInformations,
        bool isForward,
        bool isAutoFocus,
        CancellationToken cancellationToken)
    {
        var pmtIds = cibInformations.GroupBy(t => t.PMTId).Select(t => t.Key).ToArray();

        var dfImgCalibrationRet = Invoke(new SxCollectImgParam
        {
            Type = pmtIds.Length > 1 ? SxCollectImgType.Using : SxCollectImgType.Normal,
            Mag = productivityInformation.AdaptTo().Mag,
            Speed = productivityInformation.AdaptTo().Speed,
            NIOI = productivityInformation.OpticsIlluminationModeEnum.ToSxNIOIEnum(),
            CoordinateSystem = stageCoordinateSystemEnum.ToSxCollectImgCoordinateSystemEnum(),
            CollectMode = SxCollectMode.PTP,
            PMTId = pmtIds.Length > 1 ? -1 : pmtIds[0],
            StartPoint = [startPosition.ToSxPointD()],
            EndPoint = [endPosition.ToSxPointD()],
            IsSingle = true,
            AF = isAutoFocus ? 0 : 1,
            IsForward = isForward,
            IsCalibration = true, /*为true时不下发波形*/
            ImgArrayResoult = false /*true时返回CgRawImgModel/C2MImgMode(byte[])，false时返回M2CImgSysCollectImgDTO(Url)*/
        });

        var result = new DarkFieldRawScanImageDTO[cibInformations.Count];

        await Task.WhenAll(cibInformations.Index().Select(t => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (index, cibInformation) = t;

            var m2CImgSysCollectImgDto = dfImgCalibrationRet.Anything.Single(tt => tt.PMTId == cibInformation.PMTId && tt.Channel == cibInformation.ChannelId);

            result[index] = new DarkFieldRawScanImageDTO().AdaptIn(m2CImgSysCollectImgDto);
        }, cancellationToken)));

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldRawScanImageDTO>>(result);
    }

    public async Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point startPosition,
        Point endPosition,
        IReadOnlyList<CIBInformation> cibInformations,
        double startECS,
        double stopECS,
        bool isForward,
        CancellationToken cancellationToken)
    {
        Guard.IsLessThan(startECS, stopECS);

        var pmtIds = cibInformations.GroupBy(t => t.PMTId).Select(t => t.Key).ToArray();

        var time = Math.Abs(startPosition.X - endPosition.X) / productivityInformation.XSpeedValue;
        var speedECS = Math.Abs(stopECS - startECS) / time;

        var dfImgCalibrationRet = Invoke(new SxCollectImgParam
        {
            Type = pmtIds.Length > 1 ? SxCollectImgType.Using : SxCollectImgType.Normal,
            Mag = productivityInformation.AdaptTo().Mag,
            Speed = productivityInformation.AdaptTo().Speed,
            NIOI = productivityInformation.OpticsIlluminationModeEnum.ToSxNIOIEnum(),
            CoordinateSystem = stageCoordinateSystemEnum.ToSxCollectImgCoordinateSystemEnum(),
            CollectMode = SxCollectMode.PTP,
            PMTId = pmtIds.Length > 1 ? -1 : pmtIds[0],
            StartPoint = [startPosition.ToSxPointD()],
            EndPoint = [endPosition.ToSxPointD()],
            IsSingle = true,
            AF = 1,
            IsForward = isForward,
            IsCalibration = true, /*为true时不下发波形*/
            ImgArrayResoult = false /*true时返回CgRawImgModel/C2MImgMode(byte[])，false时返回M2CImgSysCollectImgDTO(Url)*/,
            ZParam = new SxZParam
            {
                Start = Convert.ToInt32(startECS),
                End = Convert.ToInt32(stopECS),
                Vel = Convert.ToInt32(speedECS)
            }
        });

        var result = new DarkFieldImageDTO[cibInformations.Count];

        await Task.WhenAll(cibInformations.Index().Select(t => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (index, cibInformation) = t;

            var m2CImgSysCollectImgDto = dfImgCalibrationRet.Anything.Single(tt => tt.PMTId == cibInformation.PMTId && tt.Channel == cibInformation.ChannelId);

            var rawBytes = File.ReadAllBytes(m2CImgSysCollectImgDto.Url);
            var image = RawImageFactory.CreateImage(rawBytes);

            result[index] = new DarkFieldImageDTO { Image = image }.AdaptIn(m2CImgSysCollectImgDto);
        }, cancellationToken)));

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldImageDTO>>(result);
    }

    private SxExecuteRet<IReadOnlyList<M2CImgSysCollectImgDTO>> Invoke(SxCollectImgParam sxCollectImgParam)
    {
        SxExecuteRet<List<M2CImgSysCollectImgDTO>> dfImgCalibrationRet;

        try
        {
            var setWaitTimeRet = Invoke(() => Service?.SetWaitTime(60));
            if (setWaitTimeRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<M2CImgSysCollectImgDTO>>(setWaitTimeRet.ErrorMsg, []);

            dfImgCalibrationRet = Invoke(() => Service?.GetDFImgCalibration(sxCollectImgParam));
        }
        finally
        {
            var setWaitTimeRet = Invoke(() => Service?.SetWaitTime(30));
            if (setWaitTimeRet.IsSuccess == false) throw new CugaException(setWaitTimeRet.ErrorMsg);
        }

        if (dfImgCalibrationRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<M2CImgSysCollectImgDTO>>(dfImgCalibrationRet.ErrorMsg, []);
        // if (dfImgCalibrationRet.Anything.Count != cibInformations.Count) return SxExecuteRetHelper.CreateError<IReadOnlyList<DarkFieldRawScanImageDTO>>($"Dark Images Count is not {cibInformations.Count}", []);

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<M2CImgSysCollectImgDTO>>(dfImgCalibrationRet.Anything);
    }
}