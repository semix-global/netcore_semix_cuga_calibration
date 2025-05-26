namespace CanvasViewer.Editor.Entity;

/// <summary>
/// 编辑器Getter的初始化参数
/// </summary>
/// <typeparam name="TValue">输入结果类型</typeparam>
internal sealed class InitArgs<TValue> where TValue : new()
{
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
    /// 编辑器Getter是否等待输入完成
    /// </summary>
    public bool IsContinueAsync { get; set; } = true;
}