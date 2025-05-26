namespace CanvasViewer.Editor.Entity;

/// <summary>
/// 编辑器Getter的输入参数
/// </summary>
/// <typeparam name="TInput">计算输入结果的中间类型</typeparam>
/// <typeparam name="TValue">输入结果类型</typeparam>
internal sealed class InputArgs<TInput, TValue>(TInput input) where TValue : new()
{
    /// <summary>
    /// 计算输入结果的中间值<br/>
    /// 如果获取角度点等信息类型为Point2D, 如果是获取字符串那么类型为string
    /// </summary>
    public readonly TInput Input = input;

    /// <summary>
    /// 输入结果初始值
    /// </summary>
    public TValue Value { get; set; } = new();

    /// <summary>
    /// 无效错误信息
    /// </summary>
    public string ErrorMessage { get; set; } = "*Invalid input*";

    /// <summary>
    /// 输入是否有效
    /// </summary>
    public bool IsInputValid { get; set; } = true;

    /// <summary>
    /// 输入是否完成
    /// </summary>
    public bool IsInputCompleted { get; set; } = true;
}