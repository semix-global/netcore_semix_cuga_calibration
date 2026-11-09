using algocv_sharp;
using AwesomeAssertions;

namespace CugaCalibrationUnitTest.AlgoCVSharp.StageMap;

/// <summary>
/// StageMapEngine 本仓库无生产调用, 只覆盖 IsDisposed.
/// </summary>
public class StageMapEngineDisposeTest
{
    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("IDisposableAnalyzers", "IDISP017:Prefer using", Justification = "Testing explicit Dispose behavior.")]
    public void Engine_IsDisposed_ShouldReflectDisposeState()
    {
        var engine = new StageMapEngine(new StageMapConfig());
        engine.IsDisposed.Should().BeFalse();
        engine.Dispose();
        engine.IsDisposed.Should().BeTrue();
        engine.Dispose();
        engine.IsDisposed.Should().BeTrue();
    }
}