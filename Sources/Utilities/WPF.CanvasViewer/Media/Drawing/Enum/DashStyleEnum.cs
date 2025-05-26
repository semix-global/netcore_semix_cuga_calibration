namespace CanvasViewer.Media.Drawing.Enum;

/// <summary>
/// dash样式
/// </summary>
public enum DashStyleEnum
{
    /// <summary>
    /// dash样式使用Layer的dash样式
    /// </summary>
    ByLayer = -1,

    /// <summary>
    /// 实心
    /// </summary>
    Solid = 0,

    /// <summary>
    /// 破折号
    /// </summary>
    Dash = 1,

    /// <summary>
    /// 点
    /// </summary>
    Dot = 2,

    /// <summary>
    /// 破折号 点
    /// </summary>
    DashDot = 3,

    /// <summary>
    /// 破折号 点 点
    /// </summary>
    DashDotDot = 4
}