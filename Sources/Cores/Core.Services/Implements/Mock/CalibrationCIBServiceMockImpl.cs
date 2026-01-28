using CommunityToolkit.Diagnostics;
using Core.Models.Enums.CIB;
using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Net.Utilities.Algorithms.Halcon;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using System.IO;

namespace Core.Services.Implements.Mock;

[IOCAppService(ServiceType = typeof(ICalibrationCIBService), IOCLifetimeEnum = IOCLifeTimeEnum.Singleton, IOCEnvironmentEnum = IOCEnvironmentEnum.Development)]
public sealed class CalibrationCIBServiceMockImpl(
    ICalibrationAlgorithmService calibrationAlgorithmService) : ICalibrationCIBService
{
    private readonly string _mockImageFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\Data\test.raw");

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

    public SxExecuteRet<bool> ToggleEnableAGC(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleProfileMode(IReadOnlyList<CIBInformation> cibInformations, CIBProfileModeEnum cibProfileModeEnum)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableL0K(IReadOnlyList<CIBInformation> cibInformations, bool enable)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetGain(IReadOnlyList<CIBInformation> cibInformations, double gain)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> ToggleEnableMarkMode(IReadOnlyList<CIBInformation> cibInformations, bool enable)
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

    public Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point position,
        IReadOnlyList<CIBInformation> cibInformations,
        int imageWidth,
        bool isForward,
        bool isAutoFocus,
        CancellationToken cancellationToken)
    {
        var bytes = File.ReadAllBytes(_mockImageFilePath);

        var results = new DarkFieldImageDTO[cibInformations.Count];

        for (var i = 0; i < results.Length; i++)
        {
            var cibInformation = cibInformations[i];

            var image = RawImageFactory.CreateImage(bytes);
            var size = (SizeI)image.GetSize();
            results[i] = new DarkFieldImageDTO { PMTId = cibInformation.PMTId, ChannelId = cibInformation.ChannelId, Width = size.Width, Height = size.Height, RawImageFilePath = _mockImageFilePath, Image = image };
        }

        return Task.FromResult(SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldImageDTO>>(results));
    }

    public Task<SxExecuteRet<IReadOnlyList<DarkFieldRawScanImageDTO>>> GetPMTImagesAsync(
        ProductivityInformation productivityInformation,
        StageCoordinateSystemEnum stageCoordinateSystemEnum,
        Point startPosition,
        Point endPosition,
        IReadOnlyList<CIBInformation> cibInformations,
        bool isForward,
        bool isAutoFocus,
        CancellationToken cancellationToken)
    {
        using var fileSteam = File.OpenRead(_mockImageFilePath);
        using var binaryReader = new BinaryReader(fileSteam);

        var results = new DarkFieldRawScanImageDTO[cibInformations.Count];

        for (var i = 0; i < results.Length; i++)
        {
            var cibInformation = cibInformations[i];


            var (size, _, _) = RawImageFactory.GetSize(binaryReader);
            var sizeI = (SizeI)size;

            results[i] = new DarkFieldRawScanImageDTO { PMTId = cibInformation.PMTId, ChannelId = cibInformation.ChannelId, Width = sizeI.Width, Height = sizeI.Height, RawImageFilePath = _mockImageFilePath };
        }

        return Task.FromResult(SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldRawScanImageDTO>>(results));
    }

    public Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDTO>>> GetPMTImagesAsync(
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
        var bytes = File.ReadAllBytes(_mockImageFilePath);

        var results = new DarkFieldImageDTO[cibInformations.Count];

        for (var i = 0; i < results.Length; i++)
        {
            var cibInformation = cibInformations[i];

            var image = RawImageFactory.CreateImage(bytes);
            var size = (SizeI)image.GetSize();
            results[i] = new DarkFieldImageDTO { PMTId = cibInformation.PMTId, ChannelId = cibInformation.ChannelId, Width = size.Width, Height = size.Height, RawImageFilePath = _mockImageFilePath, Image = image };
        }

        return Task.FromResult(SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldImageDTO>>(results));
    }
}