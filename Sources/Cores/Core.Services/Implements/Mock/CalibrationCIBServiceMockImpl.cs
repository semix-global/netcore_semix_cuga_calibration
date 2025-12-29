using Core.Models.Enums.Stage;
using Core.Models.Helper;
using Core.Models.Models.Common.DarkField;
using Core.Models.Models.Common.Pattern;
using Core.Services.Interfaces;
using Net.Utilities.Algorithms.Halcon.Extensions;
using Net.Utilities.Attributes;
using Net.Utilities.Enums;
using Net.Utilities.Models.Geometries;
using Semix.CoreLib;
using System.IO;
using CommunityToolkit.Diagnostics;
using Core.Models.Enums.CIB;
using Net.Utilities.Algorithms.Halcon;

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

    public SxExecuteRet<bool> ToggleEnableAutoGainControl(IReadOnlyList<CIBInformation> cibInformations, bool enable)
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

    public SxExecuteRet<bool> SetMMD(CIBInformation cibInformation, IReadOnlyList<double> logGainMul128U12Bits, IReadOnlyList<double> gainS16Bits, double maxLogGain)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public SxExecuteRet<bool> SetLightMatching(IReadOnlyList<CIBInformation> cibInformations, double digitalGainPlusMultiplicativeFactors)
    {
        Thread.Sleep(100);

        return SxExecuteRetHelper.CreateSuccess(true);
    }

    public Task<SxExecuteRet<IReadOnlyList<DarkFieldImageDto>>> GetPMTImagesAsync(
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

        var result = new DarkFieldImageDto[cibInformations.Count];

        for (var i = 0; i < result.Length; i++)
        {
            var cibInformation = cibInformations[i];

            var (image, matrix) = calibrationAlgorithmService.ToImageInfo(bytes);
            var size = (SizeI)image.GetSize();
            result[i] = new DarkFieldImageDto { PmtId = cibInformation.PMTId, ChannelId = cibInformation.ChannelId, Width = size.Width, Height = size.Height, RawImageFilePath = _mockImageFilePath, Image = image, Matrix = matrix };
        }

        return Task.FromResult(SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldImageDto>>(result));
    }

    public Task<SxExecuteRet<IReadOnlyList<DarkFieldRawScanImageDto>>> GetPMTImagesAsync(
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

        var result = new DarkFieldRawScanImageDto[cibInformations.Count];

        for (var i = 0; i < result.Length; i++)
        {
            var cibInformation = cibInformations[i];


            var (size, _, _) = RawImageFactory.GetSize(binaryReader);
            var sizeI = (SizeI)size;

            result[i] = new DarkFieldRawScanImageDto { PmtId = cibInformation.PMTId, ChannelId = cibInformation.ChannelId, Width = sizeI.Width, Height = sizeI.Height, RawImageFilePath = _mockImageFilePath };
        }

        return Task.FromResult(SxExecuteRetHelper.CreateSuccess<IReadOnlyList<DarkFieldRawScanImageDto>>(result));
    }
}