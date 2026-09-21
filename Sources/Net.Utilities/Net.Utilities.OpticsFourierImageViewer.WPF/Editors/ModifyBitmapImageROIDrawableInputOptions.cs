using CommunityToolkit.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Graphics.Primitives.Editors.Getters.Options;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Editors;

public sealed partial class ModifyBitmapImageROIDrawableInputOptions : InputOptions<Unit>
{
    /// <summary>
    /// 编辑器操作所依据的图片: 所有ROI坐标都会被限制在该图片范围内.
    /// </summary>
    public BitmapImageDrawable BitmapImageDrawable { get; }

    /// <summary>
    /// 表示位图图像感兴趣区域 (ROI) 拖拽移动的类型枚举, 用于定义可以操作的拖拽方向.
    /// </summary>
    [ObservableProperty]
    public partial BitmapImageROIDragMoveTypeEnum BitmapImageROIDragMoveTypeEnum { get; set; } = BitmapImageROIDragMoveTypeEnum.All;

    /// <summary>
    /// 是否允许按 Delete 隐藏选中的 ROI, 默认关闭. 隐藏支持撤销, 重做和取消恢复.
    /// </summary>
    [ObservableProperty]
    public partial bool IsDeleteEnabled { get; set; } = false;

    /// <summary>
    /// 方向键移动 ROI 的步距, 单位为像素, 必须为有限正数.
    /// </summary>
    [ObservableProperty]
    public partial double KeyboardMoveStep { get; set; } = 1d;

    partial void OnKeyboardMoveStepChanging(double value)
    {
        Guard.IsTrue(double.IsFinite(value));
        Guard.IsGreaterThan(value, 0d);
    }

    /// <summary>
    /// Shift + 方向键向外扩展 ROI 对应边的步距, 单位为像素, 必须为有限正数.
    /// </summary>
    [ObservableProperty]
    public partial double KeyboardResizeStep { get; set; } = 1d;

    partial void OnKeyboardResizeStepChanging(double value)
    {
        Guard.IsTrue(double.IsFinite(value));
        Guard.IsGreaterThan(value, 0d);
    }

    /// <summary>
    /// 创建只编辑已有Rect ROI的输入选项.
    /// </summary>
    public ModifyBitmapImageROIDrawableInputOptions(BitmapImageDrawable bitmapImageDrawable) : base("Select ROI")
    {
        Guard.IsNotNull(bitmapImageDrawable.BitmapImage);

        BitmapImageDrawable = bitmapImageDrawable;
    }

    public Rect GetImageRect()
    {
        var bitmapImage = BitmapImageDrawable.BitmapImage;
        Guard.IsNotNull(bitmapImage);
        Guard.IsFalse(bitmapImage.IsEmpty);

        return new Rect(BitmapImageDrawable.Point, bitmapImage.Size);
    }
}