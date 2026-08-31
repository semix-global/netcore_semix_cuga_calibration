namespace Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;

/// <summary>
/// 描述 Bitmap 图像 ROI 控制点类型的枚举.
/// 此枚举通过标志位表示不同位置的矩形控制点, 例如矩形的四个角点、边缘中点以及分组的所有点.
/// 可用于定义和处理矩形范围的可视化控制点行为.
/// </summary>
[Flags]
public enum BitmapImageROIControlPointTypeEnum
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