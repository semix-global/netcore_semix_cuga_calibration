using algocv_sharp;
using CommunityToolkit.Diagnostics;
using Core.Algorithm.Interfaces.TemplateMatch;
using Core.Algorithm.Models.TemplateMatch;
using Net.Utilities.Models.Geometries;

namespace Core.Algorithm.Implements.AlgoCSharp;

internal sealed class AlgoCvTemplateMatchModel(
    TemplateMatchEngine engine,
    AlgoCvTemplateMatchParams engineParams,
    Point centerOffset = default) : ITemplateMatchModel
{
    public TemplateMatchEngine Engine => engine;

    public AlgoCvTemplateMatchParams EngineParams { get; } = engineParams;

    public Point CenterOffset { get; } = centerOffset;

    public void Dispose()
    {
        // engine 所有权在构造时从 Generate/Read 转入本 Model，由本方法负责释放，调用方不得再 Dispose 传入的 engine
#pragma warning disable IDISP007
        engine.Dispose();
#pragma warning restore IDISP007
        if (engine.IsDisposed == false) ThrowHelper.ThrowInvalidOperationException("Dispose failed!");
    }
}