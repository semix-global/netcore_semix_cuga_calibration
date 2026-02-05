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
using Cuga.Data.DataStruct.DTO.Swath;
using Core.Models.Extensions;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;

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
    CalibrationSetting calibrationSetting)
    : ICalibrationLaserService
{
    private static readonly Random Random = new();

    private readonly CalibrationLaserServiceImpl _calibrationLaserServiceImpl = new(calibrationAlgorithmService, calibrationStageService, calibrationConfigService, calibrationSetting);
    private readonly string _mockImageFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test.raw");

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

    public SxExecuteRet<double> GetOpticalMeasurePower()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(Convert.ToDouble(Random.Next(30, 60) * _coefficient));
    }

    public SxExecuteRet<double> GetOpticalMeasurePower(ProductivityInformation productivityInformation, double flatnessTime)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(Convert.ToDouble(Random.Next(30, 60) * _coefficient));
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
        Thread.Sleep(100);

        var oiProductivityInformations = new[]
        {
            ProductivityInformation.Default.Clone().AdaptIn(
                new C2MProductivityInfo
                {
                    Name = "S5",
#if NETFRAMEWORK
                    NIOI = SxNIOIEnum.OI,
#endif
                    Mag = SxMAGEnum.Low,
                    Speed = SxSpeedEnum.High,
                    IsUsed = true
                },
                new CgSwathSpeedInfo
                {
                    YPixelSize = 0.327,
                    YPixel = 508
                },
                508,
                408,
                445000
#if NET
                , OpticsIlluminationModeEnum.OI
#endif
            ),
            ProductivityInformation.Default.Clone().AdaptIn(
                new C2MProductivityInfo
                {
                    Name = "S10",
#if NETFRAMEWORK
                    NIOI = SxNIOIEnum.OI,
#endif
                    Mag = SxMAGEnum.Low,
                    Speed = SxSpeedEnum.Low,
                    IsUsed = true
                },
                new CgSwathSpeedInfo
                {
                    YPixelSize = 0.327,
                    YPixel = 508
                },
                508,
                408,
                222500
#if NET
                , OpticsIlluminationModeEnum.OI
#endif
            ),
            ProductivityInformation.Default.Clone().AdaptIn(
                new C2MProductivityInfo
                {
                    Name = "S25",
#if NETFRAMEWORK
                    NIOI = SxNIOIEnum.OI,
#endif
                    Mag = SxMAGEnum.Mid,
                    Speed = SxSpeedEnum.High,
                    IsUsed = true
                },
                new CgSwathSpeedInfo
                {
                    YPixelSize = 0.1635,
                    YPixel = 1008
                },
                1008,
                290,
                175900
#if NET
                , OpticsIlluminationModeEnum.OI
#endif
            ),
            ProductivityInformation.Default.Clone().AdaptIn(
                new C2MProductivityInfo
                {
                    Name = "S40",
#if NETFRAMEWORK
                    NIOI = SxNIOIEnum.OI,
#endif
                    Mag = SxMAGEnum.Mid,
                    Speed = SxSpeedEnum.Low,
                    IsUsed = true
                },
                new CgSwathSpeedInfo
                {
                    YPixelSize = 0.1635,
                    YPixel = 1008
                },
                1008,
                290,
                88060
#if NET
                , OpticsIlluminationModeEnum.OI
#endif
            ),
            ProductivityInformation.Default.Clone().AdaptIn(
                new C2MProductivityInfo
                {
                    Name = "S55",
#if NETFRAMEWORK
                    NIOI = SxNIOIEnum.OI,
#endif
                    Mag = SxMAGEnum.High,
                    Speed = SxSpeedEnum.High,
                    IsUsed = true
                },
                new CgSwathSpeedInfo
                {
                    YPixelSize = 0.11286,
                    YPixel = 1500
                },
                1500,
                210,
                87240
#if NET
                , OpticsIlluminationModeEnum.OI
#endif
            ),
            ProductivityInformation.Default.Clone().AdaptIn(
                new C2MProductivityInfo
                {
                    Name = "S90",
#if NETFRAMEWORK
                    NIOI = SxNIOIEnum.OI,
#endif
                    Mag = SxMAGEnum.High,
                    Speed = SxSpeedEnum.Low,
                    IsUsed = true
                },
                new CgSwathSpeedInfo
                {
                    YPixelSize = 0.11286,
                    YPixel = 1500
                },
                1500,
                210,
                43600
#if NET
                , OpticsIlluminationModeEnum.OI
#endif
            )
        };

        var niProductivityInformations = new[]
        {
            ProductivityInformation.Default.Clone().AdaptIn(
                new C2MProductivityInfo
                {
                    Name = "S40",
#if NETFRAMEWORK
                    NIOI = SxNIOIEnum.NI,
#endif
                    Mag = SxMAGEnum.Mid,
                    Speed = SxSpeedEnum.Low,
                    IsUsed = true
                },
                new CgSwathSpeedInfo
                {
                    YPixelSize = 0.144,
                    YPixel = 1160
                },
                1160,
                200,
                40000
#if NET
                , OpticsIlluminationModeEnum.NI
#endif
            ),
            ProductivityInformation.Default.Clone().AdaptIn(
                new C2MProductivityInfo
                {
                    Name = "S90",
#if NETFRAMEWORK
                    NIOI = SxNIOIEnum.NI,
#endif
                    Mag = SxMAGEnum.High,
                    Speed = SxSpeedEnum.Low,
                    IsUsed = true
                },
                new CgSwathSpeedInfo
                {
                    YPixelSize = 0.096,
                    YPixel = 1720
                },
                1720,
                200,
                26880
#if NET
                , OpticsIlluminationModeEnum.NI
#endif
            )
        };

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<ProductivityInformation>>(opticsIlluminationModeEnum == OpticsIlluminationModeEnum.OI
            ? [.. oiProductivityInformations.OrderBy(t => t)]
            : [.. niProductivityInformations.OrderBy(t => t)]);
    }

    [Obsolete]
    public SxExecuteRet<bool> ToggleOpticsMagType(OpticsIlluminationModeEnum opticsIlluminationModeEnum, OpticsMagTypeEnum opticsMagTypeEnum)
    {
        Thread.Sleep(100);

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

    public SxExecuteRet<bool> ToggleOpticsPolarizationMode(OpticsPolarizationModeEnum opticsPolarizationModeEnum)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    [Obsolete]
    public SxExecuteRet<bool> SetAODDelayValue(OpticsIlluminationModeEnum opticsIlluminationModeEnum, OpticsMagTypeEnum opticsMagTypeEnum, double prescanAODDelay, double chirpAODDelay)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetAODDelayValue(ProductivityInformation productivityInformation, double prescanAODDelay, double chirpAODDelay)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
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

    public SxExecuteRet<bool> SetCIBChirp(IReadOnlyList<double> gainList, int pmtId, int channelId)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<(double Ecs, double Motor, bool isAFServo)> RuntimeAfCalibration(
        CalChipSiteModelEnum calChipSiteModelEnum,
        ProductivityInformation productivityInformation,
        int pmtId,
        double? coefficient = null,
        Point? point = null)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((Random.NextDouble(), Random.NextDouble(), true));
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
        var bytes = File.ReadAllBytes(_mockImageFilePath);

        var result = new List<DarkFieldImageDTO>(3);

        foreach (var i in Enumerable.Range(0, 3))
        {
            var image = RawImageFactory.CreateImage(bytes);
            var size = (SizeI)image.GetSize();
            result.Add(new DarkFieldImageDTO { PMTId = pmtId, ChannelId = i + 1, Width = size.Width, Height = size.Height, RawImageFilePath = _mockImageFilePath, Image = image });
        }

        return SxExecuteRetHelper.CreateSuccess(result);
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
        var bytes = File.ReadAllBytes(_mockImageFilePath);

        var result = new List<DarkFieldImageDTO>(3);

        foreach (var i in Enumerable.Range(0, 3))
        {
            var image = RawImageFactory.CreateImage(bytes);
            var size = (SizeI)image.GetSize();
            result.Add(new DarkFieldImageDTO { PMTId = pmtId, ChannelId = i + 1, Width = size.Width, Height = size.Height, RawImageFilePath = _mockImageFilePath, Image = image });
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
        var uri = _mockImageFilePath;

        var result = new List<DarkFieldRawScanImageDTO>(3);

        foreach (var i in Enumerable.Range(0, 3))
        {
            using var fileSteam = File.OpenRead(uri);
            using var binaryReader = new BinaryReader(fileSteam);
            var (size, _, _) = RawImageFactory.GetSize(binaryReader);
            var sizeI = (SizeI)size;
            result.Add(new DarkFieldRawScanImageDTO { PMTId = pmtId, ChannelId = i + 1, Width = sizeI.Width, Height = sizeI.Height, RawImageFilePath = uri });
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
        var uri = _mockImageFilePath;

        var result = new List<DarkFieldRawScanImageDTO>(3);

        var pmtList = pmtId != -1 ? [pmtId] : Enumerable.Range(1, 15).ToList();
        foreach (var id in pmtList)
        {
            foreach (var i in Enumerable.Range(0, 3))
            {
                using var fileSteam = File.OpenRead(uri);
                using var binaryReader = new BinaryReader(fileSteam);
                var (size, _, _) = RawImageFactory.GetSize(binaryReader);
                var sizeI = (SizeI)size;

                result.Add(new DarkFieldRawScanImageDTO { PMTId = id, ChannelId = i + 1, Width = sizeI.Width, Height = sizeI.Height, RawImageFilePath = uri });
            }
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
        var bytes = File.ReadAllBytes(_mockImageFilePath);

        var result = new List<List<DarkFieldImageDTO>>(machinePositionList.Count);

        foreach (var temp in machinePositionList.Select(_ => new List<DarkFieldImageDTO>(3)))
        {
            foreach (var i in Enumerable.Range(0, 3))
            {
                var image = RawImageFactory.CreateImage(bytes);
                var size = (SizeI)image.GetSize();
                temp.Add(new DarkFieldImageDTO { PMTId = pmtId, ChannelId = i + 1, Width = size.Width, Height = size.Height, RawImageFilePath = _mockImageFilePath, Image = image });
            }

            result.Add(temp);
        }

        return SxExecuteRetHelper.CreateSuccess(result);
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
        var bytes = File.ReadAllBytes(_mockImageFilePath);

        var result = new List<List<DarkFieldImageDTO>>(machinePositionList.Count);

        foreach (var temp in machinePositionList.Select(_ => new List<DarkFieldImageDTO>(3)))
        {
            foreach (var i in Enumerable.Range(0, 3))
            {
                var image = RawImageFactory.CreateImage(bytes);
                var size = (SizeI)image.GetSize();
                temp.Add(new DarkFieldImageDTO { PMTId = pmtId, ChannelId = i + 1, Width = size.Width, Height = size.Height, RawImageFilePath = _mockImageFilePath, Image = image });
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