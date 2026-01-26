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
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Net.Utilities.Algorithms.Halcon;
using Semix.CoreLib;
using Semix.WcfTransfer.DTO;
using System.IO;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationCIBService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationCIBServiceImpl(
    ICalibrationAlgorithmService calibrationAlgorithmService) : BaseService<ICgCalibrationService>, ICalibrationCIBService
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
        var sxExecuteRet = Invoke(() => Service?.SetPmtDiffDataCommon(PMTRegEnum.DcAgc, [.. cibInformations.Select(t => (enable ? 0x00_01_00_00 : 0x00_00_00_00, t.PMTId, t.ChannelId))]));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleProfileMode(IReadOnlyList<CIBInformation> cibInformations, CIBProfileModeEnum cibProfileModeEnum)
    {
        var sxExecuteRet = Invoke(() => Service?.SetPmtDiffDataCommon(PMTRegEnum.CibProfile, [.. cibInformations.Select(t => (cibProfileModeEnum.ToCIBProfileMode(), t.PMTId, t.ChannelId))]));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableL0K(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        var sxExecuteRet = Invoke(() => Service?.SetPmtDiffDataCommon(PMTRegEnum.L0k, [.. cibInformations.Select(t => (enable ? 1 : 0, t.PMTId, t.ChannelId))]));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetGain(IReadOnlyList<CIBInformation> cibInformations, double gain)
    {
        var sxExecuteRet = Invoke(() => Service?.SendDc([.. cibInformations.Select(t => (gain, t.PMTId, t.ChannelId))]));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableMarkMode(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        var sxExecuteRet = Invoke(() => Service?.SetPmtDiffDataCommon(PMTRegEnum.MarkMode, [.. cibInformations.Select(t => (enable ? 1 : 0, t.PMTId, t.ChannelId))]));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetMMD(CIBInformation cibInformation, IReadOnlyList<double> logGainMul128U12Bits, IReadOnlyList<double> gainS16Bits)
    {
        var logGainMul128Bytes = new List<byte>();
        foreach (var compArray in logGainMul128U12Bits
                     .Select(t => (int)t)
                     .Select(BitConverter.GetBytes))
        {
            logGainMul128Bytes.Add(0);
            logGainMul128Bytes.Add(0);
            logGainMul128Bytes.Add(compArray[1]);
            logGainMul128Bytes.Add(compArray[0]);
        }

        var gainS16BitBytes = new List<byte>();
        foreach (var compArray in gainS16Bits
                     .Select(t => (int)t)
                     .Select(BitConverter.GetBytes))
        {
            gainS16BitBytes.Add(0);
            gainS16BitBytes.Add(0);
            gainS16BitBytes.Add(compArray[1]);
            gainS16BitBytes.Add(compArray[0]);
        }

        var sxExecuteRet = Invoke(() => Service?.SendCIBWave(logGainMul128Bytes, CgCIBWaveType.Sense, cibInformation.PMTId, cibInformation.ChannelId));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        sxExecuteRet = Invoke(() => Service?.SendCIBWave(gainS16BitBytes, CgCIBWaveType.IG, cibInformation.PMTId, cibInformation.ChannelId));
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
        var speedECS = Math.Abs(stopECS - startECS) / time * 1.097912 /* 丁宇提供的常数 */;

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
            ZParam = new SxCollectImgParam.SxZParam()
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
