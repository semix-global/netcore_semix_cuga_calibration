namespace Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;

/// <summary>
/// 表示位图图像感兴趣区域 (ROI) 拖拽移动的类型枚举.
/// </summary>
[Flags]
public enum BitmapImageROIDragMoveTypeEnum
{
    None = 0,
    X = 1 << 0,
    Y = 1 << 1,
    All = X | Y
}