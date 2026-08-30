namespace Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;

/// <summary>
/// 控制 Rect ROI 是否显示对应的八个锚点。
/// </summary>
[Flags]
public enum RectROIDrawableControlPointTypesEnum
{
    /// <summary>不显示锚点。</summary>
    None = 0,

    XMaxYMax = 1 << 0,
    XMinYMax = 1 << 1,
    XMinYMin = 1 << 2,
    XMaxYMin = 1 << 3,
    XCenterYMax = 1 << 4,
    XMinYCenter = 1 << 5,
    XCenterYMin = 1 << 6,
    XMaxYCenter = 1 << 7,

    /// <summary>八个锚点的按位或组合。</summary>
    All = XMaxYMax | XMinYMax | XMinYMin | XMaxYMin | XCenterYMax | XMinYCenter | XCenterYMin | XMaxYCenter
}
