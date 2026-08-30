using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Net.Utilities.Graphics.Drawables;
using Net.Utilities.Graphics.Primitives.Editors;
using Net.Utilities.Graphics.Primitives.Medias.Styles;
using Net.Utilities.Graphics.Renderings;
using Net.Utilities.Models.Geometries;
using Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;
using SkiaSharp;

namespace Net.Utilities.OpticsFourierImageViewer.WPF.Drawables;

public sealed partial class RectROIDrawable : AbstractDrawable
{
    private static readonly (RectROIDrawableControlPointTypesEnum Type, string Name, Func<Rect, Point> GetPoint)[] s_controlPointDefinitions =
    [
        (RectROIDrawableControlPointTypesEnum.XMaxYMax, nameof(Rect.XMaxYMax), rect => rect.XMaxYMax),
        (RectROIDrawableControlPointTypesEnum.XMinYMax, nameof(Rect.XMinYMax), rect => rect.XMinYMax),
        (RectROIDrawableControlPointTypesEnum.XMinYMin, nameof(Rect.XMinYMin), rect => rect.XMinYMin),
        (RectROIDrawableControlPointTypesEnum.XMaxYMin, nameof(Rect.XMaxYMin), rect => rect.XMaxYMin),
        (RectROIDrawableControlPointTypesEnum.XCenterYMax, nameof(Rect.XCenterYMax), rect => rect.XCenterYMax),
        (RectROIDrawableControlPointTypesEnum.XMinYCenter, nameof(Rect.XMinYCenter), rect => rect.XMinYCenter),
        (RectROIDrawableControlPointTypesEnum.XCenterYMin, nameof(Rect.XCenterYMin), rect => rect.XCenterYMin),
        (RectROIDrawableControlPointTypesEnum.XMaxYCenter, nameof(Rect.XMaxYCenter), rect => rect.XMaxYCenter)
    ];

    [ObservableProperty]
    public partial FillStyle FillStyle { get; set; } = new(SKColors.Transparent);

    [ObservableProperty]
    public partial LineStyle LineStyle { get; set; } = new(SKColors.Red);

    [ObservableProperty]
    public partial bool IsShowCrossLine { get; set; }

    // 默认显示四个角点和四个边中点；调用方可以按 Flags 隐藏不需要的锚点。
    [ObservableProperty]
    public partial RectROIDrawableControlPointTypesEnum ControlPointTypesEnum { get; set; } =
        RectROIDrawableControlPointTypesEnum.XMaxYMax |
        RectROIDrawableControlPointTypesEnum.XMinYMax |
        RectROIDrawableControlPointTypesEnum.XMinYMin |
        RectROIDrawableControlPointTypesEnum.XMaxYMin |
        RectROIDrawableControlPointTypesEnum.XCenterYMax |
        RectROIDrawableControlPointTypesEnum.XMinYCenter |
        RectROIDrawableControlPointTypesEnum.XCenterYMin |
        RectROIDrawableControlPointTypesEnum.XMaxYCenter;

    /// <summary>
    /// Rect 发生变化后由生成的属性回调自动置为 true；初始化或回滚时可显式恢复。
    /// </summary>
    [ObservableProperty]
    public partial bool IsModified { get; set; }

    [ObservableProperty]
    public partial Rect Rect { get; set; }

    // 使用 partial 回调统一记录所有 Rect 修改，避免移动和缩放路径分别维护状态。
    partial void OnRectChanged(Rect value) => IsModified = true;

    public override void Draw(Renderer renderer)
    {
        if (Rect.IsEmpty) return;

        renderer.FillRectangle(FillStyle, Rect);
        renderer.DrawRectangle(LineStyle, Rect);

        if (IsShowCrossLine == false) return;

        renderer.DrawLine(LineStyle, new Point(Rect.Center.X, Rect.YMin), new Point(Rect.Center.X, Rect.YMax));
        renderer.DrawLine(LineStyle, new Point(Rect.XMin, Rect.Center.Y), new Point(Rect.XMax, Rect.Center.Y));
    }

    public override Extents GetExtents() => (Extents)Rect;

    public override ControlPoint[] GetControlPoints()
    {
        // 名称、Flags 和坐标统一从同一份定义生成，避免锚点名称与实际位置错位。
        var controlPoints = new List<ControlPoint>(s_controlPointDefinitions.Length);
        foreach (var (type, name, getPoint) in s_controlPointDefinitions)
        {
            if ((ControlPointTypesEnum & type) == type)
                controlPoints.Add(new ControlPoint(name, getPoint(Rect)));
        }

        return [.. controlPoints];
    }
}
