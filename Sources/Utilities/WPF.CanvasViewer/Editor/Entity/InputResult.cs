using CanvasViewer.Editor.Enum;

namespace CanvasViewer.Editor.Entity;

/// <summary>
/// 编辑器输入结果
/// </summary>
/// <typeparam name="TInput">结果类型</typeparam>
public sealed class InputResult<TInput> where TInput : new()
{
    internal static InputResult<TInput> CancelResult(CancelReasonEnum cancelReason) => new(ResultModeEnum.Cancel, new TInput(), "", AcceptReasonEnum.None, cancelReason);

    internal static InputResult<TInput> KeywordResult(string keyword) => new(ResultModeEnum.Keyword, new TInput(), keyword, AcceptReasonEnum.Keyword, CancelReasonEnum.None);

    internal static InputResult<TInput> AcceptResult(TInput value, AcceptReasonEnum acceptReason) => new(ResultModeEnum.Ok, value, "", acceptReason, CancelReasonEnum.None);

    /// <summary>
    /// 输出结果的类型
    /// </summary>
    public ResultModeEnum ResultMode { get; private set; }

    /// <summary>
    /// 输出结果
    /// </summary>
    public TInput Value { get; private set; }

    /// <summary>
    /// 关键字(类似于Cad里面的命令): Close关键字表示: 图形封闭
    /// </summary>
    public string Keyword { get; private set; }

    /// <summary>
    /// 接受输入的原因
    /// </summary>
    internal AcceptReasonEnum AcceptReason { get; private set; }

    /// <summary>
    /// 取消输入的原因
    /// </summary>
    internal CancelReasonEnum CancelReason { get; private set; }

    private InputResult(ResultModeEnum result, TInput value, string keyword, AcceptReasonEnum acceptReason, CancelReasonEnum cancelReason)
    {
        ResultMode = result;
        Value = value;
        Keyword = keyword;
        AcceptReason = acceptReason;
        CancelReason = cancelReason;
    }
}