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
    /// 编辑器操作所依据的图片. 所有ROI坐标都会被限制在该图片范围内.
    /// </summary>
    public BitmapImageDrawable BitmapImageDrawable { get; }

    /// <summary>
    /// 编辑提示信息面板顶部显示的标题. 为空时不显示, 默认 "Please edit ROI".
    /// </summary>
    [ObservableProperty]
    public partial string Title { get; set; } = "Please edit ROI";

    /// <summary>
    /// 允许拖拽移动ROI的方向. 可限制为仅水平、仅垂直、全部方向或禁止移动.
    /// </summary>
    [ObservableProperty]
    public partial BitmapImageROIDragMoveTypeEnum BitmapImageROIDragMoveTypeEnum { get; set; } = BitmapImageROIDragMoveTypeEnum.All;

    /// <summary>
    /// 是否允许按 Delete 删除选中的ROI(设为不可见), 默认关闭. 删除支持撤销、重做和取消恢复.
    /// </summary>
    [ObservableProperty]
    public partial bool IsDeleteEnabled { get; set; } = false;

    /// <summary>
    /// 笛卡尔坐标系: 编辑提示信息面板在视图中的停靠角.
    /// 仅支持四个角点(XMinYMax / XMaxYMax / XMinYMin / XMaxYMin), 默认右下角.
    /// </summary>
    [ObservableProperty]
    public partial BitmapImageROIResizeJoystickStateEnum EditInformationCorner { get; set; } = BitmapImageROIResizeJoystickStateEnum.XMaxYMin;

    /// <summary>
    /// 方向键移动ROI的步距, 单位为像素, 必须为有限正数.
    /// </summary>
    [ObservableProperty]
    public partial double KeyboardMoveStep { get; set; } = 1d;

    /// <summary>
    /// Shift+方向键向外扩展ROI对应边的步距, 单位为像素, 必须为有限正数.
    /// </summary>
    [ObservableProperty]
    public partial double KeyboardResizeStep { get; set; } = 1d;

    partial void OnKeyboardMoveStepChanging(double value)
    {
        Guard.IsTrue(double.IsFinite(value));
        Guard.IsGreaterThan(value, 0d);
    }

    partial void OnKeyboardResizeStepChanging(double value)
    {
        Guard.IsTrue(double.IsFinite(value));
        Guard.IsGreaterThan(value, 0d);
    }

    /// <summary>
    /// 创建只编辑已有ROI的输入选项. 所依据的图片不能为空.
    /// </summary>
    public ModifyBitmapImageROIDrawableInputOptions(BitmapImageDrawable bitmapImageDrawable) : base("Select ROI")
    {
        Guard.IsNotNull(bitmapImageDrawable.BitmapImage);

        BitmapImageDrawable = bitmapImageDrawable;
    }

    /// <summary>
    /// 笛卡尔坐标系: 返回所依据图片的外接矩形, 用于将ROI限制在图片范围内.
    /// </summary>
    public Rect GetImageRect()
    {
        var bitmapImage = BitmapImageDrawable.BitmapImage;
        Guard.IsNotNull(bitmapImage);
        Guard.IsFalse(bitmapImage.IsEmpty);

        return new Rect(BitmapImageDrawable.Point, bitmapImage.Size);
    }
}