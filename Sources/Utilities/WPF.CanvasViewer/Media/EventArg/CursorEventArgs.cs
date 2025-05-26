using CanvasViewer.Geometry;
using CanvasViewer.Media.Drawing.Enum;

namespace CanvasViewer.Media.EventArg;

public sealed class CursorEventArgs(MouseButtonsEnum button, Point2D location)
    : EventArgs
{
    /// <summary>
    /// 获取曾按下的是哪个鼠标按钮
    /// </summary>
    public MouseButtonsEnum Button { get; private set; } = button;

    /// <summary>
    /// 获取鼠标在产生鼠标事件时的世界坐标
    /// </summary>
    public Point2D Location { get; private set; } = location;

    /// <summary>
    /// 获取按钮并释放鼠标按钮的次数
    /// </summary>
    public int Clicks { get; set; }

    /// <summary>
    /// 获取鼠标轮已转动的制动器数的有效符号计数乘以WHERE_DELTA常数. 制动器是鼠标轮的一个凹口
    /// </summary>
    public int Delta { get; set; }

    /// <summary>
    /// 获取鼠标在产生鼠标事件时的X世界坐标
    /// </summary>
    public double X => Location.X;

    /// <summary>
    /// 获取鼠标在产生鼠标事件时的Y世界坐标
    /// </summary>
    public double Y => Location.Y;
}