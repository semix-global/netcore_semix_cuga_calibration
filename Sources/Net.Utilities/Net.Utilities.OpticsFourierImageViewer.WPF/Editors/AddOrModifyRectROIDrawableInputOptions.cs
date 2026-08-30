using CommunityToolkit.Diagnostics;
using Net.Utilities.Graphics.Primitives.Editors.Getters.Options;
using Net.Utilities.Models;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Editors;

public sealed class AddOrModifyRectROIDrawableInputOptions : InputOptions<Unit>
{
    /// <summary>
    /// 编辑器操作所依据的图片；所有 ROI 坐标都会被限制在该图片范围内。
    /// </summary>
    public BitmapImageDrawable BitmapImageDrawable { get; }

    /// <summary>
    /// 是否允许拖拽 ROI 本体移动。关闭时仍然可以通过锚点调整 ROI 大小。
    /// </summary>
    public bool IsEnableDragMove { get; set; } = true;

    /// <summary>
    /// 创建只编辑已有 Rect ROI 的输入选项。
    /// </summary>
    public AddOrModifyRectROIDrawableInputOptions(BitmapImageDrawable bitmapImageDrawable) : base("Select Point")
    {
        Guard.IsNotNull(bitmapImageDrawable);
        BitmapImageDrawable = bitmapImageDrawable;
    }
}
