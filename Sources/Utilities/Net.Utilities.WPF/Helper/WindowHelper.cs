using HandyControl.Interactivity;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Net.Utilities.WPF.Helper;

public static class WindowHelper
{
    /// <summary>
    /// 确保视图是一个窗口被一个窗口包裹
    /// </summary>
    /// <param name="view">View</param>
    /// <param name="isDialog">无论窗口是否显示为对话框</param>
    /// <returns>窗口</returns>
    public static Window EnsureWindow(UIElement view, bool isDialog)
    {
        if (view is Window window)
        {
            if (isDialog == false)
            {
                window.Owner = Application.Current.MainWindow;
                return window;
            }

            var owner = InferOwnerOf(window);
            if (owner is not null) window.Owner = owner;
        }
        else
        {
            window = new Window
            {
                Content = view,
                SizeToContent = SizeToContent.Manual
            };

            var owner = InferOwnerOf(window);
            if (owner is not null)
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Owner = owner;
            }
            else
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }

        return window;
    }

    /// <summary>
    /// 推断获取活动窗口
    /// </summary>
    /// <param name="window">需要确定其所有者的窗口</param>
    /// <returns>活动窗口</returns>
    public static Window? InferOwnerOf(Window window)
    {
        var application = Application.Current;
        if (application is null) return null;

        // 获取当前活动窗口
        var active = application.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
        active ??= application.MainWindow is null
            ? null
            : PresentationSource.FromVisual(application.MainWindow) is null
                ? null
                : application.MainWindow;

        return active == window ? null : active;
    }

    /// <summary>
    /// 窗口上创建遮罩
    /// </summary>
    /// <param name="outputWindow">窗口</param>
    public static void CreateMask(Window outputWindow)
    {
        var parent = outputWindow.Owner;
        if (parent is null) return;

        var layer = VisualHelper.GetAdornerLayer(parent);
        if (layer is null) return;

        var adornerContainer = new AdornerContainer(layer)
        {
            Child = new Grid // 遮罩层
            {
                Background = Brushes.White,
                Opacity = 0.5
            }
        };
        layer.Add(adornerContainer);
        outputWindow.Closed += (_, _) => layer.Remove(adornerContainer);
    }

    /// <summary>
    /// 窗口前置(必须先显示窗口)
    /// </summary>
    /// <param name="window">窗口</param>
    /// <param name="isPined">是否固定住</param>
    public static void FrontWindow(Window window, bool isPined = true)
    {
        // 如果窗体最小化，则恢复窗体
        if (window.WindowState == WindowState.Minimized) window.WindowState = WindowState.Normal;

        var handle = new WindowInteropHelper(window).Handle;
        if (isPined)
            // 设置窗体显示在最上层
            User32WrapperHelper.SetWindowPos(handle, User32WrapperHelper.HWND_TOPMOST, 0, 0, 0, 0, User32WrapperHelper.SET_WINDOW_POS_FLAGS.SWP_NOSIZE | User32WrapperHelper.SET_WINDOW_POS_FLAGS.SWP_NOMOVE | User32WrapperHelper.SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE);

        // 设置本窗体为活动窗体
        User32WrapperHelper.SetActiveWindow(handle);
        User32WrapperHelper.SetForegroundWindow(handle);
        User32WrapperHelper.EnableWindow(handle, true);
    }
}