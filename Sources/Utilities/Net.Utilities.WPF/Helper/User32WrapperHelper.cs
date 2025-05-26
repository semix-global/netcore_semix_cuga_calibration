using Windows.Win32;
using Windows.Win32.Foundation;

namespace Net.Utilities.WPF.Helper;

public static class User32WrapperHelper
{
    // 设置此窗体为活动窗体：
    // 将创建指定窗口的线程带到前台并激活该窗口。键盘输入直接指向窗口，并为用户更改各种视觉提示。
    // 系统为创建前台窗口的线程分配的优先级略高于其他线程。
    /// <summary>
    /// 将创建指定窗口的线程引入前台并激活窗口。 键盘输入将定向到窗口，并为用户更改各种视觉提示。 系统为创建前台窗口的线程分配的优先级略高于其他线程的优先级
    /// </summary>
    /// <param name="hWnd">应激活并带到前台的窗口的句柄</param>
    /// <returns>如果将窗口带到前台，则返回值为非零值。如果未将窗口带到前台，则返回值为零</returns>
    public static bool SetForegroundWindow(IntPtr hWnd) => PInvoke.SetForegroundWindow((HWND)hWnd);

    /// <summary>
    /// 激活窗口。 窗口必须附加到调用线程的消息队列
    /// </summary>
    /// <param name="hWnd">要激活的顶级窗口的句柄</param>
    /// <returns>如果函数成功，则返回值是以前处于活动状态的窗口的句柄。如果函数失败，则返回值为 NULL。 要获得更多的错误信息，请调用 GetLastError</returns>
    public static IntPtr SetActiveWindow(IntPtr hWnd) => PInvoke.SetActiveWindow((HWND)hWnd);

    /// <summary>
    /// 更改子窗口、弹出窗口或顶级窗口的大小、位置和 Z 顺序。 这些窗口根据屏幕上的外观进行排序。 最上面的窗口接收最高排名，是 Z 顺序中的第一个窗口
    /// </summary>
    /// <param name="hWnd">窗口的句柄</param>
    /// <param name="hWndInsertAfter">在 Z 顺序中定位窗口之前窗口的句柄。 此参数必须是窗口句柄或以下值之一</param>
    /// <param name="x">窗口左侧的新位置，以客户端坐标表示</param>
    /// <param name="y">窗口顶部的新位置，以客户端坐标表示</param>
    /// <param name="cx">窗口的新宽度（以像素为单位）</param>
    /// <param name="cy">窗口的新高度（以像素为单位）</param>
    /// <param name="uFlags">窗口大小调整和定位标志</param>
    /// <returns>如果该函数成功，则返回值为非零值。如果函数失败，则返回值为零。 要获得更多的错误信息，请调用 GetLastError</returns>
    public static bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, SET_WINDOW_POS_FLAGS uFlags) =>
        PInvoke.SetWindowPos((HWND)hWnd, (HWND)hWndInsertAfter, x, y, cx, cy, (Windows.Win32.UI.WindowsAndMessaging.SET_WINDOW_POS_FLAGS)uFlags);

    /// <summary>
    /// 启用或禁用指定窗口或控件的鼠标和键盘输入
    /// </summary>
    /// <param name="hWnd">窗口的句柄</param>
    /// <param name="bEnable">指示是启用或禁用窗口</param>
    /// <returns>如果以前禁用窗口，则返回值为非零值. 如果以前未禁用窗口，则返回值为零</returns>
    public static bool EnableWindow(IntPtr hWnd, bool bEnable) => PInvoke.EnableWindow((HWND)hWnd, bEnable);

    #region Model

    // ReSharper disable InconsistentNaming
    // ReSharper disable IdentifierTypo
    // ReSharper disable UnusedMember.Local

    /// <summary>
    /// 将窗口置于所有非最顶部窗口的上面。 该窗口即使已停用，也会保留在最高位置
    /// </summary>
    public static readonly IntPtr HWND_TOPMOST = new(-1);

    /// <summary>
    /// 将窗口置于 Z 顺序的底部。 如果 hWnd 参数标识最顶层的窗口，则窗口将失去其最顶层状态，并放置在所有其他窗口的底部
    /// </summary>
    public static readonly IntPtr HWND_BOTTOM = new(1);

    [Flags]
    public enum SET_WINDOW_POS_FLAGS : uint
    {
        SWP_ASYNCWINDOWPOS = 0x00004000,
        SWP_DEFERERASE = 0x00002000,
        SWP_FRAMECHANGED = 0x00000020,

        /// <summary>
        /// 隐藏窗口
        /// </summary>
        SWP_HIDEWINDOW = 0x00000080,

        /// <summary>
        /// 不激活窗口。 如果未设置此标志，则会激活窗口，并根据 hWndInsertAfter 参数) 的设置 (将窗口移到最顶部或最顶层组的顶部）
        /// </summary>
        SWP_NOACTIVATE = 0x00000010,

        SWP_NOCOPYBITS = 0x00000100,

        /// <summary>
        /// 保留当前位置 (忽略 X 和 Y 参数)
        /// </summary>
        SWP_NOMOVE = 0x00000002,

        SWP_NOREDRAW = 0x00000008,
        SWP_NOREPOSITION = 0x00000200,
        SWP_NOSENDCHANGING = 0x00000400,

        /// <summary>
        /// 保留当前大小 (忽略 cx 和 cy 参数)
        /// </summary>
        SWP_NOSIZE = 0x00000001,

        SWP_NOZORDER = 0x00000004,
        SWP_SHOWWINDOW = 0x00000040
    }

    // ReSharper restore InconsistentNaming
    // ReSharper restore IdentifierTypo
    // ReSharper restore UnusedMember.Local

    #endregion Model
}