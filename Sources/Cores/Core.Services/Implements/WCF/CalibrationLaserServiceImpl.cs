using CommunityToolkit.Diagnostics;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Extensions;
using Core.Models.Helper;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Pattern;
using Core.Models.Models.Setting;
using Core.Services.Interfaces;
using Cuga.Data.DataStruct.Basic;
using Cuga.Data.DataStruct.PMT;
using Cuga.Engine.Interface;
using MathNet.Numerics.LinearAlgebra;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Helpers.Extensions;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using System.IO;
using System.Text;

namespace Core.Services.Implements.WCF;

[IOCAppService(ServiceType = typeof(ICalibrationLaserService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Production | IOCEnvironmentEnum.Staging)]
public sealed partial class CalibrationLaserServiceImpl(
    ICalibrationAlgorithmService calibrationAlgorithmService,
    ICalibrationStageService calibrationStageService,
    ICalibrationConfigService calibrationConfigService,
    CalibrationSetting calibrationSetting)
    : BaseService<ICgCalibrationService>, ICalibrationLaserService
{
    private List<CgLightConfig>? _cgLightConfigList;
    private List<(int PmtId, bool IsUsed, List<int> ChannelIdList)>? _pmtConfigList;

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

    public SxExecuteRet<(Point PD1, Point PD2)> GetLaserBeamPosition()
    {
        var sxExecuteRet = Invoke(() => Service!.ReadLaserBeamPos());
        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, (Point.Origin, Point.Origin))
            : SxExecuteRetHelper.CreateSuccess((new Point(sxExecuteRet.Anything.PD_X_1_FPOS, sxExecuteRet.Anything.PD_Y_1_FPOS) * 1000, new Point(sxExecuteRet.Anything.PD_X_2_FPOS, sxExecuteRet.Anything.PD_Y_2_FPOS) * 1000));
    }

    public SxExecuteRet<(Point PD1, Point PD2)> GetLaserOriginPosition()
    {
        var sxExecuteRet = Invoke(() => Service!.ReadLaserBeamOriginPos());
        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, (Point.Origin, Point.Origin))
            : SxExecuteRetHelper.CreateSuccess((new Point(sxExecuteRet.Anything.PD_X_1_FPOS, sxExecuteRet.Anything.PD_Y_1_FPOS) * 1000, new Point(sxExecuteRet.Anything.PD_X_2_FPOS, sxExecuteRet.Anything.PD_Y_2_FPOS) * 1000));
    }

    public SxExecuteRet<bool> AdjustmentOfReflector(bool isEnable)
    {
        var sxExecuteRet = Invoke(() => Service!.LaserBeamAdjust(isEnable));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetLaserPowerMeterLightIntensity()
    {
        var sxExecuteRet = Invoke(() => Service!.ReadDynamometer());

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<double>(sxExecuteRet.Msg)
            : SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything);
    }

    public SxExecuteRet<double> LightLevelToLightCoefficient(double level)
    {
        var sxExecuteRet = GetLightConfigList();
        var cgLightConfig = sxExecuteRet.Anything.SingleOrDefault(m => Math.Abs(m.LightProp - level) < Constants.Tolerance);

        return sxExecuteRet.IsSuccess == false || cgLightConfig is null
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, 0d)
            : SxExecuteRetHelper.CreateSuccess(cgLightConfig.LightCoeff);
    }

    public SxExecuteRet<double> LightCoefficientToLightLevel(double coefficient)
    {
        var sxExecuteRet = GetLightConfigList();
        var cgLightConfig = sxExecuteRet.Anything.SingleOrDefault(m => Math.Abs(m.LightCoeff - coefficient) < Constants.Tolerance);

        return sxExecuteRet.IsSuccess == false || cgLightConfig is null
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, 0d)
            : SxExecuteRetHelper.CreateSuccess(cgLightConfig.LightProp);
    }

    public SxExecuteRet<bool> SendOpticsMagType(OpticsMagTypeEnum yOpticsMagTypeEnum)
    {
        var sxExecuteRet = Invoke(() => Service!.RefreshMag(Convert.ToInt32(yOpticsMagTypeEnum.ToCgMagTypeEnum())));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendPrescanByCoefficient(OpticsMagTypeEnum yOpticsMagTypeEnum, double coefficient)
    {
        var prescanFilePathRet = calibrationConfigService.GetPrescanFilePath(yOpticsMagTypeEnum);
        if (prescanFilePathRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(prescanFilePathRet.Msg, false);

        var darkFieldPrescanDtoRet = ReadPrescanByFile(prescanFilePathRet.Anything, coefficient);
        if (darkFieldPrescanDtoRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(darkFieldPrescanDtoRet.Msg, false);

        var sendPrescanByListRet = SendPrescanByList(darkFieldPrescanDtoRet.Anything);
        if (sendPrescanByListRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sendPrescanByListRet.Msg, false);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendPrescanByList(DarkFieldPrescanDto darkFieldPrescanDto)
    {
        var sxExecuteRet = Invoke(() => Service!.SendPrescanFile_Illumination(darkFieldPrescanDto.RegNum, darkFieldPrescanDto.ZeroNum, darkFieldPrescanDto.PrescanByteList));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendChirpAodByList(DarkFieldChirpAodWaveDto darkFieldChirpAodWaveDto)
    {
        var sxExecuteRet = Invoke(() => Service!.SetChirpCalibration(darkFieldChirpAodWaveDto.ChirpAodWaveByteList, darkFieldChirpAodWaveDto.RegNum, darkFieldChirpAodWaveDto.ZeroNum));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleOpticsAodWorkingMode(OpticsAodWorkingModeEnum opticsAodWorkingModeEnum)
    {
        var sxExecuteRet = Invoke(() => Service!.SetAOD_NO(opticsAodWorkingModeEnum.ToOpticsAodWorkingMode()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleOpticsPolarization(OpticsPolarizationTypeEnum opticsPolarizationTypeEnum)
    {
        var sxExecuteRet = Invoke(() => Service!.SetPolarization(opticsPolarizationTypeEnum.ToCgPolarizationTypeEnum()));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetAodDelayValue(OpticsMagTypeEnum yOpticsMagTypeEnum, double prescanAodDelay, double chirpAodDelay)
    {
        var sxExecuteRet = Invoke(() => Service!.SetMagAndWaveZero(yOpticsMagTypeEnum.ToCgMagTypeEnum(), Convert.ToInt32(chirpAodDelay), Convert.ToInt32(prescanAodDelay)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleCIBControlTypeAndProfileType(CIBConfiguration cIbConfiguration, int pmtId, int channelId)
    {
        var toggleAutoGainRet = ToggleEnableAutoGainControl(cIbConfiguration.IsAutoGain, pmtId, channelId);
        if (toggleAutoGainRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(toggleAutoGainRet.ErrorMsg, false);

        if (cIbConfiguration.IsAutoGain == false)
        {
            var setGainRet = SetGain(cIbConfiguration.DcGainVoltage, pmtId, channelId);
            if (setGainRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(setGainRet.ErrorMsg, false);
        }

        var toggleL0kRet = ToggleEnableL0K(cIbConfiguration.IsL0k, pmtId, channelId);
        if (toggleL0kRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(toggleL0kRet.ErrorMsg, false);

        var toggleProfileTypeRet = ToggleProfileType(cIbConfiguration.CIBProfileMode, pmtId, channelId);
        if (toggleProfileTypeRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(toggleProfileTypeRet.ErrorMsg, false);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableAutoGainControl(bool enable, int pmtId, int channelId) => SetCIBControlValue(enable ? 0x00_00_01_00 : 0x00_00_00_00, pmtId, channelId, sendDataList => Invoke(() => Service!.SetPmtDiffDataCommon(PMTRegEnum.DcAgc, sendDataList)));

    public SxExecuteRet<bool> ToggleProfileType(CIBProfileModeEnum cibProfileModeEnum, int pmtId, int channelId) => SetCIBControlValue(cibProfileModeEnum.ToCIBProfile(), pmtId, channelId, sendDataList => Invoke(() => Service!.SetPmtDiffDataCommon(PMTRegEnum.CibProfile, sendDataList)));

    public SxExecuteRet<bool> ToggleEnableMarkMode(bool enable, int pmtId, int channelId) => SetCIBControlValue(enable ? 1 : 0, pmtId, channelId, sendDataList => Invoke(() => Service!.SetPmtDiffDataCommon(PMTRegEnum.MarkMode, sendDataList)));

    public SxExecuteRet<bool> ToggleEnableL0K(bool enable, int pmtId, int channelId) => SetCIBControlValue(enable ? 1 : 0, pmtId, channelId, sendDataList => Invoke(() => Service!.SetPmtDiffDataCommon(PMTRegEnum.L0k, sendDataList)));

    public SxExecuteRet<bool> SetGain(double gain, int pmtId, int channelId) => SetCIBControlValue(gain, pmtId, channelId, sendDataList => Invoke(() => /* direct current */Service!.SendDc(sendDataList)));

    public SxExecuteRet<bool> SetSaturation(double saturation)
    {
        var sxExecuteRet = Invoke(() => Service!.SetDCSaturation(saturation));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    private SxExecuteRet<bool> SetCIBControlValue<T>(T value, int pmtId, int channelId, Func<List<(T Data, int PMTId, int ChannelId)>, SxExecuteRet> func)
    {
        var pmtConfigListSxExecuteRet = GetPmtConfigList();
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

    public SxExecuteRet<List<(int PmtId, bool IsUsed, List<int> ChannelIdList)>> GetPmtConfigList()
    {
        if (_pmtConfigList is not null) return SxExecuteRetHelper.CreateSuccess(_pmtConfigList);

        var sxExecuteRet = Invoke(() => Service!.GetPmtState());

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<(int PmtId, bool IsUsed, List<int> ChannelIdList)>>(sxExecuteRet.Msg, []);
        var result = (
            from item in sxExecuteRet.Anything
            group item by item.id
            into g
            select (
                PmtId: g.Key,
                IsUsed: g.All(x => x.used),
                ChannelIdList: g.Select(x => x.chl).ToList()
            )
        ).ToList();

        Guard.IsTrue(result.All(item => item.ChannelIdList.SequenceEqual(result.First().ChannelIdList)), "Channel Id is not equal");
        Guard.IsTrue(result.Any(item => item.IsUsed), "Pmt Config List is not used");

        _pmtConfigList = result;

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<List<List<double>>> GetPmtDataList(int count, int pmtId, int channelId)
    {
        var sxExecuteRet = Invoke(() => Service!.GetPMTData_Appoint(pmtId, channelId));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<List<double>>>(sxExecuteRet.Msg, []);
        if (sxExecuteRet.Anything.Count == 0) return SxExecuteRetHelper.CreateError<List<List<double>>>("Pmt Value List is empty", []);

        return SxExecuteRetHelper.CreateSuccess(new List<List<double>> { sxExecuteRet.Anything });
    }

    public SxExecuteRet<List<DarkFieldPmtDataDto>> GetPmtDataList()
    {
        var pmtRet = Invoke(() => Service!.GetPMTDataALL());

        if (pmtRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<DarkFieldPmtDataDto>>(pmtRet.ErrorMsg, []);

        var result = new List<DarkFieldPmtDataDto>(pmtRet.Anything.Count);
        result.AddRange(pmtRet.Anything.Select(pmtDataModel => new DarkFieldPmtDataDto().AdaptIn(pmtDataModel)));

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<List<List<double>>> GetPmtSenseDataList(int count, int pmtId, int channelId)
    {
        var sxExecuteRet = Invoke(() => Service!.GetSenseData(count, pmtId, channelId));

        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<List<double>>>(sxExecuteRet.Msg, []);
        if (sxExecuteRet.Anything.Data.Count != count) return SxExecuteRetHelper.CreateError<List<List<double>>>("Pmt Sense Value List is empty", []);

        return SxExecuteRetHelper.CreateSuccess(sxExecuteRet.Anything.Data);
    }

    public SxExecuteRet<List<DarkFieldPmtDelayDto>> GetPmtDelayList()
    {
        var pmtRet = Invoke(() => Service!.GetPMTDelay());
        if (pmtRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<DarkFieldPmtDelayDto>>(pmtRet.ErrorMsg, []);

        var result = new List<DarkFieldPmtDelayDto>(pmtRet.Anything.Count);
        result.AddRange(pmtRet.Anything.Select(pmtDelayModel => new DarkFieldPmtDelayDto().AdaptIn(pmtDelayModel)));

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<bool> SetPmtDelayList(List<DarkFieldPmtDelayDto> darkFieldPmtDelayDtoList)
    {
        var pmtDelayModel = darkFieldPmtDelayDtoList.Select(item => item.AdaptTo()).ToList();

        var pmtRet = Invoke(() => Service!.SetPMTDelay(pmtDelayModel));

        return pmtRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(pmtRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendPmtGain(double[] gains, int pmtId, int channelId)
    {
        // pmt增益电压范围 [-14, 14]
        var senseVector = Vector<double>.Build.Dense(gains) / 14;
        var senseValues = senseVector
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
        var pmtVector = Vector<double>.Build.Dense(gains) * -1 / 14;
        var pmtValues = pmtVector
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

        var sxExecuteRet = Invoke(() => Service!.SendChirp(pmtData, senseData, pmtId, channelId));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendPmtGain(List<string> pmtData, List<string> igData, int pmtId, int channelId)
    {
        var sxExecuteRet = Invoke(() => Service!.SendPMTGain(pmtData, igData, pmtId, channelId));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRet.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double Ecs, double AfMotor)> RuntimeAfCalibration(CalChipSiteModelEnum calChipSiteModelEnum, Point position, double coefficient)
    {
        var executeRet = LightCoefficientToLightLevel(coefficient);
        if (executeRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<(double Ecs, double AfMotor)>(executeRet.ErrorMsg);

        var sxExecuteRet = Invoke(() => Service!.RuntimeAutofocusCalibration(calChipSiteModelEnum.ToCgCalChipType(), null, Convert.ToUInt16(executeRet.Anything)));

        return sxExecuteRet.IsSuccess == false
            ? SxExecuteRetHelper.CreateError<(double Ecs, double AfMotor)>(sxExecuteRet.Msg)
            : SxExecuteRetHelper.CreateSuccess<(double Ecs, double AfMotor)>((sxExecuteRet.Anything.Ecs, sxExecuteRet.Anything.Offset));
    }

    public SxExecuteRet<int> GetDarkFieldLineScanImageYPixelHeight(OpticsMagTypeEnum yOpticsMagTypeEnum, bool isCuttingPixelHeight)
    {
        if (isCuttingPixelHeight)
        {
            var sxExecuteRet = Invoke(() => Service!.GetSpeedInfo(yOpticsMagTypeEnum.ToSxMagEnum()));

            return sxExecuteRet.IsSuccess == false
                ? SxExecuteRetHelper.CreateError<int>(sxExecuteRet.Msg)
                : SxExecuteRetHelper.CreateSuccess(Convert.ToInt32(sxExecuteRet.Anything.YPixel));
        }
        else
        {
            var sxExecuteRet = Invoke(() => Service!.GetPmtDataLineHeight(yOpticsMagTypeEnum.ToCgMagTypeEnum()));

            return sxExecuteRet.IsSuccess == false
                ? SxExecuteRetHelper.CreateError<int>(sxExecuteRet.Msg)
                : SxExecuteRetHelper.CreateSuccess(Convert.ToInt32(sxExecuteRet.Anything));
        }
    }

    public SxExecuteRet<List<DarkFieldImageDto>> GetDarkFieldLineScanImageList(
        Point position,
        int xWidthPixel,
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward)
    {
        var darkFieldImagesRet = stageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Bright or StageCoordinateSystemEnum.Dark => Invoke(() => Service!.LoadRawImg_Mag_Calibration(
                yOpticsMagTypeEnum.ToSxMagEnum(),
                xStageSpeedEnum.ToSxSpeedEnum(),
                xWidthPixel,
                position.ToSxPointD(),
                pmtId,
                /*是否单向*/isSingle: true,
                /*是否正向*/isForward: isForward,
                /*是否开启自动聚焦*/af: isAutoFocus ? 0 : 1)),
            StageCoordinateSystemEnum.Machine => Invoke(() => Service!.LoadRawImg_Mag_ToStagePos(
                yOpticsMagTypeEnum.ToSxMagEnum(),
                xStageSpeedEnum.ToSxSpeedEnum(),
                xWidthPixel,
                position.ToSxPointD(),
                pmtId,
                /*是否单向*/isSingle: true,
                /*是否正向*/isForward: isForward,
                /*是否开启自动聚焦*/af: isAutoFocus ? 0 : 1)),
            _ => throw new ArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum), stageCoordinateSystemEnum, null)
        };

        if (darkFieldImagesRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<DarkFieldImageDto>>(darkFieldImagesRet.ErrorMsg, []);
        if (darkFieldImagesRet.Anything.Count != 3) return SxExecuteRetHelper.CreateError<List<DarkFieldImageDto>>("Dark Images Count is not 3", []);

        var result = new List<DarkFieldImageDto>(darkFieldImagesRet.Anything.Count);

        foreach (var c2MImgModel in darkFieldImagesRet.Anything)
        {
            var (image, matrix) = calibrationAlgorithmService.ToImageInfo(c2MImgModel.Img);
            result.Add(new DarkFieldImageDto { Image = image, Matrix = matrix }.AdaptIn(c2MImgModel));
        }

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<List<DarkFieldRawScanImageDto>> GetDarkFieldLineScanImageList(
        Point startPosition,
        Point endPosition,
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward)
    {
        var darkFieldImagesRet = stageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Machine => Invoke(() => Service!.LoadRawImg_Mag_PTP(
                yOpticsMagTypeEnum.ToSxMagEnum(),
                xStageSpeedEnum.ToSxSpeedEnum(),
                isForward ? startPosition.ToSxPointD() : endPosition.ToSxPointD(),
                isForward ? endPosition.ToSxPointD() : startPosition.ToSxPointD(),
                pmtId,
                /*是否单向*/isSingle: true,
                /*是否开启自动聚焦*/af: isAutoFocus ? 0 : 1)),
            _ => throw new ArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum), stageCoordinateSystemEnum, null)
        };

        if (darkFieldImagesRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<DarkFieldRawScanImageDto>>(darkFieldImagesRet.ErrorMsg, []);
        if (darkFieldImagesRet.Anything.Count != 3) return SxExecuteRetHelper.CreateError<List<DarkFieldRawScanImageDto>>("Dark Images Count is not 3", []);

        var result = new List<DarkFieldRawScanImageDto>(darkFieldImagesRet.Anything.Count);

        foreach (var c2MImgModel in darkFieldImagesRet.Anything)
        {
            result.Add(new DarkFieldRawScanImageDto().AdaptIn(c2MImgModel));
        }

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<List<List<DarkFieldImageDto>>> GetChuckDarkFieldRowLineScanImageList(
        List<Point> machinePositionList,
        int xWidthPixel,
        double xPixelSize,
        OpticsMagTypeEnum yOpticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus)
    {

        if (machinePositionList.Count < 2
            || machinePositionList.Any(t => t.Y - machinePositionList[0].Y == 0) == false // 检查y是否相同
            || machinePositionList.Zip(machinePositionList.Skip(1), (current, next) => current.X <= next.X).All(b => b) == false) // 检查x是否递增
            throw new ArgumentOutOfRangeException(nameof(machinePositionList), machinePositionList, null);

        var directionRect = calibrationStageService.GetMachineDirection();
        if (directionRect.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<List<DarkFieldImageDto>>>(directionRect.ErrorMsg, []);
        var directionX = directionRect.Anything.XDirection;

        var picturePixelHeightRet = GetDarkFieldLineScanImageYPixelHeight(yOpticsMagTypeEnum, true);
        if (picturePixelHeightRet.IsSuccess == false) return SxExecuteRetHelper.CreateError<List<List<DarkFieldImageDto>>>(picturePixelHeightRet.ErrorMsg, []);
        var height = picturePixelHeightRet.Anything;

        var scanLineXPixelSize = calibrationSetting.SettingCommonParam.GetScanLineXPixelSize(yOpticsMagTypeEnum, xStageSpeedEnum);
        var extendWidth = xWidthPixel * scanLineXPixelSize / 2.0;

        // 计算采图的起点终点机械坐标
        var startPoint = new Point(machinePositionList[0].X - extendWidth, machinePositionList[0].Y);
        var endPoint = new Point(machinePositionList.Last().X + extendWidth * 3, machinePositionList[0].Y); // 后面多采集一段，防止最后一段数据不全

        // 从起点到终点采图，输出三通道长图片
        var darkFieldImagesRet = stageCoordinateSystemEnum switch
        {
            StageCoordinateSystemEnum.Machine => Invoke(() => Service!.LoadRawImg_Mag_PTP(
                yOpticsMagTypeEnum.ToSxMagEnum(),
                xStageSpeedEnum.ToSxSpeedEnum(),
                startPoint.ToSxPointD(),
                endPoint.ToSxPointD(),
                pmtId,
                /*是否单向*/isSingle: true,
                /*是否开启自动聚焦*/af: isAutoFocus ? 0 : 1)),
            _ => throw new ArgumentOutOfRangeException(nameof(stageCoordinateSystemEnum), stageCoordinateSystemEnum, null)
        };

        // 获得stageMap两点x像素间隔（剪裁小图的宽度width）
        var heightPixelOfByte = height * 2;
        var splitImageLength = xWidthPixel * heightPixelOfByte;
        var pointerList = Enumerable
            .Range(0, machinePositionList.Count)
            .Select((count, index) => index == 0 ? 0 : count * (machinePositionList[1].X - machinePositionList[0].X) / xPixelSize * heightPixelOfByte)
            .Select(Convert.ToInt64)
            .ToList();

        var splitImagesAllChannels = new List<List<DarkFieldImageDto>>();
        foreach (var channelId in Enumerable.Range(0, 3))
        {
            using var fileSteam = File.OpenRead(darkFieldImagesRet.Anything[channelId].Url);
            using var binaryReader = new BinaryReader(fileSteam, Encoding.UTF8, false);

            var (_, bodyBytesStartIndex, bodyBytesLength) = RawImageHelper.GetSize(binaryReader);

            var splitImages = new List<DarkFieldImageDto>();
            foreach (var (index, pointer) in pointerList.Select((t, i) => (Index: i, Pointer: t)))
            {
                var pointerTemp = pointer - pointer % heightPixelOfByte; // dieWidthPixel不是整数倍, 需要对齐
                byte[] array;
                if (pointerTemp + splitImageLength > bodyBytesLength)
                {
                    if (index != pointerList.Count - 1) ThrowHelper.ThrowArgumentException("Data length is not a multiple of width.");

                    var offset = xWidthPixel - (bodyBytesLength - pointerTemp) / heightPixelOfByte;

                    pointerTemp += offset * heightPixelOfByte;
                    pointerTemp -= pointerTemp % heightPixelOfByte; // dieWidthPixel不是整数倍, 需要对齐

                    fileSteam.Seek(bodyBytesStartIndex + pointerTemp, SeekOrigin.Begin);
                    array = binaryReader.ReadBytes();
                }
                else
                {
                    fileSteam.Seek(bodyBytesStartIndex + pointerTemp, SeekOrigin.Begin);
                    array = binaryReader.ReadBytes(splitImageLength);
                }

                var splitRawBytes = calibrationAlgorithmService.ToRawBytes(array, new Size(xWidthPixel, height));
                var (image, matrix, horizontalFlipRawBytes) = calibrationAlgorithmService.ToHorizontalFlipImageInfo(splitRawBytes);
                // 三通道图片分别用以上ROI集合裁剪
                var splitImageDto = new DarkFieldImageDto { PmtId = 8, ChannelId = channelId + 1, Bytes = horizontalFlipRawBytes, Image = image, Matrix = matrix, Height = height, Width = xWidthPixel };
                splitImages.Add(splitImageDto);
            }

            splitImagesAllChannels.Add(splitImages);
        }

        return SxExecuteRetHelper.CreateSuccess(splitImagesAllChannels);
    }

    private SxExecuteRet<List<CgLightConfig>> GetLightConfigList()
    {
        if (_cgLightConfigList is not null) return SxExecuteRetHelper.CreateSuccess(_cgLightConfigList);

        var sxExecuteRet = Invoke(() => Service!.GetLightConfig());
        if (sxExecuteRet.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRet.ErrorMsg, new List<CgLightConfig>());
        if (sxExecuteRet.Anything.Length == 0) return SxExecuteRetHelper.CreateError<List<CgLightConfig>>("Lens List is empty", []);

        _cgLightConfigList = [.. sxExecuteRet.Anything];

        return SxExecuteRetHelper.CreateSuccess(_cgLightConfigList);
    }
}