namespace Net.Utilities.OpticsFourierImageViewer.WPF.Primitives.Enums;

[Flags]
public enum BitmapImageROIDragMoveTypeEnum
{
    None = 0,
    X = 1 << 0,
    Y = 1 << 1,
    All = X | Y
}
