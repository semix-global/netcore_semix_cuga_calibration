using CommunityToolkit.Diagnostics;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Data.DataStruct.PMT;
using Cuga.Engine.Interface;
using MathNet.Numerics;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using Semix.WcfTransfer.DTO;
using System.Runtime.CompilerServices;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationCIBService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed class CalibrationCIBServiceImpl(
    ICalibrationStageService calibrationStageService) : BaseService<ICgCalibrationService>, ICalibrationCIBService
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

    public SxExecuteRet<IReadOnlyList<bool>> GetAGC(IReadOnlyList<CIBInformation> cibInformations) => ReadRegister(cibInformations, PMTRegEnum.DcAgc, value => value == 1);

    public SxExecuteRet<bool> SetAGC(IReadOnlyList<CIBInformation> cibInformations, bool enable) => WriteRegister(cibInformations, PMTRegEnum.DcAgc, enable ? 0x00_01_00_00 : 0x00_00_00_00);

    public SxExecuteRet<IReadOnlyList<CIBProfileModeEnum>> GetCIBProfileModeEnum(IReadOnlyList<CIBInformation> cibInformations) => ReadRegister(cibInformations, PMTRegEnum.CibProfile, value => value.ToCIBProfileModeEnum());

    public SxExecuteRet<bool> SetCIBProfileModeEnum(IReadOnlyList<CIBInformation> cibInformations, CIBProfileModeEnum cibProfileModeEnum) => WriteRegister(cibInformations, PMTRegEnum.CibProfile, cibProfileModeEnum.ToCIBProfileMode());

    public SxExecuteRet<IReadOnlyList<bool>> GetL0K(IReadOnlyList<CIBInformation> cibInformations) => ReadRegister(cibInformations, PMTRegEnum.L0k, value => value == 1);

    public SxExecuteRet<bool> SetL0K(IReadOnlyList<CIBInformation> cibInformations, bool enable) => WriteRegister(cibInformations, PMTRegEnum.L0k, enable ? 1 : 0);

    public SxExecuteRet<bool> SetGain(IReadOnlyList<CIBInformation> cibInformations, double gain)
    {
        var sxExecuteRet = Invoke(() => Service?.SendDc([.. cibInformations.Select(t => (gain, t.PMTId, t.ChannelId))]));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetMMD(CIBInformation cibInformation, IReadOnlyList<double> logGainMul128U12Bits, IReadOnlyList<double> gainS16Bits)
    {
        var sxExecuteRet = Invoke(() => Service?.SendCIBWave([.. logGainMul128U12Bits], CgCIBWaveType.Sense, cibInformation.PMTId, cibInformation.ChannelId));
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false);

        sxExecuteRet = Invoke(() => Service?.SendCIBWave([.. gainS16Bits], CgCIBWaveType.IG, cibInformation.PMTId, cibInformation.ChannelId));
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

    public SxExecuteRet<bool> SetXPixelSize(ProductivityInformation productivityInformation, double xPixelSize)
    {
        var ret = Invoke(() => Service?.SetRealXPixel(
            productivityInformation.AdaptTo().Mag.ToCgMagTypeEnum(),
            productivityInformation.AdaptTo().Speed.ToCgSpeedLevelType(),
            productivityInformation.AdaptTo().NIOI.ToCgNIOIType(),
            xPixelSize));

        return ret.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(ret.Msg, false)
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
                /*
                 * 范围[-14, 14]归一化数据需要转换为16-bit整数格式进行传输[DSP、FPGA、DAC数字信号转换为模拟信号] // todo: 建议写到cuga里面 [-14, 14] 这个太魔法值了
                 * 16-bit PCM(脉冲编码调制)格式: Int16 范围 [-32768, 32767]
                 *
                 * 归一化映射:
                 *   -1.0 → -32768 (0x8000) Math.Pow(2d, 15d) - 1
                 *    0.0 → 0      (0x0000)
                 *   +1.0 → +32767 (0x7FFF) -Math.Pow(2d, 15d)
                 */
                cibmmdGains[gainIndex] = new CIBMMDGainRelationshipDTO()
                    .WithCIBInformation(cibInformation)
                    .WithGain(gain)
                    .WithSenseU14Bit(Convert.ToInt32(cgDcSenseRelationalModel.AvgSense[gainIndex].sense))
                    .WithGainS16Bit((short)Math.Clamp(Math.Round(gain / 14d * Math.Pow(2d, 15d), MidpointRounding.AwayFromZero), short.MinValue, short.MaxValue));
            }

            results[cibInformationIndex] = cibmmdGains;
        }

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<IReadOnlyList<CIBMMDGainRelationshipDTO>>>(results);
    }

    public SxExecuteRet<bool> SetRTFCParam(ProductivityInformation productivityInformation)
    {
        var sxExecuteRet = Invoke(() => Service!.SetFocusCacheToMachine(productivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum(), productivityInformation.AdaptTo().Mag.ToCgMagTypeEnum(), productivityInformation.AdaptTo().Speed.ToCgSpeedLevelType()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public async Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point centerPosition,
        int imageWidth,
        IReadOnlyList<CIBInformation> cibInformations,
        bool isForward,
        bool isAutoFocus,
        bool isKeepRawImageCIBProfileModeEnum,
        bool isCustomAFParam,
        CancellationToken cancellationToken)
    {
        var getMachineDirectionRet = calibrationStageService.GetMachineDirection();
        if (getMachineDirectionRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<DarkFieldImageDTO>>(getMachineDirectionRet.Msg, []);

        var pmtIds = cibInformations.GroupBy(t => t.PMTId).Select(t => t.Key).ToArray();

        var getPMTImagesRet = await GetPMTImagesAsync(
            cibInformations,
            new SxCollectImgParam
            {
                Type = pmtIds.Length > 1 ? SxCollectImgType.Using : SxCollectImgType.Normal,
                Mag = productivityInformation.AdaptTo().Mag,
                Speed = productivityInformation.AdaptTo().Speed,
                NIOI = productivityInformation.OpticsIlluminationModeEnum.ToSxNIOIEnum(),
                CoordinateSystem = stageCoordinateSystemEnum.ToSxCollectImgCoordinateSystemEnum(),
                CollectMode = SxCollectMode.PW,
                PMTId = pmtIds.Length > 1 ? -1 : pmtIds[0],
                Width = imageWidth,
                StartPoint = [centerPosition.ToSxPointD()],
                IsSingle = true,
                AF = isAutoFocus ? 0 : 1,
                IsForward = isForward,
                IsCalibration = true, /*为true时不下发波形*/
                ImgArrayResoult = false, /*true时返回CgRawImgModel/C2MImgMode(byte[])，false时返回M2CImgSysCollectImgDTO(Url)*/
                FocusParamNoUsed = isCustomAFParam
            },
            stageCoordinateSystemEnum switch
            {
                StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => isForward,
                StageCoordinateSystemEnum.Machine => getMachineDirectionRet.Anything.XDirection > 0 ? isForward : isForward == false,
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<bool>(nameof(stageCoordinateSystemEnum))
            },
            isKeepRawImageCIBProfileModeEnum,
            cancellationToken);

        return getPMTImagesRet.IsSuccess
            ? SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldImageDTO>>(await Task.WhenAll(getPMTImagesRet.Anything.Select(t => Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return new DarkFieldImageDTO().AdaptIn(t);
            }, cancellationToken))))
            : SxExecuteRetHelper.CreateError<IReadOnlyList<DarkFieldImageDTO>>(getPMTImagesRet.Msg, []);
    }

    public async Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        IReadOnlyList<Point> centerPositions,
        int imageWidth,
        CIBInformation cibInformation,
        bool isAutoFocus,
        bool isKeepRawImageCIBProfileModeEnum,
        bool isCustomAFParam,
        CancellationToken cancellationToken)
    {
        var getMachineDirectionRet = calibrationStageService.GetMachineDirection();
        if (getMachineDirectionRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<DarkFieldImageDTO>>(getMachineDirectionRet.Msg, []);

        var isIncreasing = centerPositions.Select(t => t.X).IsIncreasing(true);
        var isDecreasing = centerPositions.Select(t => t.X).IsDecreasing(true);
        if (centerPositions.Count < 2
            || centerPositions.Any(t => t.Y - centerPositions[0].Y == 0) == false // 检查y是否相同
            || (isIncreasing == false && isDecreasing == false)) // 检查x是否递增或者递减
            return ThrowHelper.ThrowArgumentException<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>>(nameof(centerPositions), "Center positions must have the same Y value and X values must be either increasing or decreasing.");

        var isForward = stageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => isIncreasing,
            StageCoordinateSystemEnum.Machine => getMachineDirectionRet.Anything.XDirection > 0 ? isIncreasing : isIncreasing == false,
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<bool>(nameof(stageCoordinateSystemEnum))
        };

        var getDFImgCalibrationRet = GetDFImgCalibration(new SxCollectImgParam
        {
            Type = SxCollectImgType.Normal,
            Mag = productivityInformation.AdaptTo().Mag,
            Speed = productivityInformation.AdaptTo().Speed,
            NIOI = productivityInformation.OpticsIlluminationModeEnum.ToSxNIOIEnum(),
            CoordinateSystem = stageCoordinateSystemEnum.ToSxCollectImgCoordinateSystemEnum(),
            CollectMode = SxCollectMode.PW,
            PMTId = cibInformation.PMTId,
            Width = imageWidth,
            StartPoint = [.. centerPositions.Select(t => t.ToSxPointD())],
            IsSingle = true,
            AF = isAutoFocus ? 0 : 1,
            IsForward = isIncreasing,
            IsCalibration = true, /*为true时不下发波形*/
            ImgArrayResoult = false, /*true时返回CgRawImgModel/C2MImgMode(byte[])，false时返回M2CImgSysCollectImgDTO(Url)*/
            FocusParamNoUsed = isCustomAFParam
        });
        if (getDFImgCalibrationRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<DarkFieldImageDTO>>(getDFImgCalibrationRet.Msg, []);

        var getCIBProfileModeEnumRet = GetCIBProfileModeEnum([cibInformation]);
        if (getCIBProfileModeEnumRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<DarkFieldImageDTO>>(getCIBProfileModeEnumRet.Msg, []);

        var result = new DarkFieldImageDTO[centerPositions.Count];

        var sxExecuteRets = await Task.WhenAll(centerPositions.Index().Select(t => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (index, _) = t;

            var m2CImgSysCollectImgDTOs = getDFImgCalibrationRet.Anything.Where(tt => tt.PMTId == cibInformation.PMTId
                                                                                      && tt.Channel == cibInformation.ChannelId
                                                                                      && tt.Position == index).ToArray();
            if (m2CImgSysCollectImgDTOs.Length != 1) return SxExecuteRetHelper.CreateError($"Failed to missing or repeat for PMT Id:{cibInformation.PMTId} Channel Id:{cibInformation.ChannelId} Position:{index}", false);
            var m2CImgSysCollectImgDTO = m2CImgSysCollectImgDTOs[0];


            result[index] = new DarkFieldImageDTO().AdaptIn(m2CImgSysCollectImgDTO, isForward, getCIBProfileModeEnumRet.Anything[0], isKeepRawImageCIBProfileModeEnum);

            return SxExecuteRetHelper.CreateSuccess(true);
        }, cancellationToken)));

        return sxExecuteRets.All(t => t.IsSuccess)
            ? SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldImageDTO>>(result)
            : SxExecuteRetHelper.CreateError<IReadOnlyList<DarkFieldImageDTO>>(string.Join(Environment.NewLine, sxExecuteRets.Where(t => t.IsSuccess == false).Select(t => t.Msg)), []);
    }

    public async Task<SxExecuteRet<IReadOnlyList<DarkFieldRawScanImageDTO>>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point startPosition,
        Point stopPosition,
        IReadOnlyList<CIBInformation> cibInformations,
        bool isForward,
        bool isAutoFocus,
        bool isKeepRawImageCIBProfileModeEnum,
        bool isCustomAFParam,
        CancellationToken cancellationToken)
    {
        var getMachineDirectionRet = calibrationStageService.GetMachineDirection();
        if (getMachineDirectionRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<DarkFieldRawScanImageDTO>>(getMachineDirectionRet.Msg, []);

        var pmtIds = cibInformations.GroupBy(t => t.PMTId).Select(t => t.Key).ToArray();

        return await GetPMTImagesAsync(
            cibInformations,
            new SxCollectImgParam
            {
                Type = pmtIds.Length > 1 ? SxCollectImgType.Using : SxCollectImgType.Normal,
                Mag = productivityInformation.AdaptTo().Mag,
                Speed = productivityInformation.AdaptTo().Speed,
                NIOI = productivityInformation.OpticsIlluminationModeEnum.ToSxNIOIEnum(),
                CoordinateSystem = stageCoordinateSystemEnum.ToSxCollectImgCoordinateSystemEnum(),
                CollectMode = SxCollectMode.PTP,
                PMTId = pmtIds.Length > 1 ? -1 : pmtIds[0],
                StartPoint = [startPosition.ToSxPointD()],
                EndPoint = [stopPosition.ToSxPointD()],
                IsSingle = true,
                AF = isAutoFocus ? 0 : 1,
                IsForward = isForward,
                IsCalibration = true, /*为true时不下发波形*/
                ImgArrayResoult = false, /*true时返回CgRawImgModel/C2MImgMode(byte[])，false时返回M2CImgSysCollectImgDTO(Url)*/
                FocusParamNoUsed = isCustomAFParam
            },
            stageCoordinateSystemEnum switch
            {
                StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => isForward,
                StageCoordinateSystemEnum.Machine => getMachineDirectionRet.Anything.XDirection > 0 ? isForward : isForward == false,
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<bool>(nameof(stageCoordinateSystemEnum))
            },
            isKeepRawImageCIBProfileModeEnum,
            cancellationToken);
    }

    public async Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point startPosition,
        Point stopPosition,
        double startECS,
        double stopECS,
        IReadOnlyList<CIBInformation> cibInformations,
        bool isForward,
        bool isKeepRawImageCIBProfileModeEnum,
        CancellationToken cancellationToken)
    {
        Guard.IsLessThan(startECS, stopECS);

        var getMachineDirectionRet = calibrationStageService.GetMachineDirection();
        if (getMachineDirectionRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<DarkFieldImageDTO>>(getMachineDirectionRet.Msg, []);

        var pmtIds = cibInformations.GroupBy(t => t.PMTId).Select(t => t.Key).ToArray();

        var time = Math.Abs(startPosition.X - stopPosition.X) / productivityInformation.XSpeedValue;
        var speedECS = Math.Abs(stopECS - startECS) / time;

        var getPMTImagesRet = await GetPMTImagesAsync(
            cibInformations,
            new SxCollectImgParam
            {
                Type = pmtIds.Length > 1 ? SxCollectImgType.Using : SxCollectImgType.Normal,
                Mag = productivityInformation.AdaptTo().Mag,
                Speed = productivityInformation.AdaptTo().Speed,
                NIOI = productivityInformation.OpticsIlluminationModeEnum.ToSxNIOIEnum(),
                CoordinateSystem = stageCoordinateSystemEnum.ToSxCollectImgCoordinateSystemEnum(),
                CollectMode = SxCollectMode.PTP,
                PMTId = pmtIds.Length > 1 ? -1 : pmtIds[0],
                StartPoint = [startPosition.ToSxPointD()],
                EndPoint = [stopPosition.ToSxPointD()],
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
            },
            stageCoordinateSystemEnum switch
            {
                StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => isForward,
                StageCoordinateSystemEnum.Machine => getMachineDirectionRet.Anything.XDirection > 0 ? isForward : isForward == false,
                _ => ThrowHelper.ThrowArgumentOutOfRangeException<bool>(nameof(stageCoordinateSystemEnum))
            },
            isKeepRawImageCIBProfileModeEnum,
            cancellationToken);

        return getPMTImagesRet.IsSuccess
            ? SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldImageDTO>>(await Task.WhenAll(getPMTImagesRet.Anything.Select(t => Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                return new DarkFieldImageDTO().AdaptIn(t);
            }, cancellationToken))))
            : SxExecuteRetHelper.CreateError<IReadOnlyList<DarkFieldImageDTO>>(getPMTImagesRet.Msg, []);
    }

    public SxExecuteRet<(double ECS, double Motor, bool isAFServo)> RuntimeAFCalibration(
        CalChipSiteModelEnum calChipSiteModelEnum,
        ProductivityInformation productivityInformation,
        CIBInformation cibInformation,
        Point? centerMachinePosition = null,
        LaserLightInformation? laserLightInformation = null)
    {
        var sxExecuteRet = Invoke(() => Service!.RuntimeAutofocusCalibration(
            productivityInformation.OpticsIlluminationModeEnum.ToCgNIOITypeEnum(),
            productivityInformation.AdaptTo().Mag.ToCgMagTypeEnum(),
            productivityInformation.AdaptTo().Speed.ToCgSpeedLevelType(),
            calChipSiteModelEnum.ToCgCalChipType(),
            Convert.ToUInt16(cibInformation.PMTId),
            laserLightInformation is not null ? Convert.ToUInt16(laserLightInformation.Level) : null,
            centerMachinePosition?.ToCgPoint()
        ));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<(double Ecs, double AfMotor, bool isAFServo)>(sxExecuteRet.Msg)
            : SxExecuteRetHelper.CreateSuccess<(double Ecs, double AfMotor, bool isAFServo)>((sxExecuteRet.Anything.Ecs, sxExecuteRet.Anything.Offset, true));
    }

    private SxExecuteRet<IReadOnlyList<T>> ReadRegister<T>(IReadOnlyList<CIBInformation> cibInformations, PMTRegEnum pmtRegEnum, Func<int, T> valueConverter, [CallerMemberName] string name = "")
    {
        var readCIBRegRet = Invoke(() => Service?.ReadCIBReg(pmtRegEnum));
        if (readCIBRegRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<T>>(readCIBRegRet.Msg, []);

        var result = new T[cibInformations.Count];

        foreach (var (index, cibInformation) in cibInformations.Index())
        {
            var cibRegisters = readCIBRegRet.Anything.Where(t => t.Id == cibInformation.PMTId && t.Channel == cibInformation.ChannelId).ToArray();
            if (cibRegisters.Length != 1) return SxExecuteRetHelper.CreateError<IReadOnlyList<T>>($"{name} Failed to missing or repeat for PMT Id:{cibInformation.PMTId} Channel Id:{cibInformation.ChannelId}", []);

            result[index] = valueConverter(cibRegisters[0].Value);
        }

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<T>>(result);
    }

    private SxExecuteRet<bool> WriteRegister(IReadOnlyList<CIBInformation> cibInformations, PMTRegEnum pmtRegEnum, int setValue, [CallerMemberName] string name = "")
    {
        var setPmtDiffDataCommonRet = Invoke(() => Service?.SetPmtDiffDataCommon(pmtRegEnum, [.. cibInformations.Select(t => (setValue, t.PMTId, t.ChannelId))]));
        if (setPmtDiffDataCommonRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(setPmtDiffDataCommonRet.Msg, false);

        var readCIBRegRet = Invoke(() => Service?.ReadCIBReg(pmtRegEnum));
        if (readCIBRegRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(readCIBRegRet.Msg, false);

        foreach (var cibInformation in cibInformations)
        {
            var cibRegisters = readCIBRegRet.Anything.Where(t => t.Id == cibInformation.PMTId && t.Channel == cibInformation.ChannelId).ToArray();
            if (cibRegisters.Length != 1) return SxExecuteRetHelper.CreateError($"{name} Failed to missing or repeat for PMT Id:{cibInformation.PMTId} Channel Id:{cibInformation.ChannelId}", false);

            if (pmtRegEnum == PMTRegEnum.DcAgc)
            {
                if (cibRegisters[0].Value != (setValue == 0x00_01_00_00 ? 1 : 0)) return SxExecuteRetHelper.CreateError($"{name} {setValue:x8} Failed for PMT Id:{cibInformation.PMTId} Channel Id:{cibInformation.ChannelId}", false);
            }
            else
            {
                if (cibRegisters[0].Value != setValue) return SxExecuteRetHelper.CreateError($"{name} {setValue:x8} Failed for PMT Id:{cibInformation.PMTId} Channel Id:{cibInformation.ChannelId}", false);
            }
        }

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    private SxExecuteRet<List<M2CImgSysCollectImgDTO>> GetDFImgCalibration(SxCollectImgParam sxCollectImgParam)
    {
        SxExecuteRet setWaitTimeRet;
        SxExecuteRet<List<M2CImgSysCollectImgDTO>> getDFImgCalibrationRet;

        try
        {
            setWaitTimeRet = Invoke(() => Service?.SetWaitTime(60));
            if (setWaitTimeRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<M2CImgSysCollectImgDTO>>(setWaitTimeRet.ErrorMsg, []);

            getDFImgCalibrationRet = Invoke(() => Service?.GetDFImgCalibration(sxCollectImgParam));
            if (getDFImgCalibrationRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<M2CImgSysCollectImgDTO>>(getDFImgCalibrationRet.ErrorMsg, []);
        }
        finally
        {
            setWaitTimeRet = Invoke(() => Service?.SetWaitTime(30));
        }

        return setWaitTimeRet.IsSuccess
            ? getDFImgCalibrationRet
            : SxExecuteRetHelper.CreateError<List<M2CImgSysCollectImgDTO>>(setWaitTimeRet.ErrorMsg, []);
    }

    private async Task<SxExecuteRet<IReadOnlyList<DarkFieldRawScanImageDTO>>> GetPMTImagesAsync(
        IReadOnlyList<CIBInformation> cibInformations,
        SxCollectImgParam sxCollectImgParam,
        bool isForward,
        bool isKeepRawImageCIBProfileModeEnum,
        CancellationToken cancellationToken,
        [CallerMemberName] string name = "")
    {
        var getDFImgCalibrationRet = GetDFImgCalibration(sxCollectImgParam);
        if (getDFImgCalibrationRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<DarkFieldRawScanImageDTO>>(getDFImgCalibrationRet.Msg, []);

        var getCIBProfileModeEnumRet = GetCIBProfileModeEnum(cibInformations);
        if (getCIBProfileModeEnumRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<IReadOnlyList<DarkFieldRawScanImageDTO>>(getCIBProfileModeEnumRet.Msg, []);

        var result = new DarkFieldRawScanImageDTO[cibInformations.Count];

        var sxExecuteRets = await Task.WhenAll(cibInformations.Index().Select(t => Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (index, cibInformation) = t;

            var m2CImgSysCollectImgDTOs = getDFImgCalibrationRet.Anything.Where(tt => tt.PMTId == cibInformation.PMTId && tt.Channel == cibInformation.ChannelId).ToArray();
            if (m2CImgSysCollectImgDTOs.Length != 1) return SxExecuteRetHelper.CreateError($"{name} Failed to missing or repeat for PMT Id:{cibInformation.PMTId} Channel Id:{cibInformation.ChannelId}", false);

            result[index] = new DarkFieldRawScanImageDTO().AdaptIn(m2CImgSysCollectImgDTOs[0], isForward, getCIBProfileModeEnumRet.Anything[index], isKeepRawImageCIBProfileModeEnum);

            return SxExecuteRetHelper.CreateSuccess(true);
        }, cancellationToken)));

        return sxExecuteRets.All(t => t.IsSuccess)
            ? SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldRawScanImageDTO>>(result)
            : SxExecuteRetHelper.CreateError<IReadOnlyList<DarkFieldRawScanImageDTO>>(string.Join(Environment.NewLine, sxExecuteRets.Where(t => t.IsSuccess == false).Select(t => t.Msg)), []);
    }
}