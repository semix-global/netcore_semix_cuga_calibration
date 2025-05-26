namespace CanvasViewer.Media.Drawing.Enum;

[Flags]
public enum MouseButtonsEnum
{
    /// <summary>
    /// The left mouse button was pressed.
    /// </summary>
    Left = 0x00100000,

    /// <summary>
    /// No mouse button was pressed.
    /// </summary>
    None = 0x00000000,

    /// <summary>
    /// The right mouse button was pressed.
    /// </summary>
    Right = 0x00200000,

    /// <summary>
    /// The middle mouse button was pressed.
    /// </summary>
    Middle = 0x00400000,

    /// <summary>
    /// 扩展鼠标按钮1被按下
    /// </summary>
    XButton1 = 0x00800000,

    /// <summary>
    /// 扩展鼠标按钮2被按下
    /// </summary>
    XButton2 = 0x01000000
}