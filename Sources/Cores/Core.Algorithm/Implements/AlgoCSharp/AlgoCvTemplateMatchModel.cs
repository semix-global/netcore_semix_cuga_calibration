using algocv_sharp;
using Core.Algorithm.Interfaces.TemplateMatch;
using Core.Algorithm.Models.TemplateMatch;

namespace Core.Algorithm.Implements.AlgoCSharp;

internal sealed class AlgoCvTemplateMatchModel(
    TemplateMatchEngine engine,
    AlgoCvTemplateMatchParams engineParams) : ITemplateMatchModel
{
    public TemplateMatchEngine Engine => engine;

    public AlgoCvTemplateMatchParams EngineParams { get; } = engineParams;

    public void Dispose()
    {
        // todo：防呆
        engine.Dispose();
    }
}
