using AwesomeAssertions;
using Core.Models.Enums.Stage;
using Core.Models.Models.Common.Pattern;
using Core.Services.Implements.Mock;
using Net.Utilities.Models.Geometries;
using System.IO;

namespace CugaCalibrationUnitTest.RTA;

public sealed class CIBLongScanMockTest
{
    [Theory]
    [InlineData(StageCoordinateSystemEnum.Machine, true, false)]
    [InlineData(StageCoordinateSystemEnum.Machine, false, true)]
    [InlineData(StageCoordinateSystemEnum.Bright, true, true)]
    [InlineData(StageCoordinateSystemEnum.Bright, false, false)]
    [InlineData(StageCoordinateSystemEnum.Dark, true, true)]
    [InlineData(StageCoordinateSystemEnum.Dark, false, false)]
    public async Task LongScan_PreservesImageDirectionContractOnNegativeXMachine(StageCoordinateSystemEnum coordinateSystem, bool isForward, bool isExpectedForward)
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Assets\20260508_214_0_0_1_short_011375_PMT08-CH2_8.raw");
        var service = new CalibrationCIBServiceMockImpl(new CalibrationStageServiceMockImpl())
        {
            SimulatorImageFilePaths = [filePath]
        };

        var result = await service.GetPMTImagesAsync(
            ProductivityInformation.Default,
            coordinateSystem,
            Point.Origin,
            new Point(100d, 0d),
            [CIBInformation.Default],
            isForward,
            true,
            true,
            false,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var rawImage = result.Anything.Should().ContainSingle().Which;
        rawImage.CIBInformation.Should().Be(CIBInformation.Default);
        rawImage.Size.Width.Should().BeGreaterThan(0);
        rawImage.Size.Height.Should().BeGreaterThan(0);
        rawImage.IsForward.Should().Be(isExpectedForward);
        rawImage.RawImageFilePath.Should().Be(filePath);
        rawImage.IsKeepRawImageCIBProfileModeEnum.Should().BeTrue();
    }

    [Fact]
    public async Task LongScan_CancellationStopsBeforeReadingImage()
    {
        var service = new CalibrationCIBServiceMockImpl(new CalibrationStageServiceMockImpl());
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetPMTImagesAsync(
            ProductivityInformation.Default,
            StageCoordinateSystemEnum.Machine,
            Point.Origin,
            new Point(100d, 0d),
            [CIBInformation.Default],
            true,
            true,
            true,
            false,
            cancellationTokenSource.Token));
    }
}