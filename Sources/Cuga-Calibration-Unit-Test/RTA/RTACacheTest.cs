using AwesomeAssertions;
using CugaCalibration.ViewModels.Common.Windows.Tools.Stage;
using Net.Utilities.Models.Geometries;

namespace CugaCalibrationUnitTest.RTA;

public sealed class RTACacheTest
{
    [Fact]
    public void WaferMapIsInvalidatedAfterGeometryChanges()
    {
        var cache = CreateCache();
        cache.MarkWaferMapCurrent();
        cache.IsWaferMapCurrent.Should().BeTrue();

        cache.DiePitchWidth = 20d;
        cache.IsWaferMapCurrent.Should().BeFalse();
    }

    [Fact]
    public void WaferMapRejectsInPlaceAlignmentChanges()
    {
        var cache = CreateCache();
        cache.MarkWaferMapCurrent();

        cache.AlignmentResult.MarkPoint1 = new Point(10d, 20d);

        cache.IsWaferMapCurrent.Should().BeFalse();
    }

    [Fact]
    public void WaferMapTracksCurrentMarkerAndUnsubscribesReplacedMarker()
    {
        var cache = CreateCache();
        var oldMarker = cache.StageMapTemplates[0];
        cache.StageMapTemplates = [new StageMapTemplate { FindBFMachinePosition = new Point(30d, 40d) }];
        cache.MarkWaferMapCurrent();

        oldMarker.FindBFMachinePosition = Point.Origin;
        cache.IsWaferMapCurrent.Should().BeTrue();

        cache.StageMapTemplates[0].FindBFMachinePosition = Point.Origin;
        cache.IsWaferMapCurrent.Should().BeFalse();
    }

    [Fact]
    public void WaferMapRequiresOneMarkerAndIsNotRestoredFromCachedResults()
    {
        var cache = CreateCache();
        cache.IsWaferMapCurrent.Should().BeFalse();
        cache.MarkWaferMapCurrent();

        cache.StageMapTemplates = [];

        cache.IsWaferMapCurrent.Should().BeFalse();
    }

    private static RTACache CreateCache() => new()
    {
        WaferRadius = 100d,
        DiePitchWidth = 10d,
        DiePitchHeight = 10d,
        StageMapTemplates = [new StageMapTemplate { FindBFMachinePosition = new Point(10d, 20d) }]
    };
}