using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Graphics.Drawables;
using Net.Utilities.Graphics.Primitives.Editors;
using Net.Utilities.Graphics.Primitives.Enums.Medias;
using Net.Utilities.Graphics.Primitives.Medias;
using Net.Utilities.Graphics.Primitives.Medias.Styles;
using Net.Utilities.Graphics.Renderings;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;
using SkiaSharp;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

public sealed partial class BitmapImageROIDrawable(BitmapImageDrawable bitmapImageDrawable) : AbstractDrawable
{
    private static readonly (BitmapImageROIResizeJoystickStateEnum Type, string Name, Func<Rect, Point> GetPoint)[] ControlPointDefinitions =
    [
        (BitmapImageROIResizeJoystickStateEnum.XMaxYMax, nameof(BitmapImageROIResizeJoystickStateEnum.XMaxYMax), rect => rect.XMaxYMax),
        (BitmapImageROIResizeJoystickStateEnum.XMinYMax, nameof(BitmapImageROIResizeJoystickStateEnum.XMinYMax), rect => rect.XMinYMax),
        (BitmapImageROIResizeJoystickStateEnum.XMinYMin, nameof(BitmapImageROIResizeJoystickStateEnum.XMinYMin), rect => rect.XMinYMin),
        (BitmapImageROIResizeJoystickStateEnum.XMaxYMin, nameof(BitmapImageROIResizeJoystickStateEnum.XMaxYMin), rect => rect.XMaxYMin),
        (BitmapImageROIResizeJoystickStateEnum.XCenterYMax, nameof(BitmapImageROIResizeJoystickStateEnum.XCenterYMax), rect => rect.XCenterYMax),
        (BitmapImageROIResizeJoystickStateEnum.XMinYCenter, nameof(BitmapImageROIResizeJoystickStateEnum.XMinYCenter), rect => rect.XMinYCenter),
        (BitmapImageROIResizeJoystickStateEnum.XCenterYMin, nameof(BitmapImageROIResizeJoystickStateEnum.XCenterYMin), rect => rect.XCenterYMin),
        (BitmapImageROIResizeJoystickStateEnum.XMaxYCenter, nameof(BitmapImageROIResizeJoystickStateEnum.XMaxYCenter), rect => rect.XMaxYCenter)
    ];

    /// <summary>
    /// 该ROI所依附的图片. 所有ROI坐标都会被限制在该图片范围内.
    /// </summary>
    public readonly BitmapImageDrawable BitmapImageDrawable = bitmapImageDrawable;

    /// <summary>
    /// 未锁定时ROI的填充样式.
    /// </summary>
    [ObservableProperty]
    public partial FillStyle FillStyle { get; set; } = new(SKColors.Red.WithAlpha(64));

    /// <summary>
    /// 锁定时ROI的填充样式.
    /// </summary>
    [ObservableProperty]
    public partial FillStyle FixedFillStyle { get; set; } = new(SKColors.Green.WithAlpha(64));

    /// <summary>
    /// 笛卡尔坐标系: 当前ROI矩形区域.
    /// </summary>
    [ObservableProperty]
    public partial Rect Rect { get; set; }

    /// <summary>
    /// 是否锁定该ROI. 锁定后不可编辑, 并使用 FixedFillStyle 绘制.
    /// </summary>
    [ObservableProperty]
    public partial bool IsFixed { get; set; } = false;

    /// <summary>
    /// 绘制在ROI中心的文本标签. 为空时不绘制.
    /// </summary>
    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;

    /// <summary>
    /// 笛卡尔坐标系: 表示当前编辑器用于调整ROI区域的手柄角状态. 该属性用于确定可调整的ROI顶点或边界, 从而控制用户在界面中的编辑行为.
    /// </summary>
    [ObservableProperty]
    public partial BitmapImageROIResizeJoystickStateEnum ResizeJoystickStateEnum { get; set; } = BitmapImageROIResizeJoystickStateEnum.All;

    /// <summary>
    /// 编辑器是否已修改该 ROI. 仅由编辑器内部更新, 用于标识相对进入编辑前的原始状态是否发生变化.
    /// </summary>
    [ObservableProperty]
    public partial bool IsEditorModified { get; internal set; } = false;

    public override void Draw(Renderer renderer)
    {
        if (BitmapImageDrawable.BitmapImage is null || Rect.IsEmpty) return;

        renderer.FillRectangle(IsFixed ? FixedFillStyle : FillStyle, Rect);
        var lineStyle = new LineStyle(IsFixed
            ? FixedFillStyle.BackgroundColor.WithAlpha(255)
            : FillStyle.BackgroundColor.WithAlpha(255));
        renderer.DrawRectangle(lineStyle, Rect);

        if (string.IsNullOrEmpty(Text) == false)
        {
            renderer.DrawString(
                new TextStyle(Fonts.Monospace, Math.Min(Rect.Width, Rect.Height) / 4d, isUnitPx: true),
                new FillStyle(IsFixed
                    ? FixedFillStyle.BackgroundColor.WithAlpha(255)
                    : FillStyle.BackgroundColor.WithAlpha(255)),
                Rect.Center,
                Text,
                AlignmentEnum.MiddleCenter,
                new FillStyle(SKColors.Transparent),
                new LineStyle(SKColors.Transparent),
                new Vector(0d, 0d),
                new Padding(0d));
        }
    }

    public override Extents GetExtents() => (Extents)Rect;

    public override ControlPoint[] GetControlPoints()
    {
        var controlPointList = new List<ControlPoint>(ControlPointDefinitions.Length);

        foreach (var (type, name, getPoint) in ControlPointDefinitions)
        {
            if ((ResizeJoystickStateEnum & type) == type) controlPointList.Add(new ControlPoint(name, getPoint(Rect)));
        }

        return [.. controlPointList];
    }
}