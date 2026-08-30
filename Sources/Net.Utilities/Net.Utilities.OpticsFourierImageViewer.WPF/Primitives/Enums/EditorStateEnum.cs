namespace Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;

/// <summary>
/// ROI 编辑器当前所处的交互状态。
/// </summary>
public enum EditorStateEnum
{
    /// <summary>等待点选或框选 ROI。</summary>
    Select,

    /// <summary>已有 ROI 被选中，可执行移动或缩放。</summary>
    Modify
}
