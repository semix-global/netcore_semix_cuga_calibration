using algocv_sharp;
using AwesomeAssertions;
using Core.Services.Implements;
using RTAScanSample = Core.Services.Interfaces.ICalibrationAlgorithmService.RTAScanSample;

namespace CugaCalibrationUnitTest.RTA;

public sealed class RTAScanSamplesTest
{
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(7)]
    public void BuildSamples_AveragesEveryReturnedColumn(int columnCount)
    {
        using var hits = new Image(columnCount, 2, 2, ImageDataType.Double);
        using var scores = new Image(columnCount, 2, 1, ImageDataType.Double);
        for (var row = 0; row < 2; row++)
        {
            for (var column = 0; column < columnCount; column++)
            {
                hits.SetValue(column, row, 100d * row + column * 2d, 0);
                hits.SetValue(column, row, column * 10d, 1);
                scores.SetValue(column, row, 0.9);
            }
        }

        var array = new StageMapArray { hit = hits, score = scores, rows = 2, cols = columnCount, i0 = 12 };
        var samples = CalibrationAlgorithmServiceImpl.BuildRTAScanSamples(array, 1000, true);

        samples.Should().Equal(
            new RTAScanSample(12, columnCount - 1d, (columnCount - 1) * 5d, columnCount),
            new RTAScanSample(13, 100d + columnCount - 1d, (columnCount - 1) * 5d, columnCount));
    }

    [Fact]
    public void BuildSamples_SkipsMissingCellsAndPreservesMissingGridRows()
    {
        using var hits = new Image(3, 3, 2, ImageDataType.Double);
        using var scores = new Image(3, 3, 1, ImageDataType.Double);
        for (var row = 0; row < 3; row++)
        {
            for (var column = 0; column < 3; column++)
            {
                hits.SetValue(column, row, 10d + row * 100d, 0);
                hits.SetValue(column, row, 20d + column * 10d, 1);
                scores.SetValue(column, row, 0.9);
            }
        }

        hits.SetValue(1, 0, double.NaN, 0);
        hits.SetValue(2, 0, double.PositiveInfinity, 1);
        for (var column = 0; column < 3; column++) scores.SetValue(column, 1, double.NaN);
        scores.SetValue(0, 2, double.NegativeInfinity);

        var array = new StageMapArray { hit = hits, score = scores, rows = 3, cols = 3, i0 = 7 };
        var samples = CalibrationAlgorithmServiceImpl.BuildRTAScanSamples(array, 1000, true);

        samples.Should().Equal(new RTAScanSample(7, 10d, 20d, 1), new RTAScanSample(9, 210d, 35d, 2));
    }

    [Fact]
    public void BuildSamples_ReverseMapsXWithoutChangingYOrGridIdentity()
    {
        using var hits = new Image(1, 2, 2, ImageDataType.Double);
        using var scores = new Image(1, 2, 1, ImageDataType.Double);
        hits.SetValue(0, 0, 10d, 0);
        hits.SetValue(0, 0, 30d, 1);
        hits.SetValue(0, 1, 20d, 0);
        hits.SetValue(0, 1, 40d, 1);

        var array = new StageMapArray { hit = hits, score = scores, rows = 2, cols = 1, i0 = 100 };
        var samples = CalibrationAlgorithmServiceImpl.BuildRTAScanSamples(array, 100, false);

        samples.Should().Equal(new RTAScanSample(101, 79d, 40d, 1), new RTAScanSample(100, 89d, 30d, 1));
    }

    [Fact]
    public void BuildSamples_KeepsFiniteOutliersWithoutStatisticalFiltering()
    {
        using var hits = new Image(5, 1, 2, ImageDataType.Double);
        using var scores = new Image(5, 1, 1, ImageDataType.Double);
        for (var column = 0; column < 5; column++)
        {
            hits.SetValue(column, 0, 50d, 0);
            hits.SetValue(column, 0, column == 4 ? 10000d : 0d, 1);
        }

        var array = new StageMapArray { hit = hits, score = scores, rows = 1, cols = 5 };
        var samples = CalibrationAlgorithmServiceImpl.BuildRTAScanSamples(array, 100, true);

        samples.Should().Equal(new RTAScanSample(0, 50d, 2000d, 5));
    }

    [Fact]
    public void BuildSamples_EmptyArrayReturnsNoSamples()
    {
        CalibrationAlgorithmServiceImpl.BuildRTAScanSamples(new StageMapArray(), 100, true).Should().BeEmpty();
    }
}