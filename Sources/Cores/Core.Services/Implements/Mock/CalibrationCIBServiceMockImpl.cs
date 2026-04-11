using CommunityToolkit.Diagnostics;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using MiniExcelLibs;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using System.Collections.Concurrent;
using System.IO;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationCIBService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationCIBServiceMockImpl : ICalibrationCIBService
{
    private readonly string _mockImageFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test.raw");
    private readonly string _xzSyncMockImageFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test_xz_log.raw");
    private readonly string _cibMMDGainDTOFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\CIBMMDGainRelationshipDTO.xlsx");

    private readonly ConcurrentDictionary<CIBInformation, bool> _agcStatusStore = new();
    private readonly ConcurrentDictionary<CIBInformation, CIBProfileModeEnum> _profileModeStore = new();
    private readonly ConcurrentDictionary<CIBInformation, bool> _l0KStatusStore = new();

    public SxExecuteRet<bool> Connect()
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<IReadOnlyList<CIBInformation>> GetCIBInformations()
    {
        var cibInformations =
            (
                from pmtId in Enumerable.Range(1, 15)
                from channelId in Enumerable.Range(1, 3)
                select CIBInformation.Default.Clone().AdaptIn((pmtId, channelId, true))
            )
            .ToArray();

        Guard.IsTrue(cibInformations.DistinctBy(t => t).Count() == cibInformations.Length, "CIB Information is not unique");

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<CIBInformation>>([.. cibInformations.OrderBy(t => t)]);
    }

    public SxExecuteRet<IReadOnlyList<bool>> GetAGC(IReadOnlyList<CIBInformation> cibInformations)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<bool>>([.. cibInformations.Select(c => _agcStatusStore.GetOrAdd(c, false))]);
    }

    public SxExecuteRet<bool> SetAGC(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        Thread.Sleep(100);

        foreach (var cibInformation in cibInformations) _agcStatusStore[cibInformation] = enable;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<IReadOnlyList<CIBProfileModeEnum>> GetCIBProfileModeEnum(IReadOnlyList<CIBInformation> cibInformations)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<CIBProfileModeEnum>>([.. cibInformations.Select(c => _profileModeStore.GetOrAdd(c, CIBProfileModeEnum.PMTVoltage))]);
    }

    public SxExecuteRet<bool> SetCIBProfileModeEnum(IReadOnlyList<CIBInformation> cibInformations, CIBProfileModeEnum cibProfileModeEnum)
    {
        Thread.Sleep(100);

        foreach (var cibInformation in cibInformations) _profileModeStore[cibInformation] = cibProfileModeEnum;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<IReadOnlyList<bool>> GetL0K(IReadOnlyList<CIBInformation> cibInformations)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<bool>>([.. cibInformations.Select(c => _l0KStatusStore.GetOrAdd(c, false))]);
    }

    public SxExecuteRet<bool> SetL0K(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        Thread.Sleep(100);

        foreach (var cibInformation in cibInformations) _l0KStatusStore[cibInformation] = enable;

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetGain(IReadOnlyList<CIBInformation> cibInformations, double gain)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetMMD(CIBInformation cibInformation, IReadOnlyList<double> logGainMul128U12Bits, IReadOnlyList<double> gainS16Bits)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetLightMatching(IReadOnlyList<CIBInformation> cibInformations, double digitalGainPlusMultiplicativeFactors)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetIlluminationProfile(IReadOnlyList<CIBInformation> cibInformations, IReadOnlyList<double> illuminationProfiles)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<IReadOnlyList<CIBDelayDTO>> GetDelays(IReadOnlyList<CIBInformation> cibInformations)
    {
        var results = new CIBDelayDTO[cibInformations.Count];

        for (var i = 0; i < results.Length; i++)
        {
            var cibInformation = cibInformations[i];

            results[i] = new CIBDelayDTO { CIBInformation = cibInformation, PMTDelay = Random.Shared.Next(240, 300), SenseDelay = Random.Shared.Next(240, 300), AGCDelay = Random.Shared.Next(240, 300) };
        }

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<CIBDelayDTO>>(results);
    }

    public SxExecuteRet<bool> SetDelays(IReadOnlyList<CIBDelayDTO> delays)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetXPixelSize(ProductivityInformation productivityInformation, double xPixelSize)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<IReadOnlyList<IReadOnlyList<CIBMMDGainRelationshipDTO>>> GetCIBMMDGains(IReadOnlyList<CIBInformation> cibInformations, double startGain, double stepGain, double stopGain)
    {
        Thread.Sleep(100);

        var values = MiniExcel.Query<CIBMMDGainRelationshipDTO>(_cibMMDGainDTOFilePath).ToArray();
        var results = new CIBMMDGainRelationshipDTO[cibInformations.Count][];

        for (var i = 0; i < results.Length; i++)
        {
            var cibInformation = cibInformations[i];

            results[i] = [.. values.Select(t => t.Clone().WithCIBInformation(cibInformation))];
        }

        return SxExecuteRetHelper.CreateSuccess<IReadOnlyList<IReadOnlyList<CIBMMDGainRelationshipDTO>>>(results);
    }

    public SxExecuteRet<bool> ToggleRTFCParam(ProductivityInformation productivityInformation)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(
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
        var bytes = File.ReadAllBytes(_mockImageFilePath);

        var results = new DarkFieldImageDTO[cibInformations.Count];

        for (var i = 0; i < results.Length; i++)
        {
            var cibInformation = cibInformations[i];
            var (size, _, _) = RAWImageFactory.GetSize(bytes);
            
            results[i] = new DarkFieldImageDTO().AdaptIn(new DarkFieldRawScanImageDTO { CIBInformation = cibInformation, Size = size, IsForward = isForward, RawImageCIBProfileModeEnum = CIBProfileModeEnum.PMTVoltage, RawImageFilePath = _mockImageFilePath, IsKeepRawImageCIBProfileModeEnum = isKeepRawImageCIBProfileModeEnum });
        }

        return Task.FromResult(SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldImageDTO>>(results));
    }

    public Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(
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
        var bytes = File.ReadAllBytes(_mockImageFilePath);

        var results = new DarkFieldImageDTO[centerPositions.Count];

        for (var i = 0; i < results.Length; i++)
        {
            var (size, _, _) = RAWImageFactory.GetSize(bytes);

            results[i] = new DarkFieldImageDTO().AdaptIn(new DarkFieldRawScanImageDTO { CIBInformation = cibInformation, Size = size, IsForward = true, RawImageCIBProfileModeEnum = CIBProfileModeEnum.PMTVoltage, RawImageFilePath = _mockImageFilePath, IsKeepRawImageCIBProfileModeEnum = isKeepRawImageCIBProfileModeEnum });
        }

        return Task.FromResult(SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldImageDTO>>(results));
    }

    public Task<SxExecuteRet<IReadOnlyList<DarkFieldRawScanImageDTO>>> GetPMTImagesAsync(
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
        using var fileSteam = File.OpenRead(_mockImageFilePath);
        using var binaryReader = new BinaryReader(fileSteam);

        var results = new DarkFieldRawScanImageDTO[cibInformations.Count];

        for (var i = 0; i < results.Length; i++)
        {
            var cibInformation = cibInformations[i];
            var (size, _, _) = RAWImageFactory.GetSize(binaryReader);

            results[i] = new DarkFieldRawScanImageDTO { CIBInformation = cibInformation, Size = size, IsForward = isForward, RawImageCIBProfileModeEnum = CIBProfileModeEnum.PMTVoltage, RawImageFilePath = _mockImageFilePath, IsKeepRawImageCIBProfileModeEnum = isKeepRawImageCIBProfileModeEnum };
        }

        return Task.FromResult(SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldRawScanImageDTO>>(results));
    }

    public Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(
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
        var bytes = File.ReadAllBytes(_xzSyncMockImageFilePath);

        var results = new DarkFieldImageDTO[cibInformations.Count];

        for (var i = 0; i < results.Length; i++)
        {
            var cibInformation = cibInformations[i];
            var (size, _, _) = RAWImageFactory.GetSize(bytes);
            
            results[i] = new DarkFieldImageDTO().AdaptIn(new DarkFieldRawScanImageDTO { CIBInformation = cibInformation, Size = size, IsForward = isForward, RawImageCIBProfileModeEnum = CIBProfileModeEnum.PMTLog, RawImageFilePath = _xzSyncMockImageFilePath, IsKeepRawImageCIBProfileModeEnum = isKeepRawImageCIBProfileModeEnum });
        }

        return Task.FromResult(SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldImageDTO>>(results));
    }

    public SxExecuteRet<(double ECS, double Motor, bool isAFServo)> RuntimeAFCalibration(
        CalChipSiteModelEnum calChipSiteModelEnum,
        ProductivityInformation productivityInformation,
        CIBInformation cibInformation,
        Point? centerMachinePosition = null,
        LaserLightInformation? laserLightInformation = null)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess((Random.Shared.NextDouble(), Random.Shared.NextDouble(), true));
    }
}