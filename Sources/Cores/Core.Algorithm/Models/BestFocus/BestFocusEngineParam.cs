using Core.Models.Enums.Algorithm;

namespace Core.Algorithm.Models.BestFocus;

public sealed class BestFocusEngineParam
{
    /// <summary>
    /// 开始ECS
    /// </summary>
    public double StartEcs { get; init; }

    /// <summary>
    /// 停止ECS
    /// </summary>
    public double StopEcs { get; init; }

    /// <summary>
    /// bestfocus目标晶圆类型
    /// </summary>
    public AlgorithmBestFocusTypeEnum BestFocusType { get; init; }

    /// <summary>
    /// 算法库独有的参数
    /// </summary>
    public EngineParams? EngineParams { get; init; }

    public object ToHtmlAnonymous() => new
    {
        StartEcs,
        StopEcs,
        BestFocusType,
        EngineParams = EngineParams?.ToHtmlAnonymous()
    };
}
