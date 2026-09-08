namespace Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;

/// <summary>
/// 表示一个枚举, 用于描述在图像 ROI (Region of Interest) 重置过程中, 控制杆的状态与位置.
/// </summary>
/// <remarks>
/// 枚举值可以表示不同的控制点状态, 包括但不限于图像区域的四个角点 (XMaxYMax, XMinYMax, XMaxYMin, XMinYMin),
/// 边缘中心点 (XCenterYMax, XMinYCenter, XCenterYMin, XMaxYCenter), 以及全部控制点 (All).
/// 这些值可以单独使用, 或通过标志组合来描述複杂的状态.
/// </remarks>
[Flags]
public enum BitmapImageROIResizeJoystickStateEnum
{
    None = 0,
    XMaxYMax = 1 << 0,
    XMinYMax = 1 << 1,
    XMinYMin = 1 << 2,
    XMaxYMin = 1 << 3,
    XCenterYMax = 1 << 4,
    XMinYCenter = 1 << 5,
    XCenterYMin = 1 << 6,
    XMaxYCenter = 1 << 7,
    All = XMaxYMax | XMinYMax | XMinYMin | XMaxYMin | XCenterYMax | XMinYCenter | XCenterYMin | XMaxYCenter
}