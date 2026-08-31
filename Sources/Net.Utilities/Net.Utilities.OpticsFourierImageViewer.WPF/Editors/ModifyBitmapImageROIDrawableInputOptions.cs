using CommunityToolkit.Diagnostics;
using Net.Utilities.Graphics.Primitives.Editors.Getters.Options;
using Net.Utilities.Models;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Editors;

public sealed class ModifyBitmapImageROIDrawableInputOptions : InputOptions<Unit>
{
    /// <summary>
    /// 编辑器操作所依据的图片: 所有ROI坐标都会被限制在该图片范围内
    /// </summary>
    public BitmapImageDrawable BitmapImageDrawable { get; }

    /// <summary>
    /// ROI 本体允许移动的方向。None 表示不允许移动，All 表示允许 X、Y 两个方向移动。
    /// </summary>
    public BitmapImageROIDragMoveTypeEnum IsEnableDragMove { get; set; } = BitmapImageROIDragMoveTypeEnum.All;

    /// <summary>
    /// 创建只编辑已有Rect ROI的输入选项
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
