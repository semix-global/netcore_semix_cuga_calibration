using System.IO;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Optics;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.Common.AODWaveform;
using Core.Models.Models.Common.AODWaveform.Generates;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using Core.Models.Models.Setting;
using Cuga.Data.DataStruct.PMT;
using CommunityToolkit.Diagnostics;
using Core.Models.Extensions;
using Semix.WcfTransfer.DTO;

#if NET
using Core.Services.Implements.GRPC;
using Semix.GRPC.DTO;
#else
using Core.Services.Implements.WCF;
using Semix.WcfTransfer.DTO;
#endif

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationLaserService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationLaserServiceMockImpl(
    ICalibrationAlgorithmService calibrationAlgorithmService,
    ICalibrationStageService calibrationStageService,
    ICalibrationConfigService calibrationConfigService,
    CalibrationSetting calibrationSetting) : ICalibrationLaserService
{
    private static readonly Random Random = new();
    private readonly CalibrationLaserServiceImpl _calibrationLaserServiceImpl = new(calibrationAlgorithmService, calibrationStageService, calibrationConfigService, calibrationSetting);

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

        _curPosition = new Point(Random.Next(240, 300), Random.Next(240, 300));

        return SxExecuteRetHelper.CreateSuccess((_curPosition, _curPosition));
    }

    public SxExecuteRet<(Point PD1Point, Point PD2Point)> GetLaserBeamOriginPoint()
    {
        Thread.Sleep(100);

        _curPosition = new Point(Random.Next(240, 300), Random.Next(240, 300));

        return SxExecuteRetHelper.CreateSuccess((_curPosition, _curPosition));
    }

    public SxExecuteRet<bool> AdjustBeamStabilizer(bool isEnable)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<double> GetOpticalPowerMeter()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(Convert.ToDouble(Random.Next(1, 30) * _coefficient));
    }

    public SxExecuteRet<IReadOnlyList<LaserLightInformation>> GetLaserLightInformations()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<LaserLightInformation>>([
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
        ]);
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

    public SxExecuteRet<IReadOnlyList<ProductivityInformation>> GetProductivityInformations()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<ProductivityInformation>>([
            ProductivityInformation.Default.Clone().AdaptIn(new C2MProductivityInfo { Name = "S5", Mag = SxMAGEnum.Low, Speed = SxSpeedEnum.High, IsUsed = true }),
            ProductivityInformation.Default.Clone().AdaptIn(new C2MProductivityInfo { Name = "S10", Mag = SxMAGEnum.Low, Speed = SxSpeedEnum.Low, IsUsed = true }),
            ProductivityInformation.Default.Clone().AdaptIn(new C2MProductivityInfo { Name = "S25", Mag = SxMAGEnum.Mid, Speed = SxSpeedEnum.High, IsUsed = true }),
            ProductivityInformation.Default.Clone().AdaptIn(new C2MProductivityInfo { Name = "S40", Mag = SxMAGEnum.Mid, Speed = SxSpeedEnum.Low, IsUsed = true }),
            ProductivityInformation.Default.Clone().AdaptIn(new C2MProductivityInfo { Name = "S55", Mag = SxMAGEnum.High, Speed = SxSpeedEnum.High, IsUsed = true }),
            ProductivityInformation.Default.Clone().AdaptIn(new C2MProductivityInfo { Name = "S90", Mag = SxMAGEnum.High, Speed = SxSpeedEnum.Low, IsUsed = true }),
        ]);
    }

    public SxExecuteRet<bool> ToggleOpticsMagType(OpticsMagTypeEnum opticsMagTypeEnum)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleOpticsMagType(ProductivityInformation productivityInformation)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleOpticsAODWorkingMode(OpticsAODWorkingModeEnum opticsAodWorkingModeEnum)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleOpticsPolarization(OpticsPolarizationTypeEnum opticsPolarizationTypeEnum)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetAODDelayValue(OpticsMagTypeEnum opticsMagTypeEnum, double prescanAodDelay, double chirpAodDelay)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetAODDelayValue(ProductivityInformation productivityInformation, double prescanAodDelay, double chirpAodDelay)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetDefaultPrescanAODWaveProfileByCoefficient(OpticsMagTypeEnum opticsMagTypeEnum, double coefficient)
    {
        var sxExecuteRetByGetPrescanAODWaveProfiles = calibrationConfigService.GetPrescanAODWaveProfiles(opticsMagTypeEnum);
        if (sxExecuteRetByGetPrescanAODWaveProfiles.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRetByGetPrescanAODWaveProfiles.Msg, false);

        var prescanAODWaveProfiles = sxExecuteRetByGetPrescanAODWaveProfiles.Anything;

        foreach (var aodWaveProfile in prescanAODWaveProfiles) aodWaveProfile.ApplyCoefficient(coefficient);

        var sxExecuteRetBySetPrescanAODWaveProfiles = SetPrescanAODWaveProfiles(prescanAODWaveProfiles);

        return sxExecuteRetBySetPrescanAODWaveProfiles.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRetBySetPrescanAODWaveProfiles.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetDefaultPrescanAODWaveProfileByCoefficient(ProductivityInformation productivityInformation, double coefficient)
    {
        var sxExecuteRetByGetPrescanAODWaveProfiles = calibrationConfigService.GetPrescanAODWaveProfiles(productivityInformation.AdaptTo().Mag.ToOpticsMagTypeEnum());
        if (sxExecuteRetByGetPrescanAODWaveProfiles.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRetByGetPrescanAODWaveProfiles.Msg, false);

        var prescanAODWaveProfiles = sxExecuteRetByGetPrescanAODWaveProfiles.Anything;

        foreach (var aodWaveProfile in prescanAODWaveProfiles) aodWaveProfile.ApplyCoefficient(coefficient);

        var sxExecuteRetBySetPrescanAODWaveProfiles = SetPrescanAODWaveProfiles(prescanAODWaveProfiles);

        return sxExecuteRetBySetPrescanAODWaveProfiles.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRetBySetPrescanAODWaveProfiles.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetPrescanAODWaveProfiles(IReadOnlyList<PrescanAODWaveformProfile> prescanAODWaveProfiles)
    {
        foreach (var aodWaveProfile in prescanAODWaveProfiles)
        {
            Guard.IsNotEmpty(aodWaveProfile.ByteList);
        }

        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetDefaultChirpAODWaveProfile(OpticsMagTypeEnum opticsMagTypeEnum)
    {
        var sxExecuteRetByGetChirpAODWaveProfiles = calibrationConfigService.GetChirpAODWaveProfiles(opticsMagTypeEnum);
        if (sxExecuteRetByGetChirpAODWaveProfiles.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRetByGetChirpAODWaveProfiles.Msg, false);

        var sxExecuteRetBySetPrescanAODWaveProfiles = SetChirpAODWaveProfiles(sxExecuteRetByGetChirpAODWaveProfiles.Anything);

        return sxExecuteRetBySetPrescanAODWaveProfiles.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRetBySetPrescanAODWaveProfiles.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetDefaultChirpAODWaveProfile(ProductivityInformation productivityInformation)
    {
        var sxExecuteRetByGetChirpAODWaveProfiles = calibrationConfigService.GetChirpAODWaveProfiles(productivityInformation.AdaptTo().Mag.ToOpticsMagTypeEnum());
        if (sxExecuteRetByGetChirpAODWaveProfiles.IsSuccess == false) return SxExecuteRetHelper.CreateError(sxExecuteRetByGetChirpAODWaveProfiles.Msg, false);

        var sxExecuteRetBySetPrescanAODWaveProfiles = SetChirpAODWaveProfiles(sxExecuteRetByGetChirpAODWaveProfiles.Anything);

        return sxExecuteRetBySetPrescanAODWaveProfiles.IsSuccess == false
            ? SxExecuteRetHelper.CreateError(sxExecuteRetBySetPrescanAODWaveProfiles.Msg, false)
            : SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetChirpAODWaveProfiles(IReadOnlyList<ChirpAODWaveformProfile> chirpAODWaveProfiles)
    {
        foreach (var aodWaveProfile in chirpAODWaveProfiles)
        {
            Guard.IsNotEmpty(aodWaveProfile.ByteList);
        }

        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<IReadOnlyList<PrescanAODWaveformProfile>> GeneratePrescanAodWaves(GeneratePrescanAODWaveformParam generatePrescanAODWaveformParam)
    {
        return _calibrationLaserServiceImpl.GeneratePrescanAodWaves(generatePrescanAODWaveformParam);
    }

    public SxExecuteRet<IReadOnlyList<ChirpAODWaveformProfile>> GenerateChirpAodWaves(GenerateChirpAODWaveformParam generateChirpAODWaveformParam)
    {
        return _calibrationLaserServiceImpl.GenerateChirpAodWaves(generateChirpAODWaveformParam);
    }

    public SxExecuteRet<bool> ToggleCIBControlTypeAndProfileType(CIBConfiguration cIbConfiguration, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableAutoGainControl(bool enable, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleProfileMode(CIBProfileModeEnum cibProfileModeEnum, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableMarkMode(bool enable, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableL0K(bool enable, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetGain(double gain, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetSaturation(double saturation)
    {
        Thread.Sleep(100);
        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<IReadOnlyList<(int PmtId, bool IsUsed, IReadOnlyList<int> ChannelIdList)>> GetCIBConfigList()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<(int PmtId, bool IsUsed, IReadOnlyList<int> ChannelIdList)>>([.. Enumerable.Range(1, 15).Select(t => (t, true, (List<int>)[1, 2, 3]))]);
    }

    public SxExecuteRet<IReadOnlyList<IReadOnlyList<double>>> GetCIBOfPMTDataList(int count, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<IReadOnlyList<double>>>(new List<List<double>> { Enumerable.Range(1, 800).Select(_ => Random.NextDouble() * 3950).ToList() });
    }

    public SxExecuteRet<IReadOnlyList<DarkFieldPmtDataDto>> GetCIBOfPMTDataList()
    {
        var result = new List<DarkFieldPmtDataDto>();

        for (var i = 1; i < 16; i++)
        {
            for (var j = 1; j < 4; j++)
            {
                var pmtDataDto = new DarkFieldPmtDataDto
                {
                    PmtId = i,
                    Channel = j,
                    LineCount = 800,
                    Data = [.. Enumerable.Range(1, 800).Select(_ => Random.NextDouble())]
                };
                result.Add(pmtDataDto);
            }
        }

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldPmtDataDto>>(result);
    }

    public SxExecuteRet<IReadOnlyList<IReadOnlyList<double>>> GetCIBOfSenseDataList(int count, int pmtId, int channelId)
    {
        Thread.Sleep(2000);

        var result = new List<List<double>>(count);

        for (var i = 0; i < count; i++)
        {
            result.Add([.. Enumerable.Range(1, 800).Select(_ => Random.NextDouble() * 3950)]);
        }

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<IReadOnlyList<double>>>(result);
    }

    public SxExecuteRet<IReadOnlyList<DarkFieldPmtDelayDto>> GetCIBDelayList()
    {
        var result = new List<DarkFieldPmtDelayDto>();

        for (var i = 1; i < 16; i++)
        {
            for (var j = 1; j < 4; j++)
            {
                var pMtDelayDto = new DarkFieldPmtDelayDto
                {
                    PmtId = i,
                    ChannelId = j,
                    PmtDelay = Random.Next(240, 300)
                };
                result.Add(pMtDelayDto);
            }
        }

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldPmtDelayDto>>(result);
    }

    public SxExecuteRet<bool> SetCIBDelayList(IReadOnlyList<DarkFieldPmtDelayDto> darkFieldPmtDelayDtoList)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetCIBChirp(IReadOnlyList<double> gainList, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SendPMTGain(List<string> pmtData, List<string> igData, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double Ecs, double AfMotor)> RuntimeAfCalibration(
        CalChipSiteModelEnum calChipSiteModelEnum,
        int pmtId,
        double? coefficient = null,
        Point? point = null)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((Random.NextDouble(), Random.NextDouble()));
    }

    [Obsolete]
    public SxExecuteRet<int> GetDarkFieldLineScanImageYPixelHeight(OpticsMagTypeEnum opticsMagTypeEnum, bool isCuttingPixelHeight)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(1080);
    }

    public SxExecuteRet<int> GetDarkFieldLineScanImageYPixelHeight(ProductivityInformation productivityInformation, bool isCuttingPixelHeight)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(1080);
    }

    [Obsolete]
    public SxExecuteRet<List<DarkFieldImageDto>> GetDarkFieldLineScanImageList(Point position,
        int xWidthPixel,
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward)
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test.raw"));

        var result = new List<DarkFieldImageDto>(3);

        foreach (var i in Enumerable.Range(0, 3))
        {
            var (image, matrix) = calibrationAlgorithmService.ToImageInfo(bytes);
            result.Add(new DarkFieldImageDto { PmtId = pmtId, ChannelId = i + 1, Bytes = bytes, Image = image, Matrix = matrix });
        }

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<List<DarkFieldImageDto>> GetDarkFieldLineScanImageList(Point position,
        int xWidthPixel,
        ProductivityInformation productivityInformation,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward)
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test.raw"));

        var result = new List<DarkFieldImageDto>(3);

        foreach (var i in Enumerable.Range(0, 3))
        {
            var (image, matrix) = calibrationAlgorithmService.ToImageInfo(bytes);
            result.Add(new DarkFieldImageDto { PmtId = pmtId, ChannelId = i + 1, Bytes = bytes, Image = image, Matrix = matrix });
        }

        return SxExecuteRetHelper.CreateSuccess(result);
    }


    [Obsolete]
    public SxExecuteRet<List<DarkFieldRawScanImageDto>> GetDarkFieldLineScanImageList(
        Point startPosition,
        Point endPosition,
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward)
    {
        var uri = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test.raw");

        var result = new List<DarkFieldRawScanImageDto>(3);

        foreach (var i in Enumerable.Range(0, 3))
        {
            result.Add(new DarkFieldRawScanImageDto { PmtId = pmtId, ChannelId = i + 1, Url = uri });
        }

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<List<DarkFieldRawScanImageDto>> GetDarkFieldLineScanImageList(
        Point startPosition,
        Point endPosition,
        ProductivityInformation productivityInformation,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus,
        bool isForward)
    {
        var uri = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test.raw");

        var result = new List<DarkFieldRawScanImageDto>(3);

        foreach (var i in Enumerable.Range(0, 3))
        {
            result.Add(new DarkFieldRawScanImageDto { PmtId = pmtId, ChannelId = i + 1, Url = uri });
        }

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    [Obsolete]
    public SxExecuteRet<List<List<DarkFieldImageDto>>> GetChuckDarkFieldRowLineScanImageList(
        List<Point> machinePositionList,
        int xWidthPixel,
        double xPixelSize,
        OpticsMagTypeEnum opticsMagTypeEnum,
        StageSpeedEnum xStageSpeedEnum,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus)
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test.raw"));

        var result = new List<List<DarkFieldImageDto>>(machinePositionList.Count);

        foreach (var temp in machinePositionList.Select(_ => new List<DarkFieldImageDto>(3)))
        {
            foreach (var i in Enumerable.Range(0, 3))
            {
                var (image, matrix) = calibrationAlgorithmService.ToImageInfo(bytes);
                temp.Add(new DarkFieldImageDto { PmtId = pmtId, ChannelId = i + 1, Bytes = bytes, Image = image, Matrix = matrix });
            }

            result.Add(temp);
        }

        return SxExecuteRetHelper.CreateSuccess(result);
    }

    public SxExecuteRet<List<List<DarkFieldImageDto>>> GetChuckDarkFieldRowLineScanImageList(
        List<Point> machinePositionList,
        int xWidthPixel,
        double xPixelSize,
        ProductivityInformation productivityInformation,
        int pmtId,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        bool isAutoFocus)
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test.raw"));

        var result = new List<List<DarkFieldImageDto>>(machinePositionList.Count);

        foreach (var temp in machinePositionList.Select(_ => new List<DarkFieldImageDto>(3)))
        {
            foreach (var i in Enumerable.Range(0, 3))
            {
                var (image, matrix) = calibrationAlgorithmService.ToImageInfo(bytes);
                temp.Add(new DarkFieldImageDto { PmtId = pmtId, ChannelId = i + 1, Bytes = bytes, Image = image, Matrix = matrix });
            }

            result.Add(temp);
        }

        return SxExecuteRetHelper.CreateSuccess(result);
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