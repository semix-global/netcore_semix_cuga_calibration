using AwesomeAssertions;
using CugaCalibration.ViewModels.Common.Windows.Tools.Stage;
using Net.Utilities.Models.Geometries;
using Newtonsoft.Json;
using RTAScanSample = Core.Services.Interfaces.ICalibrationAlgorithmService.RTAScanSample;

namespace CugaCalibrationUnitTest.RTA;

public sealed class RTAScanResultTest
{
    [Fact]
    public void PositionPoints_ConvertKHzToMilliseconds()
    {
        var scan = new RTARowScan
        {
            SampleRate = 50d,
            Samples = [new RTAScanSample(1, 100d, 7d, 5), new RTAScanSample(2, 200d, 9d, 4)]
        };

        scan.GetPositionPoints(true).Should().Equal(new Point(2d, 100d), new Point(4d, 200d));
        scan.GetPositionPoints(false).Should().Equal(new Point(2d, 7d), new Point(4d, 9d));
    }

    [Fact]
    public void FFT_UsesGridPixelIntervalAndKeepsRawMagnitudeAndDC()
    {
        var scan = new RTARowScan
        {
            SampleRate = 50d,
            Samples = [.. Enumerable.Range(0, 8).Select(index => new RTAScanSample(
                index, 100d + index * 10d, 5d + Math.Sin(2d * Math.PI * 2d * index / 8d), 5))]
        };

        var points = scan.GetFrequencyPoints(false);

        points.Should().HaveCount(5);
        points[0].X.Should().Be(0d);
        points[0].Y.Should().BeApproximately(40d, 1e-8);
        points[2].X.Should().BeApproximately(1250d, 1e-8);
        points[2].Y.Should().BeApproximately(4d, 1e-8);
    }

    [Fact]
    public void FFT_UsesLongestConsecutiveSegmentWithoutFillingGaps()
    {
        int[] rows = [0, 1, 2, 4, 5, 6, 7, 8];
        var scan = new RTARowScan
        {
            SampleRate = 50d,
            Samples = [.. rows.Select(row => new RTAScanSample(row, row * 10d, 5d, 3))]
        };

        scan.GetFFTInput().Select(t => t.GridRowIndex).Should().Equal(4, 5, 6, 7, 8);
        scan.GetPositionPoints(false).Should().HaveCount(8);
        scan.GetFrequencyPoints(false)[0].Y.Should().BeApproximately(25d, 1e-8);
    }

    [Fact]
    public void FFT_ReverseGridNumbersRemainConsecutiveAfterPhysicalSorting()
    {
        var scan = new RTARowScan
        {
            IsForward = false,
            SampleRate = 50d,
            Samples = [.. Enumerable.Range(0, 4).Select(index => new RTAScanSample(10 - index, index * 10d, 3d, 2)).Reverse()]
        };

        scan.GetFFTInput().Select(t => t.GridRowIndex).Should().Equal(10, 9, 8, 7);
        scan.GetFrequencyPoints(false)[0].Y.Should().BeApproximately(12d, 1e-8);
    }

    [Fact]
    public void FFT_FewerThanFourConsecutiveSamplesKeepsPositionPlotOnly()
    {
        var scan = new RTARowScan
        {
            SampleRate = 50d,
            Samples = [.. new[] { 0, 1, 2, 4, 5, 6 }.Select(row => new RTAScanSample(row, row * 10d, 5d, 3))]
        };

        scan.GetPositionPoints(false).Should().HaveCount(6);
        scan.GetFrequencyPoints(false).Should().BeEmpty();
        scan.FFTStatus.Should().StartWith("Unavailable");
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidSampleRateDoesNotCreateTimeOrFrequencyPoints(double sampleRate)
    {
        var scan = new RTARowScan
        {
            SampleRate = sampleRate,
            Samples = [.. Enumerable.Range(0, 4).Select(row => new RTAScanSample(row, row * 10d, 5d, 3))]
        };

        scan.GetPositionPoints(false).Should().BeEmpty();
        scan.GetFrequencyPoints(false).Should().BeEmpty();
        scan.FFTStatus.Should().StartWith("Unavailable");
    }

    [Fact]
    public void CacheRoundTripPreservesNumericalSampleRateAndReadonlySamples()
    {
        var scan = new RTARowScan
        {
            Row = 8,
            IsForward = false,
            StartPosition = new Point(-100d, 20d),
            StopPosition = new Point(100d, 20d),
            SampleRate = 50d,
            Samples = [new RTAScanSample(7, 100d, 9d, 4)]
        };

        var restored = JsonConvert.DeserializeObject<RTARowScan>(JsonConvert.SerializeObject(scan));

        restored.Should().NotBeNull();

        if (restored is null) return;

        restored.Row.Should().Be(scan.Row);
        restored.IsForward.Should().Be(scan.IsForward);
        restored.StartPosition.Should().Be(scan.StartPosition);
        restored.StopPosition.Should().Be(scan.StopPosition);
        restored.SampleRate.Should().Be(scan.SampleRate);
        restored.Samples.Should().Equal(scan.Samples);
        restored.GetPositionPoints(false).Should().Equal(scan.GetPositionPoints(false));
    }
}